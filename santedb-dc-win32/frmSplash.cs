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
using SanteDB.DisconnectedClient.UI;
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

namespace SanteDB.DisconnectedClient.Win32
{
    public partial class frmSplash : Form
    {
        // HACK: Windows 8 doesn't like IPC so we're going to use a timer
        private Timer m_timer = new Timer();
        private ApplicationProgressEventArgs m_lastArgs;

        EventHandler<ApplicationProgressEventArgs> m_progressHandler;


        public frmSplash()
        {
            InitializeComponent();
            this.m_timer.Interval = 1000;
            this.m_timer.Start();
        }

        private void frmSplash_Load(object sender, EventArgs e)
        {
            lblVersion.Text = String.Format("v.{0} - {1}", Assembly.GetEntryAssembly().GetName().Version, Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

            this.m_timer.Tick += (o, ev) =>
            {
                if(this.m_lastArgs != null)
                    lblProgress.Text = String.Format("{0} ({1:#0}%)", this.m_lastArgs.ProgressText, this.m_lastArgs.Progress * 100);
            };

            this.m_progressHandler = (o, ev) =>
            {
                this.m_lastArgs = ev;
            };
            DcApplicationContext.ProgressChanged += this.m_progressHandler;
        }

        private void frmSplash_FormClosing(object sender, FormClosingEventArgs e)
        {
            DcApplicationContext.ProgressChanged -= this.m_progressHandler;
            this.m_timer.Stop();
        }
    }
}
