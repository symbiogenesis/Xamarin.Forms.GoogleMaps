﻿using System.Diagnostics;
using Xamarin.Forms.GoogleMaps.Internals;

namespace Xamarin.Forms.GoogleMaps
{
    public struct Distance
    {
        const double MetersPerMile = GeoConstants.MetersPerMile;
        const double MetersPerKilometer = GeoConstants.MetersPerKilometer;

        public Distance(double meters)
        {
            Meters = meters;
        }

        public double Meters { get; }

        public double Miles => Meters / MetersPerMile;

        public double Kilometers => Meters / MetersPerKilometer;

        public static Distance FromMiles(double miles)
        {
            if (miles < 0)
            {
                Debug.WriteLine("Negative values for distance not supported");
                miles = 0;
            }
            return new Distance(miles * MetersPerMile);
        }

        public static Distance FromMeters(double meters)
        {
            if (meters < 0)
            {
                Debug.WriteLine("Negative values for distance not supported");
                meters = 0;
            }
            return new Distance(meters);
        }

        public static Distance FromKilometers(double kilometers)
        {
            if (kilometers < 0)
            {
                Debug.WriteLine("Negative values for distance not supported");
                kilometers = 0;
            }
            return new Distance(kilometers * MetersPerKilometer);
        }

        public bool Equals(Distance other)
        {
            return Meters.Equals(other.Meters);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            return obj is Distance distance && Equals(distance);
        }

        public override int GetHashCode()
        {
            return Meters.GetHashCode();
        }

        public static bool operator ==(Distance left, Distance right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Distance left, Distance right)
        {
            return !left.Equals(right);
        }
    }
    public enum DistanceType { Kilometers, Meters, Miles }
}