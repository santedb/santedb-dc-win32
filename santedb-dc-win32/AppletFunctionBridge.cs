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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.DisconnectedClient.Win32
{

    /// <summary>
    /// Application function bridge
    /// </summary>
#if IE
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
#endif
    public class AppletFunctionBridge
    {

        /// <summary>
        /// Convert object to JSON.
        /// </summary>
        private static String ToJson<T>(T obj)
        {

            JsonSerializer jsz = new JsonSerializer();
            jsz.Converters.Add(new StringEnumConverter());
            jsz.NullValueHandling = NullValueHandling.Ignore;

            using (StringWriter sw = new StringWriter())
            {
                jsz.Serialize(sw, obj);
                return sw.ToString();
            }

        }

        /// <summary>
        /// Convert object from JSON
        /// </summary>
        private static T FromJson<T>(String json)
        {
            JsonSerializer jsz = new JsonSerializer()
            {
                SerializationBinder = new ModelSerializationBinder(),
                TypeNameAssemblyFormat= System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
                TypeNameHandling = TypeNameHandling.All
            };
            jsz.Converters.Add(new StringEnumConverter());
            using (StringReader sr = new StringReader(json))
            {
                return (T)jsz.Deserialize(sr, typeof(T));
            }
        }


        // Context
        private Tracer m_tracer = Tracer.GetTracer(typeof(AppletFunctionBridge));

        // Cached menus
        private Dictionary<IPrincipal, String> m_cachedMenus = new Dictionary<IPrincipal, String>();

        private frmDisconnectedClient m_context;
        
        /// <summary>
        /// Gets the context of the function
        /// </summary>
        /// <param name="context">Context.</param>
        public AppletFunctionBridge(frmDisconnectedClient context)
        {
            this.m_context = context;
        }

        /// <summary>
        /// Get the specified reference set
        /// </summary>
        public String GetDataAsset(String dataId)
        {
            dataId = String.Format("data/{0}", dataId);
            return Convert.ToBase64String(ApplicationContext.Current.GetService<IAppletManagerService>().Applets.RenderAssetContent(DcApplicationContext.Current.GetService<IAppletManagerService>().Applets.SelectMany(o => o.Assets).FirstOrDefault(o => o.Name == dataId), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));
        }

        /// <summary>
        /// Gets the online status
        /// </summary>
        public bool GetOnlineState()
        {
            return ApplicationContext.Current?.GetService<INetworkInformationService>()?.IsNetworkAvailable == true;
        }

        /// <summary>
        /// Gets the online status
        /// </summary>
        public bool IsClinicalAvailable()
        {
            return ApplicationContext.Current.GetService<IClinicalIntegrationService>().IsAvailable();
        }

        /// <summary>
        /// Gets the online status
        /// </summary>
        public bool IsAdminAvailable()
        {
            return ApplicationContext.Current.GetService<IAdministrationIntegrationService>().IsAvailable();
        }

        /// <summary>
        /// Send log file
        /// </summary>
        public String GetStatus()
        {
            return "";
        }


        /// <summary>
        /// Gets the registered template form
        /// </summary>
        /// <param name="templateId"></param>
        public String GetTemplateForm(String templateId)
        {
            return DcApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetTemplateDefinition(templateId)?.Form;
        }


        /// <summary>
        /// Gets the registered template form
        /// </summary>
        /// <param name="templateId"></param>
        public String GetTemplateView(String templateId)
        {
            return DcApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetTemplateDefinition(templateId)?.View;
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        public String GetTemplates()
        {
            return $"[{String.Join(",", DcApplicationContext.Current.GetService<IAppletManagerService>().Applets.SelectMany(o => o.Templates).Where(o => o.Public).Select(o => $"\"{o.Mnemonic}\""))}]";
        }

        /// <summary>
        /// Get log files
        /// </summary>
        /// <returns></returns>
        public String GetLogFiles()
        {
            try
            {
                String logFileBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "log");
                List<String> files = new List<string>();
                foreach (var f in Directory.GetFiles(logFileBase))
                    files.Add(Path.GetFileName(f));
                return ToJson(files);
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error getting log files: {0}", ex);
                return null;
            }
        }

        /// <summary>
        /// Shows the specified message as a toast
        /// </summary>
        public void ShowToast(String toastText)
        {
            try
            {
                // TODO:
            }
            catch { }
        }

        /// <summary>
        /// Gets the current asset title
        /// </summary>
        public string GetCurrentAssetTitle()
        {
            return null;
        }

        /// <summary>
        /// Get version name
        /// </summary>
        public String GetVersion()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(o => o.DefinedTypes.Any(t => t.Name == "SplashActivity")) ?? typeof(SanteDBConfiguration).Assembly;

            return String.Format("{0} ({1})", asm.GetName().Version,
                asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        }

        /// <summary>
        /// Create new UUID
        /// </summary>
        public String NewGuid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Get version name
        /// </summary>
        public String GetService(String serviceName)
        {
            Type serviceType = Type.GetType(serviceName);
            if (serviceType == null)
                return ApplicationContext.Current.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.FirstOrDefault(
                    o => o.GetType().GetInterfaces().Any(i => i.Name == serviceName) ||
                    o.GetType().Name == serviceName || o.GetType().BaseType.Name == serviceName
                )?.Type.Name;
            else
                return ApplicationContext.Current.GetService(serviceType)?.GetType().Name;
        }


        
        public string GetNetworkState()
        {
            var networkInformationService = ApplicationContext.Current.GetService<INetworkInformationService>();

            return networkInformationService.IsNetworkAvailable.ToString();
        }
      
        /// <summary>
        /// Get the specified string
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="stringId">String identifier.</param>
        public String GetString(String locale, String stringId)
        {
            try
            {
                return DcApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetStrings(locale)?.FirstOrDefault(o => o.Key == stringId).Value ?? stringId;
            }
            catch (Exception e)
            {
                //this.m_tracer.TraceWarning("Error retreiving string {0}", stringId);
                return stringId;
            }
        }

        /// <summary>
        /// Go back home
        /// </summary>
        public void Back()
        {
            this.m_context.Back();
        }


        /// <summary>Close the applet</summary>
        public void Close()
        {
            Action doClose = () => { this.m_context.Close(); };
            this.m_context.Invoke(doClose);
        }

        /// <summary>
        /// Get the applet strings
        /// </summary>
        public String GetStrings(String locale)
        {
            var strings = DcApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetStrings(locale);

            using (StringWriter sw = new StringWriter())
            {
                sw.Write("{");
                foreach (var itm in strings)
                {
                    sw.Write("\"{0}\":\"{1}\",", itm.Key, itm.Value);
                }
                sw.Write("\"locale\":\"{0}\" }}", locale);
                return sw.ToString();
            }

        }

        /// <summary>
        /// Set the locale of the application
        /// </summary>
        public String SetLocale(String locale)
        {
            try
            {
                this.m_tracer.TraceVerbose("Setting locale to {0}", locale);
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(locale);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error setting locale to {0}: {1}", locale, e);
            }
            return CultureInfo.DefaultThreadCurrentUICulture.TwoLetterISOLanguageName;
        }

        /// <summary>
        /// Get magic
        /// </summary>
        public String GetMagic()
        {
            return ApplicationContext.Current.ExecutionUuid.ToString();
        }
    }
}
