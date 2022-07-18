﻿using CoreGraphics;
using Google.Maps;
using System.Collections.Generic;
using System.Linq;
using UIKit;
using Xamarin.Forms.GoogleMaps.iOS.Extensions;
using Xamarin.Forms.GoogleMaps.iOS.Factories;
using NativeGroundOverlay = Google.Maps.GroundOverlay;

namespace Xamarin.Forms.GoogleMaps.Logics.iOS
{
    internal class GroundOverlayLogic : DefaultGroundOverlayLogic<NativeGroundOverlay, MapView>
    {
        private readonly IImageFactory _imageFactory;
        
        protected override IList<GroundOverlay> GetItems(Map map) => map.GroundOverlays;

        public GroundOverlayLogic(IImageFactory imageFactory)
        {
            _imageFactory = imageFactory;
        }

        internal override void Register(MapView oldNativeMap, Map oldMap, MapView newNativeMap, Map newMap)
        {
            base.Register(oldNativeMap, oldMap, newNativeMap, newMap);

            if (newNativeMap != null)
            {
                newNativeMap.OverlayTapped += OnOverlayTapped;
            }
        }

        internal override void Unregister(MapView nativeMap, Map map)
        {
            if (nativeMap != null)
            {
                nativeMap.OverlayTapped -= OnOverlayTapped;
            }

            base.Unregister(nativeMap, map);
        }

        protected override NativeGroundOverlay CreateNativeItem(GroundOverlay outerItem)
        {
            var factory = _imageFactory ?? DefaultImageFactory.Instance;
            var nativeOverlay = NativeGroundOverlay.GetGroundOverlay(
                outerItem.Bounds.ToCoordinateBounds(), factory.ToUIImage(outerItem.Icon));
            nativeOverlay.Bearing = outerItem.Bearing;
            nativeOverlay.Opacity = 1 - outerItem.Transparency;
            nativeOverlay.Tappable = outerItem.IsClickable;
            nativeOverlay.ZIndex = outerItem.ZIndex;

            if (outerItem.Icon != null)
            {
                nativeOverlay.Icon = factory.ToUIImage(outerItem.Icon);
            }

            outerItem.NativeObject = nativeOverlay;
            nativeOverlay.Map = outerItem.IsVisible ? NativeMap : null;

            OnUpdateIconView(outerItem, nativeOverlay);

            return nativeOverlay;
        }

        protected override NativeGroundOverlay DeleteNativeItem(GroundOverlay outerItem)
        {
            var nativeOverlay = outerItem.NativeObject as NativeGroundOverlay;
            nativeOverlay.Map = null;

            return nativeOverlay;
        }

        void OnOverlayTapped(object sender, GMSOverlayEventEventArgs e)
        {
            var targetOuterItem = GetItems(Map).FirstOrDefault(
                outerItem => object.ReferenceEquals(outerItem.NativeObject, e.Overlay));
            targetOuterItem?.SendTap();
        }

        internal override void OnUpdateBearing(GroundOverlay outerItem, NativeGroundOverlay nativeItem)
        {
            nativeItem.Bearing = outerItem.Bearing;
        }

        internal override void OnUpdateBounds(GroundOverlay outerItem, NativeGroundOverlay nativeItem)
        {
            nativeItem.Bounds = outerItem.Bounds.ToCoordinateBounds();
        }

        internal override void OnUpdateIcon(GroundOverlay outerItem, NativeGroundOverlay nativeItem)
        {
            if (outerItem.Icon.Type == BitmapDescriptorType.View)
            {
                OnUpdateIconView(outerItem, nativeItem);
            }
            else
            {
                var factory = _imageFactory ?? DefaultImageFactory.Instance;
                nativeItem.Icon = factory.ToUIImage(outerItem.Icon);
            }
        }

        internal override void OnUpdateIsClickable(GroundOverlay outerItem, NativeGroundOverlay nativeItem)
        {
            nativeItem.Tappable = outerItem.IsClickable;
        }

        internal override void OnUpdateTransparency(GroundOverlay outerItem, NativeGroundOverlay nativeItem)
        {
            nativeItem.Opacity = 1f - outerItem.Transparency;
        }

        internal override void OnUpdateZIndex(GroundOverlay outerItem, NativeGroundOverlay nativeItem)
        {
            nativeItem.ZIndex = outerItem.ZIndex;
        }

        protected void OnUpdateIconView(GroundOverlay outerItem, NativeGroundOverlay nativeItem)
        {
            if (outerItem?.Icon?.Type == BitmapDescriptorType.View && outerItem?.Icon?.View != null)
            {
                NativeMap.InvokeOnMainThread(() =>
                {
                    var iconView = outerItem.Icon.View;
                    var nativeView = Utils.ConvertFormsToNative(iconView, new CGRect(0, 0, iconView.WidthRequest, iconView.HeightRequest));
                    nativeView.BackgroundColor = UIColor.Clear;
                    //nativeItem.GroundAnchor = new CGPoint(iconView.AnchorX, iconView.AnchorY);
                    nativeItem.Icon = Utils.ConvertViewToImage(nativeView);

                    // Would have been way cooler to do this instead, but surprisingly, we can't do this on Android:
                    // nativeItem.IconView = nativeView;
                });
            }
        }
    }
}

