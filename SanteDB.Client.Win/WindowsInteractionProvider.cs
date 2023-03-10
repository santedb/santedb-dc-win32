using SanteDB.Client.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.WinUI
{
    public class WindowsInteractionProvider : IUserInterfaceInteractionProvider
    {
        public string ServiceName => "Windows Interaction Provider";

        readonly MainWindow m_MainWindow;

        public WindowsInteractionProvider(MainWindow mainWindow)
        {
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
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(statusText);
#endif
            m_MainWindow.ShowSplashStatusText(statusText);
        }
    }
}
