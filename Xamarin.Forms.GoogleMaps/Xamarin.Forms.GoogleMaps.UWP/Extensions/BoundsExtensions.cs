using Windows.Devices.Geolocation;

namespace Xamarin.Forms.GoogleMaps.UWP.Extensions
{
    internal static class BoundsExtensions
    {
        public static GeoboundingBox ToGeoboundingBox(this Bounds self)
        {
            return new GeoboundingBox(
                self.NorthWest.ToBasicGeoposition(), 
                self.SouthEast.ToBasicGeoposition());
        }
    }
}
