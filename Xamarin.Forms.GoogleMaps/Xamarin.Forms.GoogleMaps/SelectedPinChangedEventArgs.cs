using System;
namespace Xamarin.Forms.GoogleMaps
{
    public sealed class SelectedPinChangedEventArgs : EventArgs
    {
        public Pin SelectedPin
        {
            get;

        }

        internal SelectedPinChangedEventArgs(Pin selectedPin)
        {
            this.SelectedPin = selectedPin;
        }
    }
}

