using Xamarin.Forms.GoogleMaps.Internals;

namespace Xamarin.Forms.GoogleMaps
{
    public sealed class CameraUpdate
    {
        private static double lastZoom;

        public CameraUpdateType UpdateType { get; }
        public Position Position { get; }
        public double Zoom { get; } = lastZoom;
        public Bounds Bounds { get; }
        public int Padding { get; }
        public CameraPosition CameraPosition { get; }

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