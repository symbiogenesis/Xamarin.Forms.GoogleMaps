using Xamarin.Forms.GoogleMaps.Internals;

namespace Xamarin.Forms.GoogleMaps
{
    public sealed class CameraUpdate
    {
        private static double lastZoom;

        public CameraUpdateType UpdateType { get; set; }
        public Position Position { get; set; }
        public double Zoom { get; set; } = lastZoom;
        public Bounds Bounds { get; set; }
        public int Padding { get; set; }
        public CameraPosition CameraPosition { get; set; }

        public CameraUpdate() { }

        public CameraUpdate(Position position)
        {
            UpdateType = CameraUpdateType.LatLng;
            Position = position;
        }

        public CameraUpdate(Position position, double zoomLv)
        {
            UpdateType = CameraUpdateType.LatLngZoom;
            Position = position;
            Zoom = zoomLv;
            lastZoom = Zoom;
        }

        public CameraUpdate(Bounds bounds, int padding)
        {
            UpdateType = CameraUpdateType.LatLngBounds;
            Bounds = bounds;
            Padding = padding;
        }

        public CameraUpdate(CameraPosition cameraPosition)
        {
            UpdateType = CameraUpdateType.CameraPosition;
            CameraPosition = cameraPosition;
            Zoom = cameraPosition.Zoom;
            lastZoom = Zoom;
        }
    }
}