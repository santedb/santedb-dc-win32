using SanteDB.Core.Configuration;
using SanteDB.DisconnectedClient.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.DisconnectedClient.Win32.ConfigurationProviders
{
    /// <summary>
    /// Initial configuration provider
    /// </summary>
    public class ShimGeneratorConfigurationProvider : IInitialConfigurationProvider
    {
        /// <summary>
        /// Provide information to the configuration for the SHIM
        /// </summary>
        public SanteDBConfiguration Provide(SanteDBConfiguration existing)
        {
            existing.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Insert(0, new TypeReferenceConfiguration(typeof(ShimGenerator)));
            return existing;
        }
    }
}
