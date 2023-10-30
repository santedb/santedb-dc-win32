using Microsoft.UI;
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
using SanteDB.Client.Win.ViewModels;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SanteDB.Client.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool m_FirstRender;
        private nint m_Hwnd;
        private WindowId m_WindowId;
        private string? m_Magic;
        private bool m_IsStarted;


        private JsonSerializer m_JsonSerializer;

        private TracerOutputWindow? m_TracerWindow;

        private UISettings m_UISettings;

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool m_IsWindowDeactivated;

        private SolidColorBrush m_CaptionButtonForegroundBrush;
        private SolidColorBrush m_CaptionButtonDisabledForegroundBrush;
        /// <summary>
        /// Gets the current <see cref="IApplicationServiceContext"/> that is running.
        /// </summary>
        private IApplicationServiceContext ApplicationServiceContext => SanteDB.Core.ApplicationServiceContext.Current;

        /// <summary>
        /// Key strings for the webview2 context menu. Items not present in this list are excluded from the context menu. This prevents things like search with bing, translate, etc from appearing.
        /// </summary>
        public static string[] s_AllowedContextMenuItems = new[] { "back", "forward", "reload", "print", "emoji", "undo", "redo", "cut", "copy", "paste", "pasteAndMatchStyle", "selectAll" };

        public MainWindow()
        {
            m_FirstRender = true;
            m_IsStarted = false;

            this.InitializeComponent();

            ViewModel.MainWindow = this;
            ViewModel.Browser = Browser;
            SetupTimers();

            m_WindowId = AppWindow.Id;
            m_Hwnd = Win32Interop.GetWindowFromWindowId(m_WindowId);

            if (MicaController.IsSupported())
            {
                SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
            }
            else if (DesktopAcrylicController.IsSupported())
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }

            this.Closed += MainWindow_Closed;
            this.Activated += MainWindow_Activated;

            m_JsonSerializer = new JsonSerializer();

            AppWindow.SetIcon("Assets\\santedb.ico");

            //this.Title = "SanteDB";

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titlebar = AppWindow.TitleBar;
                titlebar.ExtendsContentIntoTitleBar = true;
                TitleBar.Loaded += TitleBar_Loaded;
                TitleBar.SizeChanged += TitleBar_SizeChanged;
                titlebar.PreferredHeightOption = TitleBarHeightOption.Tall;
                titlebar.ButtonBackgroundColor = Colors.Transparent;
                titlebar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
            else
            {
                //TODO: Unsupported customization
                TitleBarText.Visibility = Visibility.Collapsed;
            }

            m_UISettings = new UISettings();
            m_UISettings.ColorValuesChanged += UISettings_ColorValuesChanged;
            m_UISettings.TextScaleFactorChanged += UISettings_TextScaleFactorChanged;

            m_CaptionButtonDisabledForegroundBrush = (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];
            m_CaptionButtonForegroundBrush = (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];

            //Subclass the win32 window to handle specific window messages.
            Vanara.PInvoke.ComCtl32.SetWindowSubclass(m_Hwnd, WindowSubclassProcedure, 0x1, nint.Zero);

            //m_TracerWindow = new TracerOutputWindow();
            //m_TracerWindow.Activate();

            //Fix for incorrect foregrounds on enabled/disabled controls in the title bar.
            ViewModel.PropertyChanged += (s, e) =>
            {
                SetControlStyles();
            };
        }

        /// <summary>
        /// Gets the ViewModel that is responsible for handling this window.
        /// </summary>
        internal MainWindowViewModel ViewModel { get; } = new();

        internal DispatcherTimer _BackgroundTaskTimer;

        private void SetupTimers()
        {
            _BackgroundTaskTimer = new DispatcherTimer();
            _BackgroundTaskTimer.Interval = TimeSpan.FromSeconds(5);
            _BackgroundTaskTimer.Tick += BackgroundTaskTimer_Tick;
            _BackgroundTaskTimer.Start();

        }

        private void BackgroundTaskTimer_Tick(object? sender, object e)
        {
            if (ViewModel.BackgroundTasks.Count > 0)
            {
                var lsttoremove = new System.Collections.Generic.List<BackgroundTaskStatus>(ViewModel.BackgroundTasks.Count);
                var now = DateTimeOffset.UtcNow;

                foreach (var task in ViewModel.BackgroundTasks)
                {
                    if (((now - task.LastUpdated).TotalSeconds >= 10)
                        || task.Progress >= 100f)
                    {
                        lsttoremove.Add(task);
                    }
                }

                foreach(var remove in lsttoremove)
                {
                    ViewModel.BackgroundTasks.Remove(remove);
                }
            }
        }

        /// <summary>
        /// WNDPROC for subclassed window 
        /// </summary>
        /// <remarks>This method must be static to pass a pointer for a callback.</remarks>
        /// <param name="hWnd">Window handle which the message applies to.</param>
        /// <param name="uMsg">The message type</param>
        /// <param name="wParam">The WPARAM parameter</param>
        /// <param name="lParam">The LPARAM parameter</param>
        /// <param name="uIdSubclass">The index of the subclass defined in the <see cref="Vanara.PInvoke.ComCtl32.SetWindowSubclass(HWND, ComCtl32.SUBCLASSPROC, nuint, nint)"/> call.</param>
        /// <param name="dwRefData">The reference data of the subclass defined in the <see cref="Vanara.PInvoke.ComCtl32.SetWindowSubclass(HWND, ComCtl32.SUBCLASSPROC, nuint, nint)"/> call.</param>
        /// <returns><c>IntPtr.Zero</c> on success, or an error code.</returns>
        private static nint WindowSubclassProcedure(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
        {
            //Note: This method is static so that it will be fixed in memory and not relocated. If it's not static, prepare for System.ExecutionEngineExceptions!

            var msg = (User32.WindowMessage)uMsg;

            switch (msg)
            {
                case User32.WindowMessage.WM_SIZING:
                    var rect = Marshal.PtrToStructure<RECT>(lParam); //Get a reference to the rect supplied in lParam.

                    //Check the width, and adjust accordingly
                    rect.Width = Math.Max(rect.Width, 800);
                    rect.Height = Math.Max(rect.Height, 560);

                    //Copy the adjusted structure back to the memory location.
                    Marshal.StructureToPtr(rect, lParam, fDeleteOld: false); //fDeleteOld is false because Windows provides the structure for us. This will not leak memory
                    break;
                default: //Delegate to the default proc.
                    return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
            }

            return nint.Zero;
        }

        private void UISettings_TextScaleFactorChanged(UISettings sender, object args)
        {
            SetDragRegionForCustomTitleBar();
        }

        private void UISettings_ColorValuesChanged(UISettings sender, object args)
        {
            UpdateRuntimeBrushes();
        }

        /// <summary>
        /// Updates our cached brush values. This is usually triggered when the user updates the theme in Windows and the first time we go to paint.
        /// </summary>
        private void UpdateRuntimeBrushes()
        {
            if (this.DispatcherQueue.HasThreadAccess != true)
            {
                this.DispatcherQueue.TryEnqueue(UpdateRuntimeBrushes);
            }
            else
            {
                m_CaptionButtonDisabledForegroundBrush = (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];

                if (m_IsWindowDeactivated)
                {
                    m_CaptionButtonForegroundBrush = m_CaptionButtonDisabledForegroundBrush;
                }
                else
                {
                    m_CaptionButtonForegroundBrush = (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];

                }

                if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar)
                {
                    AppWindow.TitleBar.ButtonForegroundColor = m_CaptionButtonForegroundBrush.Color;
                    AppWindow.TitleBar.ButtonInactiveForegroundColor = m_CaptionButtonDisabledForegroundBrush.Color;
                }

                SetControlStyles(); //Force update the style on the title buttons.
            }
        }

        private void SetControlStyles()
        {
            AboutButton.Foreground = m_CaptionButtonForegroundBrush;
            TitleBarText.Foreground = m_CaptionButtonForegroundBrush;


            BackButton.Foreground = ViewModel.CanGoBack ? m_CaptionButtonForegroundBrush : m_CaptionButtonDisabledForegroundBrush;
            ForwardButton.Foreground = ViewModel.CanGoForward ? m_CaptionButtonForegroundBrush : m_CaptionButtonDisabledForegroundBrush;
            RefreshButton.Foreground = ViewModel.CanRefreshPage ? m_CaptionButtonForegroundBrush : m_CaptionButtonDisabledForegroundBrush;
            BackgroundOperationsButton.Foreground = m_CaptionButtonForegroundBrush;

        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_IsWindowDeactivated = args.WindowActivationState == WindowActivationState.Deactivated;
            UpdateRuntimeBrushes();
        }

        private void TitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDragRegionForCustomTitleBar();
        }

        private void TitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            SetDragRegionForCustomTitleBar();
        }

        /// <summary>
        /// Gets the scaling factor for windows. We cannot cache this since the user might adjust the scaling factor or drag our window to a monitor with different scaling.
        /// </summary>
        /// <returns></returns>
        private (double scaleX, double scaleY) GetScalingFactor()
        {
            var displayarea = DisplayArea.GetFromWindowId(m_WindowId, DisplayAreaFallback.Primary);

            if (null == displayarea)
            {
                //TODO: Log this
                return (1d, 1d);
            }

            var monitorhwnd = Win32Interop.GetMonitorFromDisplayId(displayarea.DisplayId);

            var result = Vanara.PInvoke.SHCore.GetDpiForMonitor((HMONITOR)monitorhwnd, SHCore.MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpix, out var dpiy);

            if (!result.Succeeded)
            {
                return (1d, 1d);
            }

            if (dpiy <= 0 || dpiy == double.NegativeZero)
            {
                dpiy = dpix;
            }

            double scalefactorpercent(uint dpi) => ((uint)(((long)dpi * 100 + (96 >> 1)) / 96)) / 100.0;

            return (scalefactorpercent(dpix), scalefactorpercent(dpiy));
        }

        /// <summary>
        /// Calculates the real sizes of the drag regions and updates the window's drag regions acordingly.
        /// </summary>
        internal void SetDragRegionForCustomTitleBar()
        {
            if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                var scalingfactor = GetScalingFactor();

                double scaleX(double value) => value * scalingfactor.scaleX;

                double scaleY(double value) => value * scalingfactor.scaleY;

                TitleBarLeftInset.Width = new GridLength(AppWindow.TitleBar.LeftInset / scalingfactor.scaleX);
                TitleBarRightInset.Width = new GridLength(AppWindow.TitleBar.RightInset / scalingfactor.scaleX);

                RectInt32 dragrectl, dragrectc, dragrectmr, dragrectr;

                int height = (int)scaleY(TitleBar.ActualHeight);

                dragrectl.X = (int)(
                    scaleX(TitleBarLeftInset.ActualWidth) +
                    scaleX(TitleBarCommandBarArea.ActualWidth)
                    );
                dragrectl.Y = 0;
                dragrectl.Width = (int)(scaleX(TitleBarLeftDragArea.ActualWidth));
                dragrectl.Height = height;

                dragrectc.X = (int)(
                        scaleX(TitleBarLeftInset.ActualWidth) +
                        scaleX(TitleBarCommandBarArea.ActualWidth) +
                        scaleX(TitleBarLeftDragArea.ActualWidth)
                    );
                dragrectc.Y = 0;
                dragrectc.Width = (int)(scaleX(TitleBarCaption.ActualWidth));
                dragrectc.Height = height;

                dragrectmr.X = (int)(
                    scaleX(TitleBarLeftInset.ActualWidth) +
                    scaleX(TitleBarCommandBarArea.ActualWidth) +
                    scaleX(TitleBarLeftDragArea.ActualWidth) +
                    scaleX(TitleBarCaption.ActualWidth)
                    );

                dragrectmr.Y = 0;
                dragrectmr.Width = (int)(scaleX(TitleBarRightMiddleDragArea.ActualWidth));
                dragrectmr.Height = height;

                dragrectr.X = (int)(
                    scaleX(TitleBarLeftInset.ActualWidth) +
                    scaleX(TitleBarCommandBarArea.ActualWidth) +
                    scaleX(TitleBarLeftDragArea.ActualWidth) +
                    scaleX(TitleBarCaption.ActualWidth) +
                    scaleX(TitleBarRightMiddleDragArea.ActualWidth)
                    );
                dragrectr.Y = 0;
                dragrectr.Width = (int)(scaleX(TitleBarRightDragArea.ActualWidth));
                dragrectr.Height = height;

                var dragrectangles = new[] { dragrectl, dragrectc, dragrectmr, dragrectr };
                AppWindow.TitleBar.SetDragRectangles(dragrectangles);
            }
        }

        private void CoreWebView2_HistoryChanged(Microsoft.Web.WebView2.Core.CoreWebView2 sender, object args)
        {
            ViewModel.CanGoBack = sender.CanGoBack;
            ViewModel.CanGoForward = sender.CanGoForward;

            

            //BackButton.IsEnabled = sender.CanGoBack;
            //ForwardButton.IsEnabled = sender.CanGoForward;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            Vanara.PInvoke.ComCtl32.RemoveWindowSubclass(m_Hwnd, WindowSubclassProcedure, 0x1);

            m_TracerWindow?.Close();

            if (ApplicationServiceContext.IsRunning)
            {
                ApplicationServiceContext.Stop();
            }
        }

        //private void BackButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ViewModel.GoBackCommand.Execute(sender);
        //    //Browser.GoBack();
        //}

        //private void ForwardButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ViewModel.GoForwardCommand.Execute(sender);
        //    //Browser.GoForward();
        //}

        //private void RefreshButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ViewModel.RefreshPageCommand.Execute(sender);
        //    //Browser.Reload();
        //}

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
            Browser.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;

            ViewModel.CanRefreshPage = true;

