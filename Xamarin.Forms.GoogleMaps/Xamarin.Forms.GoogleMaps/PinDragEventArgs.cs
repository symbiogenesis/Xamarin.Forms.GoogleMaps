using System;
namespace Xamarin.Forms.GoogleMaps
{
    public sealed class PinDragEventArgs : EventArgs
    {
        public Pin Pin
        {
            get;

        }

        internal PinDragEventArgs(Pin pin)
        {
            this.Pin = pin;
        }
    }
}

