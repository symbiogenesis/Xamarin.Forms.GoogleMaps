using Google.Maps;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms.GoogleMaps.Internals;
using Xamarin.Forms.GoogleMaps.iOS.Extensions;
using Xamarin.Forms.GoogleMaps.Logics;
using Xamarin.Forms.GoogleMaps.Logics.iOS;
using Xamarin.Forms.Platform.iOS;
using GCameraPosition = Google.Maps.CameraPosition;

namespace Xamarin.Forms.GoogleMaps.iOS
{
    public class MapRenderer : ViewRenderer
    {
        private static readonly UIColor _tealColor = Color.Teal.ToUIColor();
        private static readonly UIColor _blackColor = Color.Black.ToUIColor();

        bool _shouldUpdateRegion = true;

        // ReSharper disable once MemberCanBePrivate.Global
        protected MapView NativeMap => (MapView)Control;
        // ReSharper disable once MemberCanBePrivate.Global
        protected Map Map => (Map)Element;

        protected internal static PlatformConfig Config { protected get; set; }

        readonly UiSettingsLogic _uiSettingsLogic = new UiSettingsLogic();
        readonly CameraLogic _cameraLogic;

        private bool _ready;

        internal readonly IList<BaseLogic<MapView>> Logics;

        public MapRenderer()
        {
            Logics = new List<BaseLogic<MapView>>
            {
                new PolylineLogic(),
                new PolygonLogic(),
                new CircleLogic(),
                new PinLogic(Config.ImageFactory, OnMarkerCreating, OnMarkerCreated, OnMarkerDeleting, OnMarkerDeleted),
                new TileLayerLogic(),
                new GroundOverlayLogic(Config.ImageFactory)
            };

            _cameraLogic = new CameraLogic(() => OnCameraPositionChanged(NativeMap.Camera));
        }

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            return Control.GetSizeRequest(widthConstraint, heightConstraint);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Map != null)
                {
                    Map.OnSnapshot -= OnSnapshot;
                    foreach (var logic in Logics)
                    {
                        logic.Unregister(NativeMap, Map);
                    }
                }
                _cameraLogic.Unregister();
                _uiSettingsLogic.Unregister();

