using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.WinUI
{
    internal class SplashScreenTraceWriter : SanteDB.Core.Diagnostics.TraceWriter
    {
        readonly MainWindow m_MainWindow;

        public SplashScreenTraceWriter(MainWindow window) : base(EventLevel.Verbose, null, null)
        {
            m_MainWindow = window;
        }

        public override void TraceEventWithData(EventLevel level, string source, string message, object[] data)
        {
        }

        protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
        {
            m_MainWindow.ShowSplashStatusText(string.Format(format, args));
        }
    }
}
