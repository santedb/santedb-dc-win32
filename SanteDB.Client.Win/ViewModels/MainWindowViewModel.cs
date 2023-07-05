using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Win.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the title of the main window.
        /// </summary>
        [ObservableProperty, NotifyPropertyChangedFor(nameof(WindowTitle))]
        string? title;

        /// <summary>
        /// Gets or sets the URL that the browser's source is set to.
        /// </summary>
        [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayBrowser))]
        string? browserUrl;
        /// <summary>
        /// Gets or sets the magic value used in the user agent string to authenticate the browser session.
        /// </summary>
        [ObservableProperty, NotifyPropertyChangedFor(nameof(UserAgentString))]
        string? browserMagic;

        /// <summary>
        /// Gets a value to indicate whether the browser should be shown (i.e. the startup sequence is complete)
        /// </summary>
        public bool DisplayBrowser => !string.IsNullOrWhiteSpace(BrowserUrl);
        /// <summary>
        /// Gets the user agent string that the browser will use to make requests to the local instance.
        /// </summary>
        public string? UserAgentString => $"SanteDB-{BrowserMagic}";

        public string WindowTitle => Title ?? "SanteDB";

        [RelayCommand]
        private void GoBack()
        {

        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
        bool canGoBack;

        

        [RelayCommand]
        private async Task GoForward()
        {

        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(GoForwardCommand))]
        bool canGoForward;

        [RelayCommand]
        private async Task RefreshPage()
        {

        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RefreshPageCommand))]
        bool canRefreshPage;
    }
}
