using SanteDB.Client.Services;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace SanteDB.Client.Win
{
    [PreferredService(typeof(IGeographicLocationProvider))]
    internal class WindowsLocationProvider : IGeographicLocationProvider
    {
        public string ServiceName => nameof(WindowsLocationProvider);

        public GeoTag? GetCurrentPosition()
        {
            var location = Nito.AsyncEx.AsyncContext.Run(GetLocationAsync);

            if (null != location?.Coordinate)
            {
                return new GeoTag(location.Coordinate.Latitude, location.Coordinate.Latitude, location?.Coordinate?.Accuracy < 101d);
            }

            return null;
        }

        private async Task<Geoposition> GetLocationAsync()
        {
            var access = await Geolocator.RequestAccessAsync();

            if (access == GeolocationAccessStatus.Allowed)
            {
                var locator = new Geolocator();

                return await locator.GetGeopositionAsync(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
            }


            //TODO: Log and perhaps request position.
            return null;
        }
    }
}
