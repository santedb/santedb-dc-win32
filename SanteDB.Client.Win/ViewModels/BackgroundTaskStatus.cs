using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Win.ViewModels
{
    internal partial class BackgroundTaskStatus : ObservableObject
    {
        [ObservableProperty]
        string? text;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIndeterminate))]
        float progress;

        public bool IsIndeterminate => this.Progress < 0;

        [ObservableProperty]
        bool dismissed = false;

        public string? TaskIdentifier { get; set; }

        /// <summary>
        /// Time the task was added to account for stale tasks.
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; }
    }
}
