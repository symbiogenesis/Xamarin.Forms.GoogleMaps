namespace Xamarin.Forms.GoogleMaps.Internals
{
    class MoveToRegionMessage
    {
        public MapSpan Span { get; }
        public bool Animate { get; }

        public MoveToRegionMessage(MapSpan mapSpan, bool animate = true)
        {
            this.Span = mapSpan;
            this.Animate = animate;
        }
    }
}


