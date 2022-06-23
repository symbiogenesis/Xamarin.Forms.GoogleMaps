using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Shapes;
using Xamarin.Forms.GoogleMaps.Internals;
using Xamarin.Forms.GoogleMaps.Logics;
using Xamarin.Forms.GoogleMaps.Logics.UWP;
using Xamarin.Forms.GoogleMaps.UWP.Extensions;
using Xamarin.Forms.GoogleMaps.UWP.Logics;
#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;

#else
using Xamarin.Forms.Platform.WinRT;

#endif

#if WINDOWS_UWP

namespace Xamarin.Forms.GoogleMaps.UWP
#else

namespace Xamarin.Forms.Maps.WinRT
#endif
{
    public class MapRenderer : ViewRenderer<Map, MapControl>
    {
        private static readonly Geopoint _topOfMap = new(new BasicGeoposition()
        {
            Latitude = 85,
            Longitude = 0
        });
        private static readonly Geopoint _bottomOfMap = new(new BasicGeoposition()
        {
            Latitude = -85,
            Longitude = 0
        });

        private readonly UiSettingsLogic _uiSettingsLogic = new();
        private readonly CameraLogic _cameraLogic ;

        private Map Map => Element;

        private MapControl NativeMap => Control;

        readonly BaseLogic<MapControl>[] _logics;

        public MapRenderer() : base()
        {
            _logics = new BaseLogic<MapControl>[]
            {
                new PinLogic(),
                new PolylineLogic(),
                new PolygonLogic(),
                new CircleLogic(),
                new TileLayerLogic(),
            };

            _cameraLogic = new CameraLogic();
        }

        protected override async void OnElementChanged(ElementChangedEventArgs<Map> e)
        {
            base.OnElementChanged(e);

            var oldMapView = Control;

            if (e.OldElement != null)
            {
                var mapModel = e.OldElement;
                mapModel.OnSnapshot -= OnSnapshot;
                _cameraLogic.Unregister();
                _uiSettingsLogic.Unregister();

                if (oldMapView != null)
                {
                    oldMapView.ActualCameraChanged -= OnActualCameraChanged;
                    oldMapView.ZoomLevelChanged -= OnZoomLevelChanged;
                    oldMapView.CenterChanged -= Control_CenterChanged;
                }
            }

            if (e.NewElement != null)
            {
                var mapModel = e.NewElement;
                if (Control == null)
                {
                    SetNativeControl(new MapControl());
                    Control.MapServiceToken = FormsGoogleMaps.AuthenticationToken;
                    Control.TrafficFlowVisible = Map.IsTrafficEnabled;
                    Control.ZoomLevelChanged += OnZoomLevelChanged;
                    Control.CenterChanged += Control_CenterChanged;
                    Control.ActualCameraChanged += OnActualCameraChanged;
                }

                _cameraLogic.Register(Map, NativeMap);
                Map.OnSnapshot += OnSnapshot;

                _uiSettingsLogic.Register(Map, NativeMap);
                _uiSettingsLogic.Initialize();
                UpdateMapType();
                UpdateHasScrollEnabled();
                UpdateHasZoomEnabled();
                UpdateHasRotationEnabled();

                await UpdateIsShowingUser(Element.MyLocationEnabled);

                foreach (var logic in _logics)
                {
                    logic.Register(oldMapView, e.OldElement, NativeMap, Map);
                    logic.RestoreItems();
                    logic.OnMapPropertyChanged(new PropertyChangedEventArgs(Map.SelectedPinProperty.PropertyName));
                }
            }
        }

        private void Control_CenterChanged(MapControl sender, object args)
        {
            UpdateVisibleRegion();
        }

        private void OnSnapshot(TakeSnapshotMessage snapshotMessage)
        {
            Task.Run(() =>
            {
                // Not implemented
                snapshotMessage?.OnSnapshot?.Invoke(null);
            });
        }

        private void OnZoomLevelChanged(MapControl sender, object args)
        {
            if (Map == null)
            {
                return;
            }

            var camera = sender.ActualCamera;
            var pos = new CameraPosition(
                camera.Location.Position.ToPosition(),
                sender.ZoomLevel,
                camera.Roll,
                camera.Pitch);
            Map.CameraPosition = pos;
            UpdateVisibleRegion();
            Map.SendCameraChanged(pos);
        }

        private void OnActualCameraChanged(MapControl sender, MapActualCameraChangedEventArgs args)
        {
            var camera = args.Camera;
            var pos = new CameraPosition(
                camera.Location.Position.ToPosition(),
                sender.ZoomLevel,
                camera.Heading,
                camera.Pitch);
            Map?.SendCameraChanged(pos);
        }

        protected override async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == Map.MapTypeProperty.PropertyName)
                UpdateMapType();
            else if (e.PropertyName == Map.IsShowingUserProperty.PropertyName)
                await UpdateIsShowingUser(Element.MyLocationEnabled);
            else if (e.PropertyName == Map.MyLocationEnabledProperty.PropertyName)
                await UpdateIsShowingUser(Element.MyLocationEnabled);
            else if (e.PropertyName == Map.HasScrollEnabledProperty.PropertyName)
                UpdateHasScrollEnabled();
            else if (e.PropertyName == Map.HasZoomEnabledProperty.PropertyName)
                UpdateHasZoomEnabled();
            else if (e.PropertyName == Map.IsTrafficEnabledProperty.PropertyName)
                Control.TrafficFlowVisible = Map.IsTrafficEnabled;
            else if (e.PropertyName == Map.HasRotationEnabledProperty.PropertyName)
                UpdateHasRotationEnabled();

            foreach (var logic in _logics)
            {
                logic.OnMapPropertyChanged(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                _uiSettingsLogic.Unregister();
                Map.OnSnapshot -= OnSnapshot;
                _cameraLogic.Unregister();

                foreach (var logic in _logics)
                    logic.Unregister(NativeMap, Map);

                var oldMapView = Control;
                if (oldMapView != null)
                {
                    oldMapView.ActualCameraChanged -= OnActualCameraChanged;
                    oldMapView.ZoomLevelChanged -= OnZoomLevelChanged;
                    oldMapView.CenterChanged -= Control_CenterChanged;
                }
            }
            base.Dispose(disposing);
        }

        bool _disposed;
        bool _firstZoomLevelChangeFired;
        Ellipse _userPositionCircle;

        async Task UpdateIsShowingUser(bool enabled)
        {
            if (enabled)
            {
                var myGeolocator = new Geolocator();
                if (myGeolocator.LocationStatus != PositionStatus.NotAvailable &&
                    myGeolocator.LocationStatus != PositionStatus.Disabled)
                {
                    var userPosition = await myGeolocator.GetGeopositionAsync();
                    if (userPosition?.Coordinate != null)
                        LoadUserPosition(userPosition.Coordinate, true);
                }
            }
            else if (_userPositionCircle != null && Control.Children.Contains(_userPositionCircle))
            {
                Control.Children.Remove(_userPositionCircle);
            }
        }

        void UpdateVisibleRegion()
        {
            if (Control == null || Element == null)
                return;

            if (!_firstZoomLevelChangeFired)
            {
                _cameraLogic.MoveCamera(Map.InitialCameraUpdate);

                _firstZoomLevelChangeFired = true;
                return;
            }

            try
            {
                var boundingBox = GetBounds(this.Control);
                if (boundingBox != null)
                {
                    //Hack 1 
                    //Begin
                    //var center = new Position(boundingBox.Center.Latitude, boundingBox.Center.Longitude);
                    //var latitudeDelta = Math.Abs(center.Latitude - boundingBox.NorthwestCorner.Latitude);
                    //var longitudeDelta = Math.Abs(center.Longitude - boundingBox.NorthwestCorner.Longitude);
                    //Element.VisibleRegion = new MapSpan(center, latitudeDelta, longitudeDelta);
                    //End

                    //Could it replace Hack 1?
                    var nearLeft = new Position(boundingBox.NorthwestCorner.Latitude, boundingBox.SoutheastCorner.Longitude);
                    var nearRight = boundingBox.SoutheastCorner.ToPosition();
                    var farLeft = boundingBox.NorthwestCorner.ToPosition();
                    var farRight = new Position(boundingBox.SoutheastCorner.Latitude, boundingBox.NorthwestCorner.Longitude);
                    Element.Region = new MapRegion(nearLeft, nearRight, farLeft, farRight);

                    // Simone Marra
                    UpdateCornersBounds(this.Control);
                    // End Simone Marra
                }
            }
            catch (Exception)
            {
                //couldnt update visible region
            }
        }

        // Simone Marra
        private void UpdateCornersBounds(MapControl map)
        {
            Geopoint topLeft;
            Geopoint topRight;
            Geopoint bottomLeft;
            Geopoint bottomRight;

            // TODO: [Simone] I'm not sure about the catch code... I have not tested it yet!

            try
            {
                map.GetLocationFromOffset(new Windows.Foundation.Point(0, 0), out topLeft);
            }
            catch
            {
                map.GetOffsetFromLocation(_topOfMap, out Windows.Foundation.Point topPoint);
                map.GetLocationFromOffset(new Windows.Foundation.Point(0, topPoint.Y), out topLeft);
            }

            try
            {
                map.GetLocationFromOffset(new Windows.Foundation.Point(map.ActualWidth, 0), out topRight);
            }
            catch
            {
                map.GetOffsetFromLocation(_topOfMap, out Windows.Foundation.Point topPoint);
                map.GetLocationFromOffset(new Windows.Foundation.Point(topPoint.X, topPoint.Y), out topRight);
            }

            try
            {
                map.GetLocationFromOffset(new Windows.Foundation.Point(map.ActualWidth, map.ActualHeight), out bottomRight);
            }
            catch
            {
                map.GetOffsetFromLocation(_bottomOfMap, out Windows.Foundation.Point bottomPoint);
                map.GetLocationFromOffset(new Windows.Foundation.Point(bottomPoint.X, bottomPoint.Y), out bottomRight);
            }

            try
            {
                map.GetLocationFromOffset(new Windows.Foundation.Point(0, map.ActualHeight), out bottomLeft);
            }
            catch
            {
                map.GetOffsetFromLocation(_bottomOfMap, out Windows.Foundation.Point bottomPoint);
                map.GetLocationFromOffset(new Windows.Foundation.Point(0, bottomPoint.Y), out bottomLeft);
            }

            if((topLeft != null) && (topRight != null) && (bottomLeft != null) && (bottomRight != null))
            {
                var farLeft = new Position(topLeft.Position.Latitude, topLeft.Position.Longitude);
                var farRight = new Position(topRight.Position.Latitude, topRight.Position.Longitude);
                var nearLeft = new Position(bottomLeft.Position.Latitude, bottomLeft.Position.Longitude);
                var nearRight = new Position(bottomRight.Position.Latitude, bottomRight.Position.Longitude);

                Element.Region = new MapRegion(nearLeft, nearRight, farLeft, farRight);
            }
        }
        // End Simone Marra

        private static GeoboundingBox GetBounds(MapControl map)
        {
            Geopoint topLeft;

            try
            {
                map.GetLocationFromOffset(new Windows.Foundation.Point(0, 0), out topLeft);
            }
            catch
            {
                map.GetOffsetFromLocation(_topOfMap, out Windows.Foundation.Point topPoint);
                map.GetLocationFromOffset(new Windows.Foundation.Point(0, topPoint.Y), out topLeft);
            }

            Geopoint bottomRight;
            try
            {
                map.GetLocationFromOffset(new Windows.Foundation.Point(map.ActualWidth, map.ActualHeight), out bottomRight);
            }
            catch
            {
                map.GetOffsetFromLocation(_bottomOfMap, out Windows.Foundation.Point bottomPoint);
                map.GetLocationFromOffset(new Windows.Foundation.Point(bottomPoint.X, bottomPoint.Y), out bottomRight);
            }

            if (topLeft != null && bottomRight != null)
            {
                try
                {
                    return new GeoboundingBox(topLeft.Position, bottomRight.Position);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private void CreateCircle()
        {
            if (_userPositionCircle == null)
            {
                _userPositionCircle = new Ellipse
                {
                    Stroke = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.White),
                    Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Blue),
                    StrokeThickness = 2,
                    Height = 20,
                    Width = 20,
                    Opacity = 50
                };
            }
        }

        void LoadUserPosition(Geocoordinate userCoordinate, bool center)
        {
            var userPosition = new BasicGeoposition
            {
                Latitude = userCoordinate.Point.Position.Latitude,
                Longitude = userCoordinate.Point.Position.Longitude
            };

            var point = new Geopoint(userPosition);

            CreateCircle();

            if (Control.Children.Contains(_userPositionCircle))
                Control.Children.Remove(_userPositionCircle);

            MapControl.SetLocation(_userPositionCircle, point);
            MapControl.SetNormalizedAnchorPoint(_userPositionCircle, new Windows.Foundation.Point(0.5, 0.5));

            Control.Children.Add(_userPositionCircle);

            if (center)
            {
                Control.Center = point;
                Control.ZoomLevel = 13;
            }
        }

        void UpdateMapType()
        {
            switch (Element.MapType)
            {
                case MapType.Street:
                    Control.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
                    break;
                case MapType.Satellite:
                    Control.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Aerial;
                    break;
                case MapType.Hybrid:
                    Control.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.AerialWithRoads;
                    break;
                case MapType.Terrain:
                    Control.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Terrain;
                    break;
                case MapType.None:
                    Control.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.None;
                    break;
            }
        }

#if WINDOWS_UWP
        void UpdateHasZoomEnabled()
        {
            Control.ZoomInteractionMode = Map.UiSettings.ZoomControlsEnabled
                ? MapInteractionMode.GestureAndControl
                : MapInteractionMode.Disabled;
        }

        void UpdateHasScrollEnabled()
        {
            Control.PanInteractionMode = Element.UiSettings.ScrollGesturesEnabled ? MapPanInteractionMode.Auto : MapPanInteractionMode.Disabled;
        }

        void UpdateHasRotationEnabled()
        {
            Map.UiSettings.RotateGesturesEnabled = Map.UiSettings.RotateGesturesEnabled;
        }
#else
        void UpdateHasZoomEnabled()
        {
        }

        void UpdateHasScrollEnabled()
        {
        }

        void UpdateHasRotationEnabled()
        {
        }
#endif
    }
}