#if DEBUG
            Browser.CoreWebView2.OpenDevToolsWindow();
#endif
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

        private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
        {
            ViewModel.Title = sender.DocumentTitle;
            //this.Title = sender.DocumentTitle;
            //TitleBarText.Text = this.Title;
            SetDragRegionForCustomTitleBar();
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

            var strings = localization?.GetStrings(locale)?.ToDictionaryIgnoringDuplicates(k => k.Key, v => v.Value);

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
            catch (Exception ex)
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

        private string? GetAboutDialogTitle()
            => "About SanteDB for Windows®️";

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

        private void BackgroundOperationsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {

            await AboutDialog.ShowAsync();

        }

        private Flyout GetBackgroundTaskFlyout()
        {

            var flyout = new Microsoft.UI.Xaml.Controls.Flyout()
            {
                ShowMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowMode.Transient,
                AreOpenCloseAnimationsEnabled = true
            };

            flyout.Opening += Flyout_Opening;
            flyout.Closed += Flyout_Closed;

            var ir = new Microsoft.UI.Xaml.Controls.ItemsRepeater()
            {
                MinWidth = 300,
                MaxWidth = 500,
                ItemsSource = ViewModel.BackgroundTasks,
                ItemTemplate = WindowGrid.Resources["BackgroundTaskStatusItemTemplate"] as DataTemplate
            };

            

            ir.Layout = new Microsoft.UI.Xaml.Controls.StackLayout()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10
            };

            flyout.Content = ir;

            return flyout;

        }

        private void Flyout_Closed(object? sender, object e)
        {
            //TODO: Clean up the flyout
            if (sender is Flyout flyout)
            {
                (flyout as IDisposable)?.Dispose();
            }
            BackgroundOperationsButton.Flyout = GetBackgroundTaskFlyout();
        }

        public void SetStatus(string? taskIdentifier, string? text, float? progress)
        {
            if (!m_IsStarted)
            {
                if (null != text && taskIdentifier == nameof(DependencyServiceManager))
                {
                    ShowSplashStatusText(text!, progress);
                }
            }

            SetBackgroundTask(taskIdentifier, text, progress);
        }

        public void ShowSplashStatusText(string text, float? progress = null)
        {
            if (null == this.DispatcherQueue)
            {
                return;
            }

            void settextlocal(string t, float? progress = null)
            {
                SplashStatusText.Text = t;
                m_TracerWindow?.AppendText(t);

                if (progress > 0)
                {
                    StartupProgress.IsIndeterminate = false;
                    StartupProgress.Value = progress.Value * 100;
                }
            }

            if (this.DispatcherQueue.HasThreadAccess)
            {
                settextlocal(text, progress);
            }
            else
            {
                var s = text;
                var p = progress;
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    settextlocal(s, p);
                });
            }

        }

        public void SetBackgroundTask(string? taskIdentifier, string? text, float? progress = null)
        {
            if (null == this.DispatcherQueue)
            {
                return;
            }

            void settask(string? ti, string? t, float? p)
            {
                var task = ViewModel.BackgroundTasks.FirstOrDefault(bg => bg.TaskIdentifier == ti);

                if (null != p)
                {
                    p *= 100f;
                }
                else
                {
                    p = -1;
                }

                if (null == task)
                {
                    task = new()
                    {
                        TaskIdentifier = ti,
                        Text = t,
                        Progress = p.Value,
                        Dismissed = false,
                        LastUpdated = DateTimeOffset.UtcNow
                    };

                    ViewModel.BackgroundTasks.Add(task);
                }
                else
                {
                    task.Text = t;
                    task.Progress = p.Value;
                    task.LastUpdated = DateTimeOffset.UtcNow;
                }
            };

            if (DispatcherQueue.HasThreadAccess)
            {
                settask(taskIdentifier, text, progress);
            }
            else
            {
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    settask(taskIdentifier, text, progress);
                });
            }
        }

        public void Start(string uri, string magicValue)
        {
            if (this.DispatcherQueue.HasThreadAccess)
            {
                m_Magic = magicValue;
                Browser.Source = new Uri(uri);
                m_IsStarted = true;
            }
            else
            {
                var u = new Uri(uri);
                var m = magicValue;
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    m_Magic = m;
                    Browser.Source = u;
                    m_IsStarted = true;
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

        private void Flyout_Opening(object? sender, object e)
        {
            if (sender is Flyout flyout)
            {
                if (flyout.Content is FrameworkElement fe)
                {
                    fe.RequestedTheme = BackgroundOperationsButton.RequestedTheme;
                }
            }
        }
    }
}
