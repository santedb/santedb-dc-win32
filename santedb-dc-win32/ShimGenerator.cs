using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.DisconnectedClient.Win32
{
    public class ShimGenerator : UI.Services.IShimGenerator
    {
        /// <summary>
        /// Get the shim methods from this object
        /// </summary>
        public string GetShimMethods()
        {
            using (StringWriter tw = new StringWriter())
            {
                
                // Read the static shim
                using (StreamReader shim = new StreamReader(typeof(ShimGenerator).Assembly.GetManifestResourceStream("SanteDB.DisconnectedClient.Win32.lib.shim.js")))
                    tw.Write(shim.ReadToEnd());
                return tw.ToString();
            }
        }
    }
}
