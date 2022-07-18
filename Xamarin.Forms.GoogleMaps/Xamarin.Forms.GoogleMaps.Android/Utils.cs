// Original code from https://github.com/javiholcman/Wapps.Forms.Map/
// Cacheing implemented by Gadzair

using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Java.Nio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.Android;

namespace Xamarin.Forms.GoogleMaps.Android
{
    static class Utils
    {
        /// <summary>
        /// convert from dp to pixels
        /// </summary>
        /// <param name="dp">Dp.</param>
        public static int DpToPx(float dp)
        {
            var metrics = global::Android.App.Application.Context.Resources.DisplayMetrics;
            return (int)(dp * metrics.Density);
        }

        /// <summary>
        /// convert from px to dp
        /// </summary>
        /// <param name="px">Px.</param>
        public static float PxToDp(int px)
        {
            var metrics = global::Android.App.Application.Context.Resources.DisplayMetrics;
            return px / metrics.Density;
        }

        public static Task<global::Android.Views.View> ConvertFormsToNative(View view, Rectangle size, IVisualElementRenderer vRenderer)
        {
            return Task.Run(() => {
                var nativeView = vRenderer.View;
                vRenderer.Tracker.UpdateLayout();
                var layoutParams = new ViewGroup.LayoutParams((int)size.Width, (int)size.Height);
                nativeView.LayoutParameters = layoutParams;
                view.Layout(size);
                nativeView.Layout(0, 0, (int)view.Width, (int)view.Height);
                //await FixImageSourceOfImageViews(viewGroup as ViewGroup); // Not sure why this was being done in original
                return nativeView;
            });
        }

        public static Bitmap ConvertViewToBitmap(global::Android.Views.View view)
        {
            Bitmap bitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bitmap);
            view.Draw(canvas);
            return bitmap;
        }

        private static readonly LinkedList<string> lruTracker = new LinkedList<string>();
        private static readonly ConcurrentDictionary<string, global::Android.Gms.Maps.Model.BitmapDescriptor> cache = new ConcurrentDictionary<string, global::Android.Gms.Maps.Model.BitmapDescriptor>();

        public static Task<global::Android.Gms.Maps.Model.BitmapDescriptor> ConvertViewToBitmapDescriptor(global::Android.Views.View v)
        {
            return Task.Run(() => {
                var bmp = ConvertViewToBitmap(v);
                var img = global::Android.Gms.Maps.Model.BitmapDescriptorFactory.FromBitmap(bmp);

                var buffer = ByteBuffer.Allocate(bmp.ByteCount);
                bmp.CopyPixelsToBuffer(buffer);
                buffer.Rewind();

                // https://forums.xamarin.com/discussion/5950/how-to-convert-from-bitmap-to-byte-without-bitmap-compress
                IntPtr classHandle = JNIEnv.FindClass("java/nio/ByteBuffer");
                IntPtr methodId = JNIEnv.GetMethodID(classHandle, "array", "()[B");
                IntPtr resultHandle = JNIEnv.CallObjectMethod(buffer.Handle, methodId);
                byte[] bytes = JNIEnv.GetArray<byte>(resultHandle);
                JNIEnv.DeleteLocalRef(resultHandle);

                var sha = MD5.Create();
                var hash = Convert.ToBase64String(sha.ComputeHash(bytes));

                var exists = cache.ContainsKey(hash);
                lock (lruTracker)
                {//LinkedList is not thread safe impl, will crash in multi-trheads scenerios, and so using lock to work-around
                    if (exists)
                    {
                        lruTracker.Remove(hash);
                        lruTracker.AddLast(hash);
                        return cache[hash];
                    }
                    if (lruTracker.Count > 10) // O(1)
                    {
                        cache.TryRemove(lruTracker.First.Value, out global::Android.Gms.Maps.Model.BitmapDescriptor tmp);
                        lruTracker.RemoveFirst();
                    }
                    lruTracker.AddLast(hash);
                }//lock lruTracker
                cache.GetOrAdd(hash, img);
                return img;
            });
        }

        public static global::Android.Widget.FrameLayout AddViewOnFrameLayout(global::Android.Views.View view, int width, int height)
        {
            var layout = new global::Android.Widget.FrameLayout(view.Context)
            {
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            };
            view.LayoutParameters = new global::Android.Widget.FrameLayout.LayoutParams(width, height);
            layout.AddView(view);
            return layout;
        }
    }
}