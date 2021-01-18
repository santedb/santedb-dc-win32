using MohawkCollege.Util.Console.Parameters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.DisconnectedClient.Win32
{
    /// <summary>
    /// Represents console parameters for this particular instance
    /// </summary>
    public class ConsoleParameters
    {
        // <summary>
        /// When true, parameters should be shown
        /// </summary>
        [Description("Shows help and exits")]
        [Parameter("?")]
        [Parameter("help")]
        public bool ShowHelp { get; set; }

        /// <summary>
        /// Reset the service installation
        /// </summary>
        [Description("Resets the configuration of this WWW instance to default")]
        [Parameter("reset")]
        public bool Reset { get; set; }

        /// <summary>
        /// Set the application name
        /// </summary>
        [Description("Sets the identity of the application (for OAUTH) for this instance")]
        [Parameter("appname")]
        public String ApplicationName { get; set; }

        /// <summary>
        /// The application secret
        /// </summary>
        [Description("Sets the secret of the application (for OAUTH) for this instance")]
        [Parameter("appsecret")]
        public String ApplicationSecret { get; set; }

        /// <summary>
        /// Debug mode
        /// </summary>
        [Description("Start in Debug Mode")]
        [Parameter("debug")]
        public bool Debug { get; internal set; }
    }
}
