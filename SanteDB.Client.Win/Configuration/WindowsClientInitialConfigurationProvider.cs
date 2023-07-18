﻿using SanteDB.BI.Services.Impl;
using SanteDB.Caching.Memory;
using SanteDB.Caching.Memory.Session;
using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Disconnected.Data.Synchronization.Configuration;
using SanteDB.Client.OAuth;
using SanteDB.Client.Upstream.Repositories;
using SanteDB.Client.Tickles;
using SanteDB.Client.Upstream.Management;
using SanteDB.Client.Upstream.Security;
using SanteDB.Client.Upstream;
using SanteDB.Client.UserInterface.Impl;
using SanteDB.Core;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Http;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.Data;
using SanteDB.Core.Diagnostics.Tracing;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Security;
using SanteDB.Core.Services.Impl;
using SanteDB.Rest.OAuth.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Client.WinUI;
using SanteDB.Client.Batteries.Services;

namespace SanteDB.Client.Win.Configuration
{
    public class WindowsClientInitialConfigurationProvider : IInitialConfigurationProvider
    {
        public int Order => Int32.MinValue;

        public SanteDBConfiguration Provide(SanteDBHostType hostContextType, SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            var instanceName = appServiceSection.InstanceName;
            var localDataPath = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();

            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("input.name", "simple"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("input.address", "text"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.city", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.county", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.state", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.name.family", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.given", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.state", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.county", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.city", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.precinct", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.prefix", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.suffix", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.family", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.given", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("allow.patient.religion", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("allow.patient.ethnicity", "false"));
            appServiceSection.AppSettings = appServiceSection.AppSettings.OrderBy(o => o.Key).ToList();


            // Security configuration
            var wlan = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(o => o.NetworkInterfaceType == NetworkInterfaceType.Ethernet || o.Description.StartsWith("wlan"));
            String macAddress = Guid.NewGuid().ToString();
            if (wlan != null)
            {
                macAddress = wlan.GetPhysicalAddress().ToString();
            }
            //else

            // Upstream default configuration
            UpstreamConfigurationSection upstreamConfiguration = new UpstreamConfigurationSection()
            {
                Credentials = new List<UpstreamCredentialConfiguration>()
                {
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = $"Debugee-{macAddress.Replace(" ", "")}",
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Device
                    },
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = "org.santedb.disconnected_client.win32",
                        CredentialSecret = "FE78825ADB56401380DBB406411221FD",
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Application
                    }
                }
            };



            configuration.AddSection(new SecurityConfigurationSection()
            {
                PasswordRegex = @"^(?=.*\d){1,}(?=.*[a-z]){1,}(?=.*[A-Z]){1,}(?=.*[^\w\d]){1,}.{6,}$",
                SecurityPolicy = new List<SecurityPolicyConfiguration>()
                {
                    new SecurityPolicyConfiguration(SecurityPolicyIdentification.SessionLength, new TimeSpan(0,30,0)),
                    new SecurityPolicyConfiguration(SecurityPolicyIdentification.RefreshLength, new TimeSpan(0,35,0))
                },
                Signatures = new List<SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration>()
                {
                    new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                    {
                        KeyName ="default",
                        Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.HS256,
                        HmacSecret = "QUFCN0ZDQjk2ODU1MDkwODIzNTIxREM2OEIxRTA5RDgzMUQ3MkY1RTk2MzAzNzRCMjU0ODdBMUFCQzUzRDAzMjYyMjQ1REE0RDA1MUMyRkMzOEVGMkNCMjBCM0FDQzRBRjE2MTdEQzUwQ0U4NDJGOUFFOEIzMjQzRTQ2MUNCMTE="
                    }
                }
            });
            // Trace writer

            var logDirectory = Path.Combine(localDataPath, "log");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

#if DEBUG
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.Warning,
                        InitializationData = "santedb",
                        TraceWriter = typeof(DebugDiagnosticsTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Warning,
                        InitializationData = Path.Combine(logDirectory, "santedb.log"),
                        TraceWriter = typeof(RolloverTextWriterTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Error,
                        InitializationData = "santedb",
                        TraceWriter = typeof(ConsoleTraceWriter)
                    }
                }
            };
#else
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.Informational,
                        InitializationData = Path.Combine(logDirectory, "santedb.log"),
                        TraceWriter = typeof(RolloverTextWriterTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Informational,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(ConsoleTraceWriter)
                    }
                }
            };
#endif

            // Setup the tracers 
            diagSection.TraceWriter.ForEach(o => Tracer.AddWriter(Activator.CreateInstance(o.TraceWriter, o.Filter, o.InitializationData, null) as TraceWriter, o.Filter));
            configuration.Sections.Add(new FileSystemDispatcherQueueConfigurationSection()
            {
                QueuePath = Path.Combine(localDataPath, "queue"),
            });

            var backupSection = new BackupConfigurationSection()
            {
                PrivateBackupLocation = Path.Combine(localDataPath, "backup"),
                PublicBackupLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "santedb", "dc-win32", "backup")
            };

            configuration.Sections.Add(new RestClientConfigurationSection()
            {
                RestClientType = new TypeReferenceConfiguration(typeof(RestClient))
            });
            configuration.Sections.Add(new OAuthConfigurationSection()
            {
                IssuerName = upstreamConfiguration.Credentials[0].CredentialName,
                AllowClientOnlyGrant = false,
                JwtSigningKey = "jwsdefault",
                TokenType = "bearer"
            });
            configuration.Sections.Add(new ClientConfigurationSection()
            {
                AutoUpdateApplets = true
            });
            configuration.Sections.Add(diagSection);
            configuration.Sections.Add(upstreamConfiguration);
            configuration.Sections.Add(new AuditAccountabilityConfigurationSection()
            {
                AuditFilters = new List<AuditFilterConfiguration>()
                {
                    // Audit any failure - No matter which event
                    new AuditFilterConfiguration(null, null, OutcomeIndicator.EpicFail | OutcomeIndicator.MinorFail | OutcomeIndicator.SeriousFail, true, true),
                    // Audit anything that creates, reads, or updates data
                    new AuditFilterConfiguration(ActionType.Create | ActionType.Read | ActionType.Update | ActionType.Delete, null, null, true, true)
                }
            });
            configuration.Sections.Add(new SynchronizationConfigurationSection()
            {
                Mode = SynchronizationMode.Partial,
                PollInterval = new TimeSpan(0, 15, 0),
                ForbidSending = new List<ResourceTypeReferenceConfiguration>()
                {
                    new ResourceTypeReferenceConfiguration(typeof(DeviceEntity)),
                    new ResourceTypeReferenceConfiguration(typeof(ApplicationEntity)),
                    new ResourceTypeReferenceConfiguration(typeof(Concept)),
                    new ResourceTypeReferenceConfiguration(typeof(ConceptSet)),
                    new ResourceTypeReferenceConfiguration(typeof(Place)),
                    new ResourceTypeReferenceConfiguration(typeof(ReferenceTerm)),
                    new ResourceTypeReferenceConfiguration(typeof(AssigningAuthority)),
                    new ResourceTypeReferenceConfiguration(typeof(UserEntity)),
                    new ResourceTypeReferenceConfiguration(typeof(SecurityUser)),
                    new ResourceTypeReferenceConfiguration(typeof(SecurityDevice)),
                    new ResourceTypeReferenceConfiguration(typeof(SecurityApplication))
                }
            });

            return configuration;
        }
    }
}
