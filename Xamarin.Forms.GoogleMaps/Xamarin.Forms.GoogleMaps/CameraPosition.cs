using System;

namespace Xamarin.Forms.GoogleMaps
{
    public sealed class CameraPosition : IEquatable<CameraPosition>
    {
        public Position Target { get; }
        public double Bearing { get; }
        public double Tilt { get; }
        public double Zoom { get; }

        public CameraPosition(Position target, double zoom)
        {
            Target = target;
            Zoom = zoom;
        }

        public CameraPosition(Position target, double zoom, double bearing)
        {
            Target = target;
            Zoom = zoom;
            Bearing = bearing;
        }

        public CameraPosition(Position target, double zoom, double bearing, double tilt)
        {
            Target = target;
            Bearing = bearing;
            Tilt = tilt;
            Zoom = zoom;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj is CameraPosition cameraPosition)
                return Equals(other: cameraPosition);

            return false;
        }

        public bool Equals(CameraPosition other)
        {
            return Target == other?.Target
                && Zoom == other?.Zoom
                && Tilt == other?.Tilt
                && Bearing == other?.Bearing;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
