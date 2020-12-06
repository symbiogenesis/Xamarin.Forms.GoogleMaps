using Windows.Devices.Geolocation;

namespace Xamarin.Forms.GoogleMaps.UWP.Extensions
{
    internal static class LngLatExtensions
    {
        public static Position ToPosition(this BasicGeoposition self)
        {
            return new Position(self.Latitude, self.Longitude);
        }
    }
}
