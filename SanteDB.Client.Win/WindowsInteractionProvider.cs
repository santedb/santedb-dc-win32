using SanteDB.Client.UserInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.WinUI
{
    public class WindowsInteractionProvider : IUserInterfaceInteractionProvider
    {
        private static readonly string s_DefaultTaskIdentifier = string.Empty;

        readonly List<TaskStatus> _Statuses;

        public string ServiceName => "Windows Interaction Provider";

        readonly MainWindow m_MainWindow;

        public WindowsInteractionProvider(MainWindow mainWindow)
        {
            _Statuses = new ();
            m_MainWindow = mainWindow;
        }

        public void Alert(string message)
        {
            m_MainWindow.ShowAlert(message);
        }

        public bool Confirm(string message)
        {
            throw new NotSupportedException("Synchronous UI operations are not supported.");
        }

        public string Prompt(string message, bool maskEntry = false)
        {
            throw new NotSupportedException("Synchronous UI operations are not supported.");
        }

        public void SetStatus(string statusText, float progressIndicator)
            => SetStatus(s_DefaultTaskIdentifier, statusText, progressIndicator);

        public void SetStatus(string taskIdentifier, string statusText, float progressIndicator)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(statusText);
#endif

            if (progressIndicator > 0)
            {
                //Debugger.Break();
            }

            m_MainWindow.ShowSplashStatusText($"Starting SanteDB - {Math.Round(progressIndicator, 2)} :: {taskIdentifier}");
        }

        private class TaskStatus
        {
            public string TaskIdentifier { get; set; }
            public string StatusText { get; set; }
            public int Progress { get; set; }
            public DateTimeOffset CreatedAt { get; set; }

        }
    }
}
