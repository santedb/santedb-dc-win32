using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Win
{
    [PreferredService(typeof(IOperatingSystemInfoService))]
    internal class WindowsOperatingSystemInfoService : IOperatingSystemInfoService
    {
        public string VersionString => Environment.OSVersion.VersionString;

        public OperatingSystemID OperatingSystem => OperatingSystemID.Win32;

        public string MachineName => Environment.MachineName;

        public string ManufacturerName => DefaultOperatingSystemInfoService.MANUFACTURER_GENERIC;
    }
}
