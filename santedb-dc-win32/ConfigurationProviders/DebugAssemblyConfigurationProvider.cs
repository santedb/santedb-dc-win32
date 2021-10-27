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
    /// Initial configuration provider which enables debug assemblies
    /// </summary>
    public class DebugAssemblyConfigurationProvider : IInitialConfigurationProvider
    {
        /// <summary>
        /// Provide information to the configuration for the SHIM
        /// </summary>
        public SanteDBConfiguration Provide(SanteDBConfiguration existing)
        {
#if DEBUG
            existing.GetSection<ApplicationServiceContextConfigurationSection>().AllowUnsignedAssemblies = true;
#endif
            return existing;
        }
    }
}