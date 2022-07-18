using System;

namespace Xamarin.Forms.GoogleMaps
{
    public sealed class InfoWindowLongClickedEventArgs : EventArgs
    {
        public Pin Pin { get; }

        internal InfoWindowLongClickedEventArgs(Pin pin)
        {
            this.Pin = pin;
        }
    }
}
