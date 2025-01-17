﻿using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls.Maps;

namespace Xamarin.Forms.GoogleMaps.UWP
{
    internal class UWPSyncTileLayer : CustomMapTileDataSource
    {
        private readonly Func<int, int, int, byte[]> _makeTileUri;

        public UWPSyncTileLayer(Func<int, int, int, byte[]> makeTileUri)
        {
            _makeTileUri = makeTileUri;
            this.BitmapRequested += UWPSyncTileLayer_BitmapRequested;
        }

        ~UWPSyncTileLayer()
        {
            this.BitmapRequested -= UWPSyncTileLayer_BitmapRequested;
        }

        private void UWPSyncTileLayer_BitmapRequested(CustomMapTileDataSource sender, MapTileBitmapRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var data = _makeTileUri(args.X, args.Y, args.ZoomLevel);

                        if (data != null)
                        {
                            using MemoryStream stream = new();
                            stream.Write(data, 0, data.Length);
                            stream.Flush();
                            stream.Position = 0;

                            using var randomAccessInputStream = stream.AsRandomAccessStream();
                            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessInputStream).AsTask().ConfigureAwait(false);
                            var pixelProvider = await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.ColorManageToSRgb);
                            var pixelData = pixelProvider.DetachPixelData();

                            using InMemoryRandomAccessStream randomAccessStream = new();
                            using var outputStream = randomAccessStream.GetOutputStreamAt(0);
                            using DataWriter writer = new(outputStream);
                            writer.WriteBytes(pixelData);
                            await writer.StoreAsync();
                            await writer.FlushAsync();
                            args.Request.PixelData = RandomAccessStreamReference.CreateFromStream(randomAccessStream);
                        }
                        deferral.Complete();
                    }
                    catch (Exception)
                    {
                        deferral.Complete();
                    }
                });
            }
            catch (Exception)
            {
                deferral.Complete();
            }
        }
    }
}
