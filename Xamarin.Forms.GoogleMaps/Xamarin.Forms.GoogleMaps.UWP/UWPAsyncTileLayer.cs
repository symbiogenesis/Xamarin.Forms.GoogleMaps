using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls.Maps;

namespace Xamarin.Forms.GoogleMaps.UWP
{
    internal class UWPAsyncTileLayer : CustomMapTileDataSource
    {
        private readonly Func<int, int, int, Task<byte[]>> _makeTileUri;

        public UWPAsyncTileLayer(Func<int, int, int, Task<byte[]>> makeTileUri, int tileSize = 256)
        {
            _makeTileUri = makeTileUri;
            this.BitmapRequested += UWPSyncTileLayer_BitmapRequested;
        }

        ~UWPAsyncTileLayer()
        {
            this.BitmapRequested -= UWPSyncTileLayer_BitmapRequested;
        }

        private async void UWPSyncTileLayer_BitmapRequested(CustomMapTileDataSource sender, MapTileBitmapRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();
            try
            {
                var data = await _makeTileUri(args.X, args.Y, args.ZoomLevel);
                if (data != null)
                {
                    using MemoryStream stream = new();
                    stream.Write(data, 0, data.Length);
                    stream.Position = 0;
                    using var randomAccessInputStream = stream.AsRandomAccessStream();
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessInputStream);
                    var pixelProvider = await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.ColorManageToSRgb);
                    var pixelData = pixelProvider.DetachPixelData();

                    using InMemoryRandomAccessStream randomAccessOutputStream = new();
                    using var outputStream = randomAccessOutputStream.GetOutputStreamAt(0);
                    using DataWriter writer = new(outputStream);
                    writer.WriteBytes(pixelData);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    args.Request.PixelData = RandomAccessStreamReference.CreateFromStream(randomAccessInputStream);
                }
                deferral.Complete();
            }
            catch (Exception)
            {
                deferral.Complete();
            }
        }
    }
}