                var mkMapView = (MapView)Control;
                if (mkMapView != null)
                {
                    mkMapView.CoordinateLongPressed -= CoordinateLongPressed;
                    mkMapView.CoordinateTapped -= CoordinateTapped;
                    mkMapView.CameraPositionChanged -= CameraPositionChanged;
                    mkMapView.DidTapMyLocationButton = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            // For XAML Previewer or FormsGoogleMaps.Init not called.
            if (!FormsGoogleMaps.IsInitialized)
            {
                var label = new UILabel()
                {
                    Text = "Xamarin.Forms.GoogleMaps",
                    BackgroundColor = _tealColor,
                    TextColor = _blackColor,
                    TextAlignment = UITextAlignment.Center
                };
                SetNativeControl(label);
                return;
            }

            var oldMapView = (MapView)Control;
            if (e.OldElement != null)
            {
                var oldMapModel = (Map)e.OldElement;
                oldMapModel.OnSnapshot -= OnSnapshot;
                _cameraLogic.Unregister();

                if (oldMapView != null)
                {
                    oldMapView.CoordinateLongPressed -= CoordinateLongPressed;
                    oldMapView.CoordinateTapped -= CoordinateTapped;
                    oldMapView.CameraPositionChanged -= CameraPositionChanged;
                    oldMapView.DidTapMyLocationButton = null;
                }
            }

            if (e.NewElement != null)
            {
                var mapModel = (Map)e.NewElement;

                if (Control == null)
                {
                    SetNativeControl(new MapView(RectangleF.Empty));
                    var nativeMap = NativeMap;
                    nativeMap.CameraPositionChanged += CameraPositionChanged;
                    nativeMap.CoordinateTapped += CoordinateTapped;
                    nativeMap.CoordinateLongPressed += CoordinateLongPressed;
                    nativeMap.DidTapMyLocationButton = DidTapMyLocation;
                }

                _cameraLogic.Register(Map, NativeMap);
                Map.OnSnapshot += OnSnapshot;

                //_cameraLogic.MoveCamera(mapModel.InitialCameraUpdate);
                //_ready = true;

                _uiSettingsLogic.Register(Map, NativeMap);
                UpdateMapType();
                UpdateHasScrollEnabled(_uiSettingsLogic.ScrollGesturesEnabled);
                UpdateHasZoomEnabled(_uiSettingsLogic.ZoomGesturesEnabled);
                UpdateHasRotationEnabled(_uiSettingsLogic.RotateGesturesEnabled);
                UpdateIsTrafficEnabled();
                UpdatePadding();
                UpdateMapStyle();
                UpdateMyLocationEnabled();
                _uiSettingsLogic.Initialize();

                foreach (var logic in Logics)
                {
                    logic.Register(oldMapView, (Map)e.OldElement, NativeMap, Map);
                    logic.RestoreItems();
                    logic.OnMapPropertyChanged(new PropertyChangedEventArgs(Map.SelectedPinProperty.PropertyName));
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            // For XAML Previewer or FormsGoogleMaps.Init not called.
            if (!FormsGoogleMaps.IsInitialized)
            {
                return;
            }

            if (e.PropertyName == Map.MapTypeProperty.PropertyName)
            {
                UpdateMapType();
            }
            else if (e.PropertyName == Map.MyLocationEnabledProperty.PropertyName)
            {
                UpdateMyLocationEnabled();
            }
            else if (e.PropertyName == Map.HasScrollEnabledProperty.PropertyName)
            {
                UpdateHasScrollEnabled();
            }
            else if (e.PropertyName == Map.HasRotationEnabledProperty.PropertyName)
            {
                UpdateHasRotationEnabled();
            }
            else if (e.PropertyName == Map.HasZoomEnabledProperty.PropertyName)
            {
                UpdateHasZoomEnabled();
            }
            else if (e.PropertyName == Map.IsTrafficEnabledProperty.PropertyName)
            {
                UpdateIsTrafficEnabled();
            }
            else if (e.PropertyName == VisualElement.HeightProperty.PropertyName &&
                     Map.InitialCameraUpdate != null)
            {
                _shouldUpdateRegion = true;
            }
            else if (e.PropertyName == Map.IndoorEnabledProperty.PropertyName)
            {
                UpdateHasIndoorEnabled();
            }
            else if (e.PropertyName == Map.PaddingProperty.PropertyName)
            {
                UpdatePadding();
            }
            else if (e.PropertyName == Map.MapStyleProperty.PropertyName)
            {
                UpdateMapStyle();
            }

            foreach (var logic in Logics)
            {
                logic.OnMapPropertyChanged(e);
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            // For XAML Previewer or FormsGoogleMaps.Init not called.
            if (!FormsGoogleMaps.IsInitialized)
            {
                return;
            }

            if (_shouldUpdateRegion && !_ready)
            {
                _cameraLogic.MoveCamera(Map.InitialCameraUpdate);
                _ready = true;
                _shouldUpdateRegion = false;
            }
        }

        void OnSnapshot(TakeSnapshotMessage snapshotMessage)
        {
            UIGraphics.BeginImageContextWithOptions(NativeMap.Frame.Size, false, 0f);
            NativeMap.Layer.RenderInContext(UIGraphics.GetCurrentContext());
            var snapshot = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            // Why using task? Because Android side is asynchronous. 
            Task.Run(() => snapshotMessage.OnSnapshot.Invoke(snapshot.AsPNG().AsStream()));
        }

        protected void CameraPositionChanged(object sender, GMSCameraEventArgs args)
        {
            OnCameraPositionChanged(args.Position);
        }

        void OnCameraPositionChanged(GCameraPosition pos)
        {
            if (Element == null)
                return;

            Map.Region = NativeMap.Projection.VisibleRegion.ToRegion();

            var camera = pos.ToXamarinForms();
            Map.CameraPosition = camera;
            Map.SendCameraChanged(camera);
        }

        protected void CoordinateTapped(object sender, GMSCoordEventArgs e)
        {
            Map.SendMapClicked(e.Coordinate.ToPosition());
        }

        protected void CoordinateLongPressed(object sender, GMSCoordEventArgs e)
        {
            Map.SendMapLongClicked(e.Coordinate.ToPosition());
        }

        bool DidTapMyLocation(MapView mapView)
        {
            return Map.SendMyLocationClicked();
        }

        private void UpdateHasScrollEnabled(bool? initialScrollGesturesEnabled = null)
        {
#pragma warning disable 618
            NativeMap.Settings.ScrollGestures = initialScrollGesturesEnabled ?? ((Map)Element).HasScrollEnabled;
#pragma warning restore 618
        }

        private void UpdateHasZoomEnabled(bool? initialZoomGesturesEnabled = null)
        {
#pragma warning disable 618
            NativeMap.Settings.ZoomGestures = initialZoomGesturesEnabled ?? Map.HasZoomEnabled;
#pragma warning restore 618
        }

        private void UpdateHasRotationEnabled(bool? initialRotateGesturesEnabled = null)
        {
#pragma warning disable 618
            NativeMap.Settings.RotateGestures = initialRotateGesturesEnabled ?? Map.HasRotationEnabled;
#pragma warning restore 618
        }

        void UpdateMyLocationEnabled()
        {
            NativeMap.MyLocationEnabled = Map.MyLocationEnabled;
        }

        void UpdateIsTrafficEnabled()
        {
            NativeMap.TrafficEnabled = Map.IsTrafficEnabled;
        }

        void UpdateHasIndoorEnabled()
        {
            NativeMap.IndoorEnabled = Map.IsIndoorEnabled;
        }

        void UpdateMapType()
        {
            switch (Map.MapType)
            {
                case MapType.Street:
                    NativeMap.MapType = MapViewType.Normal;
                    break;
                case MapType.Satellite:
                    NativeMap.MapType = MapViewType.Satellite;
                    break;
                case MapType.Hybrid:
                    NativeMap.MapType = MapViewType.Hybrid;
                    break;
                case MapType.Terrain:
                    NativeMap.MapType = MapViewType.Terrain;
                    break;
                case MapType.None:
                    NativeMap.MapType = MapViewType.None;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void UpdatePadding()
        {
            NativeMap.Padding = Map.Padding.ToUIEdgeInsets();
        }

        void UpdateMapStyle()
        {
            if (Map.MapStyle == null)
            {
                NativeMap.MapStyle = null;
            }
            else
            {
                var mapStyle = Google.Maps.MapStyle.FromJson(Map.MapStyle.JsonStyle, null);
                NativeMap.MapStyle = mapStyle;
            }
        }

        #region Overridable Members

        /// <summary>
        /// Call when before marker create.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">the marker options.</param>
        protected virtual void OnMarkerCreating(Pin outerItem, Marker innerItem)
        {
        }

        /// <summary>
        /// Call when after marker create.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">thr marker.</param>
        protected virtual void OnMarkerCreated(Pin outerItem, Marker innerItem)
        {
        }

        /// <summary>
        /// Call when before marker delete.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">thr marker.</param>
        protected virtual void OnMarkerDeleting(Pin outerItem, Marker innerItem)
        {
        }

        /// <summary>
        /// Call when after marker delete.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">thr marker.</param>
        protected virtual void OnMarkerDeleted(Pin outerItem, Marker innerItem)
        {
        }

        #endregion    
    }
}
