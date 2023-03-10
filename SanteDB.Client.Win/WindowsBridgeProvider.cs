using SanteDB.Client.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.WinUI
{
    public class WindowsBridgeProvider : IAppletHostBridgeProvider
    {
        readonly string _BridgeScript;

        public WindowsBridgeProvider() {
            using var stream = typeof(WindowsBridgeProvider).Assembly.GetManifestResourceStream("SanteDB.Client.Win.lib.santedb-shim.js");

            if (null != stream)
            {
                using var sr = new StreamReader(stream);
                _BridgeScript = sr.ReadToEnd();
            }
        }

        public string GetBridgeScript()
        {
            return _BridgeScript;
        }
    }
}
