using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Newtonsoft.Json;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.WinUI;
using SanteDB.Core;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Win.Controls
{
    internal class SanteDBWebView2 : WebView2
    {
        /// <summary>
        /// Key strings for the webview2 context menu. Items not present in this list are excluded from the context menu. This prevents things like search with bing, translate, etc from appearing.
        /// </summary>
        private static readonly string[] s_AllowedContextMenuItems = new[] { "back", "forward", "reload", "print", "emoji", "undo", "redo", "cut", "copy", "paste", "pasteAndMatchStyle", "selectAll" };

        private JsonSerializer m_JsonSerializer = new();

        public SanteDBWebView2()
        {
            this.CoreWebView2Initialized += SanteDBWebView2_CoreWebView2Initialized;
        }

        private void SanteDBWebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (null != args.Exception)
                throw args.Exception;

            CoreWebView2.Settings.UserAgent = $"SanteDB-{SanteDBMagic}";
            CoreWebView2.Settings.AreHostObjectsAllowed = true;
            CoreWebView2.AddWebResourceRequestedFilter("*/_appservice/*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
            CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
        }

        private async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            var uri = new Uri(args.Request.Uri);

            await Task.Yield();

            try
            {

                //Look at the path that was requested to determine which handler should process the request. The deferral will allow the handler to take time to complete and provide the response.
                switch (uri.AbsolutePath)
                {
                    case "/_appservice/state":
                        await HandleStateRequestAsync(sender, args);
                        break;
                    case "/_appservice/toast":
                        await HandleToastNotificationAsync(sender, args);
                        break;
                    case "/_appservice/strings":
                        await HandleStringRequestAsync(sender, args);
                        break;
                    case "/_appservice/barcodescan":
                        await HandleBarcodeScanRequestAsync(sender, args);
                        break;
                    default:
                        break;
                }

            }
            finally
            {
                deferral.Complete();
                deferral.Dispose();
            }
        }

        private Task HandleStateRequestAsync(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var configmgr = ApplicationServiceContext.GetService<IConfigurationManager>();
            var upstream = configmgr?.GetSection<UpstreamConfigurationSection>();

            var devicecredential = upstream?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Device);
            var appcredential = upstream?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Application);

            var security = configmgr?.GetSection<SecurityConfigurationSection>();

            var response = new Shared.AppServiceStateResponse
            {
                Version = GetAssemblyVersion(),
                Online = ApplicationServiceContext?.IsRunning ?? false,
                Hdsi = ApplicationServiceContext?.IsRunning ?? false,
                Ami = ApplicationServiceContext?.IsRunning ?? false,
                ClientId = upstream?.Realm == null ? null : appcredential?.CredentialName,
                DeviceId = upstream?.Realm == null ? null : devicecredential?.CredentialName,
                Magic = ApplicationServiceContext?.ActivityUuid.ToString(),
                Realm = upstream?.Realm?.DomainName,
                FacilityId = security?.GetSecurityPolicy<Guid>(Core.Configuration.SecurityPolicyIdentification.AssignedFacilityUuid).ToString(),
                OwnerId = security?.GetSecurityPolicy<Guid>(Core.Configuration.SecurityPolicyIdentification.AssignedOwnerUuid).ToString(),
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response)));
            args.Response = sender.Environment.CreateWebResourceResponse(stream.AsRandomAccessStream(), 200, "OK", "Content-Type: application/json");

            return Task.CompletedTask;
        }

        private Task HandleToastNotificationAsync(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            try
            {
                using var requeststream = args.Request.Content.AsStreamForRead();

                using var sr = new StreamReader(requeststream);
                using var jtr = new JsonTextReader(sr);

                var toastrequest = m_JsonSerializer.Deserialize<ToastRequest>(jtr);

                args.Response = sender.Environment.CreateWebResourceResponse(null, 204, "NO CONTENT", "Content-Length: 0");

                if (null != toastrequest)
                {

                    var notificationbuilder = new AppNotificationBuilder()
                        //.SetScenario(AppNotificationScenario.Default)
                        //.SetTimeStamp(DateTimeOffset.Now)
                        //.AddButton(new AppNotificationButton("Dismiss")
                        //{
                        //    ButtonStyle = AppNotificationButtonStyle.Default
                        //})
                        ;

                    if (!string.IsNullOrEmpty(toastrequest.Text))
                    {
                        notificationbuilder.AddText(toastrequest.Text);
                    }

                    var notification = notificationbuilder.BuildNotification();

                    AppNotificationManager.Default.Show(notification);
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }

            return Task.CompletedTask;
        }

        private Task HandleStringRequestAsync(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var localization = ApplicationServiceContext.GetService<ILocalizationService>();

            var uri = new Uri(args.Request.Uri);
            //TODO: Locale
            var locale = "en";

            var strings = localization?.GetStrings(locale)?.ToDictionaryIgnoringDuplicates(k => k.Key, v => v.Value);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(strings)));
            args.Response = sender.Environment.CreateWebResourceResponse(stream.AsRandomAccessStream(), 200, "OK", "Content-Type: application/json");

            return Task.CompletedTask;
        }

        private async Task HandleBarcodeScanRequestAsync(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var barcode = null == ScanBarcodeCallback ? null : await ScanBarcodeCallback();

            if (null == barcode)
            {
                args.Response = sender.Environment.CreateWebResourceResponse(null, 204, "NO CONTENT", "Content-Type: application/json");
            }
            else
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(barcode)));
                args.Response = sender.Environment.CreateWebResourceResponse(stream.AsRandomAccessStream(), 200, "OK", "Content-Type: application/json");
            }

        }

        private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
        {
            var menunames = args.MenuItems.Select(m => m.Name).ToList();

            var menulist = args.MenuItems;

            for (int i = 0; i < menulist.Count; i++)
            {
                if (!s_AllowedContextMenuItems.Contains(menulist[i].Name))
                {
                    menulist.RemoveAt(i--);
                }
            }
        }

        public string? SanteDBMagic { get; set; }

        public IApplicationServiceContext? ApplicationServiceContext { get; set; }

        public Func<Task<string>>? ScanBarcodeCallback { get; set; }

        private string? _Version;

        private string? GetAssemblyVersion()
        {
            if (null != _Version)
            {
                return _Version;
            }
            _Version = typeof(SanteDBWebView2).Assembly.GetName().Version?.ToString();
            return _Version;
        }
    }
}
