/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: trevor
 * Date: 2023-6-30
 */
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using SanteDB.Client.WinUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Vanara.Extensions.Reflection;

namespace SanteDB.Client.Win.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<BackgroundTaskStatus> BackgroundTasks { get; } = new();

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

        public string WindowTitle
        {
            get
            {
                var title = Title;
                if (string.IsNullOrEmpty(title))
                {
                    return "SanteDB";
                }
                else if (!title.Contains("santedb", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    return $"{title} - SanteDB";
                }
                else
                {
                    return title;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void GoBack()
        {
            Browser.GoBack();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
        bool canGoBack = false;



        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void GoForward()
        {
            Browser.GoForward();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(GoForwardCommand))]
        bool canGoForward = false;

        [RelayCommand(CanExecute = nameof(CanRefreshPage))]
        private void RefreshPage()
        {
            Browser.Reload();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RefreshPageCommand))]
        bool canRefreshPage = false;

        [ObservableProperty]
        string backgroundTasksButtonText;

        internal MainWindow MainWindow { get; set; }
        internal WebView2 Browser { get; set; }

        public MainWindowViewModel()
        {
            BackgroundTasks.CollectionChanged += BackgroundTasks_CollectionChanged;
        }

        private void BackgroundTasks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (null == BackgroundTasks || BackgroundTasks.Count == 0)
            {
                BackgroundTasksButtonText = "No Pending Operations";
            }
            else if (BackgroundTasks.Count == 1)
            {
                BackgroundTasksButtonText = "1 Pending Operation";
            }
            else
            {
                BackgroundTasksButtonText = $"{BackgroundTasks.Count} Pending Operations";
            }

            MainWindow.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () => MainWindow.SetDragRegionForCustomTitleBar());
        }
    }
}
