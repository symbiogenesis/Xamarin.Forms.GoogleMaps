using System;
using Windows.Storage;
using Windows.UI.Xaml.Controls.Maps;

namespace Xamarin.Forms.GoogleMaps.UWP
{
    internal class UWPUriTileLayer : LocalMapTileDataSource
    {
        private readonly Func<int, int, int, Uri> _makeTileUri;

        public UWPUriTileLayer(Func<int, int, int, Uri> makeTileUri, int tileSize = 256)
        {
            _makeTileUri = makeTileUri;
            this.UriRequested += UWPUriTileLayer_UriRequested;
        }

        ~UWPUriTileLayer()
        {
            this.UriRequested -= UWPUriTileLayer_UriRequested;
        }

        private void UWPUriTileLayer_UriRequested(LocalMapTileDataSource sender, MapTileUriRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();
            var uri = _makeTileUri(args.X, args.Y, args.ZoomLevel);
            if (uri != null)
            {
                if (!uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    uri = new Uri(string.Format("ms-appx:///local/{0}", uri.LocalPath.Replace(ApplicationData.Current.LocalFolder.Path, string.Empty).TrimStart('\\')));
                }
                args.Request.Uri = uri;
            }
            deferral.Complete();
        }
    }
}
