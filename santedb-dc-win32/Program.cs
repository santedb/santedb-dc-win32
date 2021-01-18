using CefSharp;
using CefSharp.WinForms;
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.Security;
using SanteDB.DisconnectedClient.Ags;
using SanteDB.DisconnectedClient.Security;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.DisconnectedClient.Win32
{
    static class Program
    {
        // Trusted certificates
        private static List<String> s_trustedCerts = new List<string>();

        /// <summary>
        /// Console parameters
        /// </summary>
        public static ConsoleParameters Parameters { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            // Output main header
            var parser = new ParameterParser<ConsoleParameters>();
            var parms = parser.Parse(args);

            // Output copyright info
            var entryAsm = Assembly.GetEntryAssembly();

            AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
            {
                string pAsmName = e.Name;
                if (pAsmName.Contains(","))
                    pAsmName = pAsmName.Substring(0, pAsmName.IndexOf(","));

                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => e.Name == a.FullName) ??
                    AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => pAsmName == a.GetName().Name);
                return asm;
            };


            try
            {

                
                // Security Application Information
                var applicationIdentity = new SecurityApplication()
                {
                    Key = Guid.Parse("feeca9f3-805e-4be9-a5c7-30e6e495939b"),
                    ApplicationSecret = parms.ApplicationSecret ?? "FE78825ADB56401380DBB406411221FD",
                    Name = parms.ApplicationName ?? "org.santedb.disconnected_client.win32"
                };

                // Setup basic parameters
                String[] directory = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SanteDB", "dc-win32"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanteDB", "dc-win32")
                };

                foreach (var dir in directory)
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                ServicePointManager.DefaultConnectionLimit = 2;
                ServicePointManager.MaxServicePointIdleTime = 100;
                TokenValidationManager.SymmetricKeyValidationCallback += (o, k, i) =>
                {
                    return MessageBox.Show(String.Format("Trust issuer {0} with symmetric key?", i), "Token Validation Error", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes;
                };
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, error) =>
                {
                    if (certificate == null || chain == null)
                        return false;
                    else
                    {
                        var valid = s_trustedCerts.Contains(certificate.Subject);
                        if (!valid && (chain.ChainStatus.Length > 0 || error != SslPolicyErrors.None))
                            if (MessageBox.Show(String.Format("The remote certificate is not trusted. The error was {0}. The certificate is: \r\n{1}\r\nWould you like to temporarily trust this certificate?", error, certificate.Subject), "Certificate Error", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                                return false;
                            else
                                s_trustedCerts.Add(certificate.Subject);

                        return true;
                        //isValid &= chain.ChainStatus.Length == 0;
                    }
                };


                if (parms.ShowHelp)
                    parser.WriteHelp(Console.Out);
                else if (parms.Reset && MessageBox.Show("Are you sure you want to wipe all your data and configuration for the Disconnected Client?", "Confirm Reset", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanteDB", "dc-win32");
                    var cData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SanteDB", "dc-win32");
                    if (Directory.Exists(appData)) Directory.Delete(cData, true);
                    if (Directory.Exists(appData)) Directory.Delete(appData, true);
                    MessageBox.Show("Environment Reset Successful");
                    return;
                }
                else // RUN THE SERVICE
                {

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    frmDisconnectedClient frmMain = null;
                    frmSplash splash = new frmSplash();
                    splash.Show();

                    if (!DcApplicationContext.StartContext(new WinFormsDialogProvider(), "dc-win32", applicationIdentity, Core.SanteDBHostType.Client))
                    {
                        if (!DcApplicationContext.StartTemporary(new WinFormsDialogProvider(), "dc-win32", applicationIdentity, Core.SanteDBHostType.Client))
                        {
                            MessageBox.Show("There was an error starting up the Disconnected Client. Please see logs in %localappdata%\\log for more information");
                            Cef.Shutdown();
                            Application.Exit();
                            Environment.Exit(666);
                            return;
                        }
                        else
                        {
                            var start = DateTime.Now;
                            var ags = DcApplicationContext.Current.GetService<AgsService>();
                            while (!ags.IsRunning && DateTime.Now.Subtract(start).TotalSeconds < 20 && splash.Visible)
                                Application.DoEvents();

                            if (ags.IsRunning)
                                frmMain = new frmDisconnectedClient("http://127.0.0.1:9200/");
                            else return;
                        }
                    }
                    else
                    {
                        var start = DateTime.Now;
                        var ags = DcApplicationContext.Current.GetService<AgsService>();
                        while (!ags.IsRunning && DateTime.Now.Subtract(start).TotalSeconds < 20 && splash.Visible)
                            Application.DoEvents();

                        frmMain = new frmDisconnectedClient("http://127.0.0.1:9200/");
                    }
                    splash.Close();

                    Application.Run(frmMain);

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("FATAL ERROR ON STARTUP: {0}", e.Message), "Error");
                Cef.Shutdown();
                Application.Exit();
                Environment.Exit(996);
            }
            finally
            {
                Cef.Shutdown();
            }

        }
    }
}
