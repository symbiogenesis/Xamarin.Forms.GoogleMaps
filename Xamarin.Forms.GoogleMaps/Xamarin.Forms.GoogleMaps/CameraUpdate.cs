using System;
using Xamarin.Forms.GoogleMaps.Internals;

namespace Xamarin.Forms.GoogleMaps
{
    public sealed class CameraUpdate : IEquatable<CameraUpdate>
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

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj is CameraUpdate cameraUpdate)
                return Equals(other: cameraUpdate);

            return false;
        }

        public bool Equals(CameraUpdate other)
        {
            return CameraPosition == other?.CameraPosition
                && Padding == other?.Padding
                && Zoom == other?.Zoom
                && UpdateType == other?.UpdateType
                && Bounds == other?.Bounds;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}