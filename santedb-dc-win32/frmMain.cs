/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 * 
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
 * User: fyfej
 * Date: 2017-9-1
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SanteDB.DisconnectedClient.UI;
using System.IO;
#if !IE
using CefSharp.WinForms;
using CefSharp;
#endif
using SanteDB.Core.Diagnostics;

namespace SanteDB.DisconnectedClient.Win32
{
    public partial class frmDisconnectedClient : Form
    {

        private Tracer m_tracer = Tracer.GetTracer(typeof(frmDisconnectedClient));
        // Browser
#if IE
        public WebBrowser m_browser = null;
#else
        public ChromiumWebBrowser m_browser = null;
#endif 

        EventHandler<ApplicationProgressEventArgs> m_progressHandler;

        // Disconnected client ctor
        public frmDisconnectedClient(String url)
        {

            InitializeComponent();

            this.m_tracer.TraceInfo("Starting up browser interface to {0}", url);
            Action<ApplicationProgressEventArgs> updateUi = (e) =>
            {
                try
                {

                    lblStatus.Text = String.Format("{0} ({1:#0}%)", e.ProgressText, e.Progress * 100);
                    if (e.Progress >= 0 && e.Progress <= 1)
                    {
                        pgMain.Style = ProgressBarStyle.Continuous;
                        pgMain.Value = (int)(e.Progress * 100);
                    }
                    else
                    {
                        pgMain.Style = ProgressBarStyle.Marquee;

                    }
                }
                catch { }
            };

            this.m_progressHandler = (o, ev) =>
            {
                this.BeginInvoke(updateUi, ev);
                Application.DoEvents();
            };
            DcApplicationContext.ProgressChanged += this.m_progressHandler;

#if IE
            this.InitializeBrowser(url);
#else
            this.InitializeChromium(url);
#endif
        }


#if IE
        private void InitializeBrowser(String url)
        {


            this.m_browser = new WebBrowser();

#if !DEBUG
            this.m_browser.IsWebBrowserContextMenuEnabled = true;
#endif

            
            this.m_browser.ObjectForScripting = new AppletFunctionBridge(this);
            this.m_browser.Navigate(url);
            this.pnlMain.Controls.Add(this.m_browser);
            this.m_browser.Dock = DockStyle.Fill;
            this.m_browser.DocumentCompleted += M_browser_DocumentCompleted;
        }

        /// <summary>
        /// Document completed
        /// </summary>
        private void M_browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this.m_browser.Document.Window.Error += (w, we) =>
            {
                this.m_tracer.TraceError("{0}@{1}: {2} ", we.Url, we.LineNumber, we.Description);
            };
        }

        // Go dback
        public void Back()
        {
            this.m_browser.GoBack();
        }

        private void showDebugToolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.m_browser.ShowPropertiesDialog();
        }

        private void btnZoomWidth_Click(object sender, EventArgs e)
        {

            double zoomFactor = Double.Parse((sender as ToolStripMenuItem).Tag.ToString());
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.m_browser.GoBack();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            this.m_browser.GoForward();
        }
#else
        /// <summary>
        /// Initialize chromium
        /// </summary>
        private void InitializeChromium(string url)
        {


#if IE
#else
#endif
            this.m_browser = new ChromiumWebBrowser(url);
            this.m_browser.RequestHandler = new DisconnectedClientRequestHandler();

#if !DEBUG
            mnsTools.Visible = Program.Parameters?.Debug == true;
#endif
            this.m_browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            this.m_browser.JavascriptObjectRepository.Register("SanteDBApplicationService", new AppletFunctionBridge(this), false, BindingOptions.DefaultBinder);
            this.pnlMain.Controls.Add(this.m_browser);
            this.m_browser.Dock = DockStyle.Fill;
            this.m_browser.JsDialogHandler = new WinFormsDialogProvider();
            this.m_browser.DownloadHandler = new DownloadHandler();
        }

        // Go dback
        public void Back()
        {
            this.m_browser.Back();
        }

        private void showDebugToolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.m_browser.ShowDevTools();
        }

        private void btnZoomWidth_Click(object sender, EventArgs e)
        {

            double zoomFactor = Double.Parse((sender as ToolStripMenuItem).Tag.ToString());
            this.m_browser.SetZoomLevel(zoomFactor);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.m_browser.Back();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            this.m_browser.Forward();
        }
#endif
        private void frmDisconnectedClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            DcApplicationContext.Current.SetProgress("Shutting down...", 0);

#if IE
#else
#endif
            DcApplicationContext.Current.Stop();
            DcApplicationContext.ProgressChanged -= this.m_progressHandler;

        }



        private void btnRefresh_Click(object sender, EventArgs e)
        {
            this.m_browser.Reload();
        }

    }
}
