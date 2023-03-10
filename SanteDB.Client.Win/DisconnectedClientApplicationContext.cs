using SanteDB.Client.UserInterface;
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
    public class DisconnectedClientApplicationContext : ClientApplicationContextBase
    {
        public DisconnectedClientApplicationContext(string instanceName, IConfigurationManager configurationManager, MainWindow mainWindow)
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
                        HmacSecret = "@@SanteDB2021!&",
                    }
                }
            });

            DependencyServiceManager.AddServiceProvider(new WindowsInteractionProvider(mainWindow));
            DependencyServiceManager.AddServiceProvider(new WindowsBridgeProvider());
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
