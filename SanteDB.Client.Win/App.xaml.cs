using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.PushNotifications;
using SanteDB.Client.Configuration;
using SanteDB.Client.Rest;
using SanteDB.Client.Shared;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SanteDB.Client.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        private void App_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (AppNotificationManager.IsSupported())
            {
                AppNotificationManager.Default.NotificationInvoked += App_NotificationInvoked;
                AppNotificationManager.Default.Register();
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => AppNotificationManager.Default.Unregister();
            }

            var commandline = Environment.GetCommandLineArgs();

            m_window = new MainWindow();
            m_window.Activate();

            await Task.Run(async () =>
            {
                await Task.Yield();
                var splashwriter = new SplashScreenTraceWriter(m_window);
                SanteDB.Core.Diagnostics.Tracer.AddWriter(splashwriter, System.Diagnostics.Tracing.EventLevel.Verbose);
                m_window.ShowSplashStatusText("Starting SanteDB");

                Directory.GetFiles(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "Sante*.dll").ToList().ForEach(itm =>
                {
                    try
                    {
                        m_window.ShowSplashStatusText(string.Format("Loading reference assembly {0}...", itm));
                        AssemblyLoadContext.Default.LoadFromAssemblyPath(itm);

                    }
                    catch (Exception e)
                    {
                        m_window.ShowSplashStatusText(string.Format("Error loading assembly {0}: {1}", itm, e));
                    }
                });

                try
                {
                    SQLitePCL.Batteries_V2.Init();
                    SqliteConnection.ClearAllPools(); //Force-load sqlite.
                }
                catch
                {

                }

                try
                {
                    var applicationidentity = new SecurityApplication
                    {
                        Key = Guid.Parse("feeca9f3-805e-4be9-a5c7-30e6e495939b"),
                        //ApplicationSecret = Parameters.ApplicationSecret ?? "FE78825ADB56401380DBB406411221FD"
                        //Name = Parameters.ApplicationName ?? "org.santedb.disconnected_client.win32"
                        ApplicationSecret = "FE78825ADB56401380DBB406411221FD",
                        Name = "org.santedb.disconnected_client.win32"
                    };

                    var directoryprovider = new Shared.LocalAppDirectoryProvider();

                    SanteDB.Client.Batteries.ClientBatteries.Initialize(directoryprovider.GetDataDirectory(), directoryprovider.GetConfigDirectory(), new Configuration.Upstream.UpstreamCredentialConfiguration()
                    {
                        CredentialType = Configuration.Upstream.UpstreamCredentialType.Application,
                        CredentialName = applicationidentity.Name,
                        CredentialSecret = applicationidentity.ApplicationSecret
                    });

                    AppDomain.CurrentDomain.SetData(RestServiceInitialConfigurationProvider.BINDING_BASE_DATA, "http://127.0.0.1:9200");

                    IConfigurationManager configmanager = null;

                    if (directoryprovider.IsConfigFilePresent())
                    {
                        configmanager = new FileConfigurationService(directoryprovider.GetConfigFilePath(), isReadonly: true);
                    }
                    else
                    {
                        configmanager = new InitialConfigurationManager(SanteDBHostType.Client, "DEFAULT", directoryprovider.GetConfigFilePath());
                    }

                    //var configmanager = new SanteDB.Client.Batteries.Configuration.DefaultDcdrConfigurationProvider();

                    var context = new WindowsApplicationContext("DEFAULT", configmanager, m_window);

                    ServiceUtil.Start(Guid.NewGuid(), context);

                    var magic = context.ActivityUuid.ToByteArray().HexEncode();

                    splashwriter.TraceInfo(string.Empty, string.Empty);

                    SanteDB.Core.Diagnostics.Tracer.RemoveWriter(splashwriter);

                    var starturl = configmanager switch
                    {
                        InitialConfigurationManager => "http://127.0.0.1:9200/#!/config/initialSettings",
                        _ => "http://127.0.0.1:9200/#!/"
                    };

                    m_window.Start(starturl, magic);
                }
                catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                {
                    Debugger.Break();
                }

            });


        }

        private MainWindow m_window;
    }
}
