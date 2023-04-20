using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Newtonsoft.Json;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Win;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WinRT;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SanteDB.Client.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        SystemBackdropConfiguration? m_BackdropConfiguration;
        ISystemBackdropControllerWithTargets? m_BackdropController;
        WindowsSystemDispatcherQueueHelper m_QueueHelper;
        private bool m_FirstRender;
        private nint m_Hwnd;
        private WindowId m_WindowId;
        private AppWindow m_AppWindow;
        private string? m_Magic;

        private JsonSerializer m_JsonSerializer;

        private TracerOutputWindow? m_TracerWindow;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            m_FirstRender = true;
            this.InitializeComponent();

            if (MicaController.IsSupported())
            {
                SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
            }
            else if (DesktopAcrylicController.IsSupported())
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }

            this.Closed += MainWindow_Closed;

            m_JsonSerializer = new JsonSerializer();

            AppWindow.SetIcon("Assets\\santedb.ico");

            Vanara.PInvoke.DwmApi.DwmSetWindowAttribute<bool>(m_Hwnd, Vanara.PInvoke.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, true);
            
            this.Title = "SanteDB";

            this.ExtendsContentIntoTitleBar = true;

            this.SetTitleBar(TitleBarDragArea);

            m_TracerWindow = new TracerOutputWindow();
            m_TracerWindow.Activate();
        }


        private void CoreWebView2_HistoryChanged(Microsoft.Web.WebView2.Core.CoreWebView2 sender, object args)
        {
            BackButton.IsEnabled = sender.CanGoBack;
            ForwardButton.IsEnabled = sender.CanGoForward;
        }

        

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            m_TracerWindow?.Close();

            if (ApplicationServiceContext.IsRunning)
            {
                ApplicationServiceContext.Stop();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Browser.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            Browser.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Browser.Reload();
        }

        private void Browser_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (null != args.Exception)
            {
                throw args.Exception;
            }

            Browser.CoreWebView2.Settings.UserAgent = $"SanteDB-{m_Magic}";
            Browser.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            Browser.CoreWebView2.AddWebResourceRequestedFilter("*/_appservice/*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
            Browser.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            Browser.NavigationStarting += Browser_NavigationStarting;
            Browser.NavigationCompleted += Browser_NavigationCompleted;
            Browser.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            Browser.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            RefreshButton.IsEnabled = true;
#if DEBUG
            Browser.CoreWebView2.OpenDevToolsWindow();
#endif
        }

        private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
        {
            this.Title = sender.DocumentTitle;
            TitleBarText.Text = this.Title;
        }

        private async void CoreWebView2_WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            var uri = new Uri(args.Request.Uri);

            await Task.Yield();

            try
            {

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

        private Task HandleStringRequestAsync(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var localization = ApplicationServiceContext.GetService<ILocalizationService>();

            var uri = new Uri(args.Request.Uri);
            //TODO: Locale
            var locale = "en";

            var strings = localization?.GetStrings(locale)?.ToDictionaryIgnoringDuplicates(k=>k.Key, v=>v.Value);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(strings)));
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
            catch(Exception ex)
            {
                Debugger.Break();
            }

            return Task.CompletedTask;
        }

        private Task HandleStateRequestAsync(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var config = ApplicationServiceContext.GetService<IConfigurationManager>()?.GetSection<UpstreamConfigurationSection>();

            var devicecredential = config?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Device);
            var appcredential = config?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Application);

            var response = new Shared.AppServiceStateResponse
            {
                Version = GetAssemblyVersion(),
                Online = ApplicationServiceContext.IsRunning,
                Hdsi = ApplicationServiceContext.IsRunning,
                Ami = ApplicationServiceContext.IsRunning,
                ClientId = appcredential?.CredentialName,
                DeviceId = devicecredential?.CredentialName,
                Magic = ApplicationServiceContext.ActivityUuid.ToString(),
                Realm = config?.Realm?.DomainName
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response)));
            args.Response = sender.Environment.CreateWebResourceResponse(stream.AsRandomAccessStream(), 200, "OK", "Content-Type: application/json");

            return Task.CompletedTask;
        }


        private string? _Version;


        private string? GetAssemblyVersion()
        {
            if (null != _Version)
            {
                return _Version;
            }
            _Version = typeof(MainWindow).Assembly.GetName().Version?.ToString();
            return _Version;
        }

        private string? _Copyright;

        private string? GetAssemblyCopyright()
        {
            if (null == _Copyright)
            {
                _Copyright = typeof(MainWindow).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

            }

            return _Copyright;
        }

        private IApplicationServiceContext ApplicationServiceContext => SanteDB.Core.ApplicationServiceContext.Current;


        private async void Browser_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (m_FirstRender)
            {
                SplashStack.Visibility = Visibility.Collapsed;
                Browser.Visibility = Visibility.Visible;
                Browser.Focus(FocusState.Keyboard);
            }
        }

        private void Browser_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            
        }



        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {

            await AboutDialog.ShowAsync();

        }

        public void ShowSplashStatusText(string text)
        {
            if (null == this.DispatcherQueue)
            {
                return;
            }

            if (this.DispatcherQueue.HasThreadAccess)
            {
                SplashStatusText.Text = text;
                m_TracerWindow?.AppendText(text);
            }
            else
            {
                var s = text;
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    SplashStatusText.Text = s;
                    m_TracerWindow?.AppendText(text);
                });
            }

        }

        public void Start(string uri, string magicValue)
        {
            if (this.DispatcherQueue.HasThreadAccess)
            {
                m_Magic = magicValue;
                Browser.Source = new Uri(uri);
            }
            else
            {
                var u = new Uri(uri);
                var m = magicValue;
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    m_Magic = m;
                    Browser.Source = u;
                });
            }
        }

        public async void ShowAlert(string message)
        {
            if (this.DispatcherQueue.HasThreadAccess)
            {
                AlertDialogText.Text = message;
                await AlertDialog.ShowAsync();
            }
            else
            {
                var m = message;
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
                {
                    AlertDialogText.Text = m;
                    await AlertDialog.ShowAsync();
                });
            }
        }
    }
}
