using System;

namespace Xamarin.Forms.GoogleMaps
{
    public struct Position : IEquatable<Position>
    {
        public Position(double latitude, double longitude)
        {
            Latitude = Math.Min(Math.Max(latitude, -90.0), 90.0);
            Longitude = Math.Min(Math.Max(longitude, -180.0), 180.0);
        }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (obj is not Position other)
                return false;
            return Equals(other);
        }

        public bool Equals(Position other)
        {
            return Latitude == other.Latitude && Longitude == other.Longitude;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Latitude.GetHashCode();
                return (hashCode * 397) ^ Longitude.GetHashCode();
            }
        }

        public static bool operator ==(Position left, Position right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !Equals(left, right);
        }

        public static Position operator -(Position left, Position right)
        {
            return new Position(left.Latitude - right.Latitude, left.Longitude - right.Longitude);
        }

        public bool IsEmpty()
        {
            return Latitude == 0 && Longitude == 0;
        }
    }
}