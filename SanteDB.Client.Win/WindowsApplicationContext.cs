/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: trevor
 * Date: 2023-4-19
 */
using SanteDB.Client.UserInterface;
using SanteDB.Client.Win;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.WinUI
{
    public class WindowsApplicationContext : ClientApplicationContextBase
    {
        public WindowsApplicationContext(string instanceName, IConfigurationManager configurationManager, MainWindow mainWindow)
            : base(Core.SanteDBHostType.Client, instanceName, configurationManager)
        {

#if DEBUG
            configurationManager.GetSection<ApplicationServiceContextConfigurationSection>().AllowUnsignedAssemblies = true;
#endif

            configurationManager.Configuration.AddSection<SecurityConfigurationSection>(new SecurityConfigurationSection
            {
                Signatures = new List<SecuritySignatureConfiguration>
                {
                    new SecuritySignatureConfiguration
                    {
                        Algorithm = SignatureAlgorithm.HS256,
                        //HmacSecret = "@@SanteDB2021!&",
                        HmacSecret = "QUFCN0ZDQjk2ODU1MDkwODIzNTIxREM2OEIxRTA5RDgzMUQ3MkY1RTk2MzAzNzRCMjU0ODdBMUFCQzUzRDAzMjYyMjQ1REE0RDA1MUMyRkMzOEVGMkNCMjBCM0FDQzRBRjE2MTdEQzUwQ0U4NDJGOUFFOEIzMjQzRTQ2MUNCMTE="
                    }
                }
            });

            DependencyServiceManager.AddServiceProvider(new WindowsInteractionProvider(mainWindow));
            DependencyServiceManager.AddServiceProvider(new WindowsBridgeProvider());
            DependencyServiceManager.AddServiceProvider(new WindowsOperatingSystemInfoService());
        }

        public override void Start()
        {
            base.Start();
        }

        protected override void OnRestartRequested(object sender)
        {
            
        }
    }
}
