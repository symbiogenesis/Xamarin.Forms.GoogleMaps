using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms.GoogleMaps.Extensions;
using Xamarin.Forms.GoogleMaps.Helpers;
using Xamarin.Forms.GoogleMaps.Internals;

namespace Xamarin.Forms.GoogleMaps
{
    public class Map : View, IEnumerable<Pin>
    {
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(Map), default(IEnumerable),
            propertyChanged: (b, o, n) => ((Map)b).OnItemsSourcePropertyChanged((IEnumerable)o, (IEnumerable)n));

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(Map), default(DataTemplate),
            propertyChanged: (b, o, n) => ((Map)b).OnItemTemplatePropertyChanged((DataTemplate)o, (DataTemplate)n));

        public static readonly BindableProperty ItemTemplateSelectorProperty = BindableProperty.Create(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(Map), default(DataTemplateSelector),
            propertyChanged: (b, o, n) => ((Map)b).OnItemTemplateSelectorPropertyChanged());

        public static readonly BindableProperty MapTypeProperty = BindableProperty.Create(nameof(MapType), typeof(MapType), typeof(Map), default(MapType));

#pragma warning disable CS0618 // Type or member is obsolete
        public static readonly BindableProperty IsShowingUserProperty = BindableProperty.Create(nameof(IsShowingUser), typeof(bool), typeof(Map), default(bool));

        public static readonly BindableProperty MyLocationEnabledProperty = BindableProperty.Create(nameof(MyLocationEnabled), typeof(bool), typeof(Map), default(bool));

        public static readonly BindableProperty HasScrollEnabledProperty = BindableProperty.Create(nameof(HasScrollEnabled), typeof(bool), typeof(Map), true);

        public static readonly BindableProperty HasZoomEnabledProperty = BindableProperty.Create(nameof(HasZoomEnabled), typeof(bool), typeof(Map), true);

        public static readonly BindableProperty HasRotationEnabledProperty = BindableProperty.Create(nameof(HasRotationEnabled), typeof(bool), typeof(Map), true);
#pragma warning restore CS0618 // Type or member is obsolete

        public static readonly BindableProperty SelectedPinProperty = BindableProperty.Create(nameof(SelectedPin), typeof(Pin), typeof(Map), default(Pin), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty IsTrafficEnabledProperty = BindableProperty.Create(nameof(IsTrafficEnabled), typeof(bool), typeof(Map), false);

        public static readonly BindableProperty IndoorEnabledProperty = BindableProperty.Create(nameof(IsIndoorEnabled), typeof(bool), typeof(Map), true);

        public static readonly BindableProperty InitialCameraUpdateProperty = BindableProperty.Create(
            nameof(InitialCameraUpdate), typeof(CameraUpdate), typeof(Map),
            CameraUpdateFactory.NewPositionZoom(new Position(41.89, 12.49), 10),  // center on Rome by default
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((Map)bindable)._useMoveToRegisonAsInitialBounds = false;
            });

        public static readonly BindableProperty NewCameraUpdateProperty = BindableProperty.Create(
            nameof(NewCameraUpdate), typeof(CameraUpdate), typeof(Map),
            null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var cameraUpdate = newValue as CameraUpdate;

                if (bindable != null && cameraUpdate != null)
                {
                    ((Map)bindable).MoveCamera(cameraUpdate);
                }
            });

        public static readonly BindableProperty PaddingProperty = BindableProperty.Create(nameof(Padding), typeof(Thickness), typeof(Map), default(Thickness));

        public static readonly BindableProperty PinItemsProperty = BindableProperty.Create(nameof(PinItems), typeof(IEnumerable<Pin>), typeof(Map), default(IEnumerable<Pin>),
            propertyChanged: (b, o, n) => ((Map)b).OnPinItemsPropertyChanged((IEnumerable<Pin>)o, (IEnumerable<Pin>)n));

        public static readonly BindableProperty PolylineItemsProperty = BindableProperty.Create(nameof(PolylineItems), typeof(IEnumerable<Polyline>), typeof(Map), default(IEnumerable<Polyline>),
            propertyChanged: (b, o, n) => ((Map)b).OnPolylineItemsPropertyChanged((IEnumerable<Polyline>)o, (IEnumerable<Polyline>)n));

        public static readonly BindableProperty PolygonItemsProperty = BindableProperty.Create(nameof(PolygonItems), typeof(IEnumerable<Polygon>), typeof(Map), default(IEnumerable<Polygon>),
            propertyChanged: (b, o, n) => ((Map)b).OnPolygonItemsPropertyChanged((IEnumerable<Polygon>)o, (IEnumerable<Polygon>)n));

        public static readonly BindableProperty CircleItemsProperty = BindableProperty.Create(nameof(CircleItems), typeof(IEnumerable<Circle>), typeof(Map), default(IEnumerable<Circle>),
            propertyChanged: (b, o, n) => ((Map)b).OnCircleItemsPropertyChanged((IEnumerable<Circle>)o, (IEnumerable<Circle>)n));

        public static readonly BindableProperty TileLayerItemsProperty = BindableProperty.Create(nameof(TileLayerItems), typeof(IEnumerable<TileLayer>), typeof(Map), default(IEnumerable<TileLayer>),
            propertyChanged: (b, o, n) => ((Map)b).OnTileLayerItemsPropertyChanged((IEnumerable<TileLayer>)o, (IEnumerable<TileLayer>)n));

        public static readonly BindableProperty GroundOverlayItemsProperty = BindableProperty.Create(nameof(GroundOverlayItems), typeof(IEnumerable<GroundOverlay>), typeof(Map), default(IEnumerable<GroundOverlay>),
            propertyChanged: (b, o, n) => ((Map)b).OnGroundOverlayItemsPropertyChanged((IEnumerable<GroundOverlay>)o, (IEnumerable<GroundOverlay>)n));

        private bool _useMoveToRegisonAsInitialBounds = true;

        public static readonly BindableProperty CameraPositionProperty = BindableProperty.Create(
            nameof(CameraPosition), typeof(CameraPosition), typeof(Map),
            defaultValueCreator: (bindable) => new CameraPosition(((Map)bindable).InitialCameraUpdate.Position, 10),
            defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty MapStyleProperty = BindableProperty.Create(nameof(MapStyle), typeof(MapStyle), typeof(Map), null);

        private readonly ObservableCollection<Pin> _pins = new ObservableCollection<Pin>();
        private readonly ObservableCollection<Polyline> _polylines = new ObservableCollection<Polyline>();
        private readonly ObservableCollection<Polygon> _polygons = new ObservableCollection<Polygon>();
        private readonly ObservableCollection<Circle> _circles = new ObservableCollection<Circle>();
        private readonly ObservableCollection<TileLayer> _tileLayers = new ObservableCollection<TileLayer>();
        private readonly ObservableCollection<GroundOverlay> _groundOverlays = new ObservableCollection<GroundOverlay>();

        public event EventHandler<PinClickedEventArgs> PinClicked;

        public event EventHandler<SelectedPinChangedEventArgs> SelectedPinChanged;

        public event EventHandler<InfoWindowClickedEventArgs> InfoWindowClicked;

        public event EventHandler<InfoWindowLongClickedEventArgs> InfoWindowLongClicked;

        public event EventHandler<PinDragEventArgs> PinDragStart;

        public event EventHandler<PinDragEventArgs> PinDragEnd;

        public event EventHandler<PinDragEventArgs> PinDragging;

        public event EventHandler<MapClickedEventArgs> MapClicked;

        public event EventHandler<MapLongClickedEventArgs> MapLongClicked;

        public event EventHandler<MyLocationButtonClickedEventArgs> MyLocationButtonClicked;

        [Obsolete("Please use Map.CameraIdled instead of this")]
        public event EventHandler<CameraChangedEventArgs> CameraChanged;

        public event EventHandler<CameraMoveStartedEventArgs> CameraMoveStarted;

        public event EventHandler<CameraMovingEventArgs> CameraMoving;

        public event EventHandler<CameraIdledEventArgs> CameraIdled;

        internal Action<MoveToRegionMessage> OnMoveToRegion { get; set; }

        internal Action<CameraUpdateMessage> OnMoveCamera { get; set; }

        internal Action<CameraUpdateMessage> OnAnimateCamera { get; set; }

        internal Action<TakeSnapshotMessage> OnSnapshot { get; set; }

        internal Func<Point, Position> OnFromScreenLocation { get; set; }
        internal Func<Position, Point> OnToScreenLocation { get; set; }

        private MapSpan _visibleRegion;
        private MapRegion _region;

        //// Simone Marra
        //public static Position _TopLeft = new Position();
        //public static Position _TopRight = new Position();
        //public static Position _BottomLeft = new Position();
        //public static Position _BottomRight = new Position();
        //// End Simone Marra

        public Map()
        {
            VerticalOptions = HorizontalOptions = LayoutOptions.FillAndExpand;

            _pins.CollectionChanged += PinsOnCollectionChanged;
            _polylines.CollectionChanged += PolylinesOnCollectionChanged;
            _polygons.CollectionChanged += PolygonsOnCollectionChanged;
            _circles.CollectionChanged += CirclesOnCollectionChanged;
            _tileLayers.CollectionChanged += TileLayersOnCollectionChanged;
            _groundOverlays.CollectionChanged += GroundOverlays_CollectionChanged;
        }

        [Obsolete("Please use Map.UiSettings.ScrollGesturesEnabled instead of this")]
        public bool HasScrollEnabled
        {
            get { return (bool)GetValue(HasScrollEnabledProperty); }
            set { SetValue(HasScrollEnabledProperty, value); }
        }

        [Obsolete("Please use Map.UiSettings.ZoomGesturesEnabled and ZoomControlsEnabled instead of this")]
        public bool HasZoomEnabled
        {
            get { return (bool)GetValue(HasZoomEnabledProperty); }
            set { SetValue(HasZoomEnabledProperty, value); }
        }

        [Obsolete("Please use Map.UiSettings.RotateGesturesEnabled instead of this")]
        public bool HasRotationEnabled
        {
            get { return (bool)GetValue(HasRotationEnabledProperty); }
            set { SetValue(HasRotationEnabledProperty, value); }
        }

        public bool IsTrafficEnabled
        {
            get { return (bool)GetValue(IsTrafficEnabledProperty); }
            set { SetValue(IsTrafficEnabledProperty, value); }
        }

        public bool IsIndoorEnabled
        {
            get { return (bool)GetValue(IndoorEnabledProperty); }
            set { SetValue(IndoorEnabledProperty, value); }
        }

        [Obsolete("Please use Map.MyLocationEnabled and Map.UiSettings.MyLocationButtonEnabled instead of this")]
        public bool IsShowingUser
        {
            get { return (bool)GetValue(IsShowingUserProperty); }
            set { SetValue(IsShowingUserProperty, value); }
        }

        public bool MyLocationEnabled
        {
            get { return (bool)GetValue(MyLocationEnabledProperty); }
            set { SetValue(MyLocationEnabledProperty, value); }
        }

        public MapType MapType
        {
            get { return (MapType)GetValue(MapTypeProperty); }
            set { SetValue(MapTypeProperty, value); }
        }

        public Pin SelectedPin
        {
            get { return (Pin)GetValue(SelectedPinProperty); }
            set { SetValue(SelectedPinProperty, value); }
        }

        [TypeConverter(typeof(CameraUpdateConverter))]
        public CameraUpdate InitialCameraUpdate
        {
            get { return (CameraUpdate)GetValue(InitialCameraUpdateProperty); }
            set { SetValue(InitialCameraUpdateProperty, value); }
        }

        [TypeConverter(typeof(CameraUpdateConverter))]
        public CameraUpdate NewCameraUpdate
        {
            get { return (CameraUpdate)GetValue(NewCameraUpdateProperty); }
            set { SetValue(NewCameraUpdateProperty, value); }
        }

        public CameraPosition CameraPosition
        {
            get { return (CameraPosition)GetValue(CameraPositionProperty); }
            internal set { SetValue(CameraPositionProperty, value); }
        }

        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        public MapStyle MapStyle
        {
            get { return (MapStyle)GetValue(MapStyleProperty); }
            set { SetValue(MapStyleProperty, value); }
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        public IEnumerable<Pin> PinItems
        {
            get => (IEnumerable<Pin>)GetValue(PinItemsProperty);
            set => SetValue(PinItemsProperty, value);
        }

        public IEnumerable<Polyline> PolylineItems
        {
            get => (IEnumerable<Polyline>)GetValue(PolylineItemsProperty);
            set => SetValue(PolylineItemsProperty, value);
        }

        public IEnumerable<Polygon> PolygonItems
        {
            get => (IEnumerable<Polygon>)GetValue(PolygonItemsProperty);
            set => SetValue(PolygonItemsProperty, value);
        }

        public IEnumerable<Circle> CircleItems
        {
            get => (IEnumerable<Circle>)GetValue(CircleItemsProperty);
            set => SetValue(CircleItemsProperty, value);
        }

        public IEnumerable<TileLayer> TileLayerItems
        {
            get => (IEnumerable<TileLayer>)GetValue(TileLayerItemsProperty);
            set => SetValue(TileLayerItemsProperty, value);
        }

        public IEnumerable<GroundOverlay> GroundOverlayItems
        {
            get => (IEnumerable<GroundOverlay>)GetValue(GroundOverlayItemsProperty);
            set => SetValue(GroundOverlayItemsProperty, value);
        }

        public IList<Pin> Pins
        {
            get { return _pins; }
        }

        public IList<Polyline> Polylines
        {
            get { return _polylines; }
        }

        public IList<Polygon> Polygons
        {
            get { return _polygons; }
        }

        public IList<Circle> Circles
        {
            get { return _circles; }
        }

        public IList<TileLayer> TileLayers
        {
            get { return _tileLayers; }
        }

        public IList<GroundOverlay> GroundOverlays
        {
            get { return _groundOverlays; }
        }

        [Obsolete("Please use Map.Region instead of this")]
        public MapSpan VisibleRegion
        {
            get { return _visibleRegion; }
            internal set
            {
                if (_visibleRegion == value)
                    return;
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                OnPropertyChanging();
                _visibleRegion = value;
                OnPropertyChanged();
            }
        }

        public MapRegion Region
        {
            get { return _region; }
            internal set
            {
                if (_region == value)
                    return;
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                OnPropertyChanging();
                _region = value;
                OnPropertyChanged();
            }
        }

        public UiSettings UiSettings { get; } = new UiSettings();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Pin> GetEnumerator()
        {
            return _pins.GetEnumerator();
        }

        public void MoveToRegion(MapSpan mapSpan, bool animate = true)
        {
            if (mapSpan == null)
                throw new ArgumentNullException(nameof(mapSpan));

            if (_useMoveToRegisonAsInitialBounds)
            {
                InitialCameraUpdate = CameraUpdateFactory.NewBounds(mapSpan.ToBounds(), 0);
                _useMoveToRegisonAsInitialBounds = false;
            }

            SendMoveToRegion(new MoveToRegionMessage(mapSpan, animate));
        }

        public Task<AnimationStatus> MoveCamera(CameraUpdate cameraUpdate)
        {
            var comp = new TaskCompletionSource<AnimationStatus>();

            SendMoveCamera(new CameraUpdateMessage(cameraUpdate, null, new DelegateAnimationCallback(
                () => comp.SetResult(AnimationStatus.Finished),
                () => comp.SetResult(AnimationStatus.Canceled))));

            return comp.Task;
        }

        public Task<AnimationStatus> AnimateCamera(CameraUpdate cameraUpdate, TimeSpan? duration = null)
        {
            var comp = new TaskCompletionSource<AnimationStatus>();

            SendAnimateCamera(new CameraUpdateMessage(cameraUpdate, duration, new DelegateAnimationCallback(
                () => comp.SetResult(AnimationStatus.Finished),
                () => comp.SetResult(AnimationStatus.Canceled))));

            return comp.Task;
        }

        public Task<Stream> TakeSnapshot()
        {
            var comp = new TaskCompletionSource<Stream>();

            SendTakeSnapshot(new TakeSnapshotMessage(image => comp.SetResult(image)));

            return comp.Task;
        }

        private void PinsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems?.Cast<Pin>().Any(pin => pin.Label == null) == true)
                throw new ArgumentException("Pin must have a Label to be added to a map");
        }

        private void PolylinesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems?.Cast<Polyline>().Any(polyline => polyline.Positions.Count < 2) == true)
                throw new ArgumentException("Polyline must have a 2 positions to be added to a map");
        }

        private void PolygonsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems?.Cast<Polygon>().Any(polygon => polygon.Positions.Count < 3) == true)
                throw new ArgumentException("Polygon must have a 3 positions to be added to a map");
        }

        private void CirclesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems?.Cast<Circle>().Any(circle => circle?.Center == null || circle?.Radius == null || circle.Radius.Meters <= 0f) == true)
                throw new ArgumentException("Circle must have a center and radius");
        }

        private void TileLayersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.NewItems != null && e.NewItems.Cast<ITileLayer>().Any(tileLayer => (circle.Center == null || circle.Radius == null || circle.Radius.Meters <= 0f)))
            //  throw new ArgumentException("Circle must have a center and radius");
        }

        private void GroundOverlays_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        internal void SendSelectedPinChanged(Pin selectedPin)
        {
            SelectedPinChanged?.Invoke(this, new SelectedPinChangedEventArgs(selectedPin));
        }

        private void OnItemTemplateSelectorPropertyChanged()
        {
            _pins.Clear();
            CreatePinItems();
        }

        internal bool SendPinClicked(Pin pin)
        {
            var args = new PinClickedEventArgs(pin);
            PinClicked?.Invoke(this, args);
            return args.Handled;
        }

        internal void SendInfoWindowClicked(Pin pin)
        {
            var args = new InfoWindowClickedEventArgs(pin);
            InfoWindowClicked?.Invoke(this, args);
        }

        internal void SendInfoWindowLongClicked(Pin pin)
        {
            var args = new InfoWindowLongClickedEventArgs(pin);
            InfoWindowLongClicked?.Invoke(this, args);
        }

        internal void SendPinDragStart(Pin pin)
        {
            PinDragStart?.Invoke(this, new PinDragEventArgs(pin));
        }

        internal void SendPinDragEnd(Pin pin)
        {
            PinDragEnd?.Invoke(this, new PinDragEventArgs(pin));
        }

        internal void SendPinDragging(Pin pin)
        {
            PinDragging?.Invoke(this, new PinDragEventArgs(pin));
        }

        internal void SendMapClicked(Position point)
        {
            MapClicked?.Invoke(this, new MapClickedEventArgs(point));
        }

        internal void SendMapLongClicked(Position point)
        {
            MapLongClicked?.Invoke(this, new MapLongClickedEventArgs(point));
        }

        internal bool SendMyLocationClicked()
        {
            var args = new MyLocationButtonClickedEventArgs();
            MyLocationButtonClicked?.Invoke(this, args);
            return args.Handled;
        }

        internal void SendCameraChanged(CameraPosition position)
        {
            CameraChanged?.Invoke(this, new CameraChangedEventArgs(position));
        }

        internal void SendCameraMoveStarted(bool isGesture)
        {
            CameraMoveStarted?.Invoke(this, new CameraMoveStartedEventArgs(isGesture));
        }

        internal void SendCameraMoving(CameraPosition position)
        {
            CameraMoving?.Invoke(this, new CameraMovingEventArgs(position));
        }

        internal void SendCameraIdled(CameraPosition position)
        {
            CameraIdled?.Invoke(this, new CameraIdledEventArgs(position));
        }

        private void SendMoveToRegion(MoveToRegionMessage message)
        {
            OnMoveToRegion?.Invoke(message);
        }

        private void SendMoveCamera(CameraUpdateMessage message)
        {
            OnMoveCamera?.Invoke(message);
        }

        private void SendAnimateCamera(CameraUpdateMessage message)
        {
            OnAnimateCamera?.Invoke(message);
        }

        private void SendTakeSnapshot(TakeSnapshotMessage message)
        {
            OnSnapshot?.Invoke(message);
        }

        private void OnItemsSourcePropertyChanged(IEnumerable oldItemsSource, IEnumerable newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnItemsSourceCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            _pins.Clear();
            CreatePinItems();
        }

        private void OnItemTemplatePropertyChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
        {
            if (newItemTemplate is DataTemplateSelector)
            {
                throw new NotSupportedException($"You are using an instance of {nameof(DataTemplateSelector)} to set the {nameof(Map)}.{ItemTemplateProperty.PropertyName} property. Use an instance of a {nameof(DataTemplate)} property instead to set an item template.");
            }

            _pins.Clear();
            CreatePinItems();
        }

        private void OnPinItemsPropertyChanged(IEnumerable<Pin> oldItemsSource, IEnumerable<Pin> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged oldNcc)
            {
                oldNcc.CollectionChanged -= OnPinItemsCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged newNcc)
            {
                newNcc.CollectionChanged += OnPinItemsCollectionChanged;
            }

            _pins.Clear();

            foreach (var pin in newItemsSource)
            {
                _pins.Add(pin);
            }
        }

        private void OnPinItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Pin item in e.NewItems)
                        _pins.Add(item);
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Pin item in e.OldItems)
                        _pins.Remove(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Pin item in e.OldItems)
                        _pins.Remove(item);
                    foreach (Pin item in e.NewItems)
                        _pins.Add(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _pins.Clear();
                    break;
            }
        }

        private void OnPolylineItemsPropertyChanged(IEnumerable<Polyline> oldItemsSource, IEnumerable<Polyline> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnPolylineItemsCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnPolylineItemsCollectionChanged;
            }

            _polylines.Clear();

            foreach (var polyline in newItemsSource)
            {
                _polylines.Add(polyline);
            }
        }

        private void OnPolylineItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Polyline item in e.NewItems)
                        _polylines.Add(item);
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Polyline item in e.OldItems)
                        _polylines.Remove(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Polyline item in e.OldItems)
                        _polylines.Remove(item);
                    foreach (Polyline item in e.NewItems)
                        _polylines.Add(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _polylines.Clear();
                    break;
            }
        }

        private void OnPolygonItemsPropertyChanged(IEnumerable<Polygon> oldItemsSource, IEnumerable<Polygon> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnPolygonItemsCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnPolygonItemsCollectionChanged;
            }

            _polygons.Clear();

            foreach (var polygon in newItemsSource)
            {
                _polygons.Add(polygon);
            }
        }

        private void OnPolygonItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Polygon item in e.NewItems)
                        _polygons.Add(item);
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Polygon item in e.OldItems)
                        _polygons.Remove(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Polygon item in e.OldItems)
                        _polygons.Remove(item);
                    foreach (Polygon item in e.NewItems)
                        _polygons.Add(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _polygons.Clear();
                    break;
            }
        }

        private void OnCircleItemsPropertyChanged(IEnumerable<Circle> oldItemsSource, IEnumerable<Circle> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnCircleItemsCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnCircleItemsCollectionChanged;
            }

            _circles.Clear();

            foreach (var circle in newItemsSource)
            {
                _circles.Add(circle);
            }
        }

        private void OnCircleItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.NewItems)
                        _circles.Add(item);
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.OldItems)
                        _circles.Remove(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.OldItems)
                        _circles.Remove(item);
                    foreach (Circle item in e.NewItems)
                        _circles.Add(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _circles.Clear();
                    break;
            }
        }

        private void OnTileLayerItemsPropertyChanged(IEnumerable<TileLayer> oldItemsSource, IEnumerable<TileLayer> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnTileLayerItemsCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnTileLayerItemsCollectionChanged;
            }

            _tileLayers.Clear();

            foreach (var tileLayer in newItemsSource)
            {
                _tileLayers.Add(tileLayer);
            }
        }

        private void OnTileLayerItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (TileLayer item in e.NewItems)
                        _tileLayers.Add(item);
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (TileLayer item in e.OldItems)
                        _tileLayers.Remove(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (TileLayer item in e.OldItems)
                        _tileLayers.Remove(item);
                    foreach (TileLayer item in e.NewItems)
                        _tileLayers.Add(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _tileLayers.Clear();
                    break;
            }
        }

        private void OnGroundOverlayItemsPropertyChanged(IEnumerable<GroundOverlay> oldItemsSource, IEnumerable<GroundOverlay> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnGroundOverlayItemsCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnGroundOverlayItemsCollectionChanged;
            }

            _groundOverlays.Clear();

            foreach (var groundOverlay in newItemsSource)
            {
                _groundOverlays.Add(groundOverlay);
            }
        }

        private void OnGroundOverlayItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (GroundOverlay item in e.NewItems)
                        _groundOverlays.Add(item);
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (GroundOverlay item in e.OldItems)
                        _groundOverlays.Remove(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (GroundOverlay item in e.OldItems)
                        _groundOverlays.Remove(item);
                    foreach (GroundOverlay item in e.NewItems)
                        _groundOverlays.Add(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _groundOverlays.Clear();
                    break;
            }
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (object item in e.NewItems)
                        CreatePin(item);
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (object item in e.OldItems)
                        RemovePin(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (object item in e.OldItems)
                        RemovePin(item);
                    foreach (object item in e.NewItems)
                        CreatePin(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _pins.Clear();
                    break;
            }
        }

        private void CreatePinItems()
        {
            if (ItemsSource == null || (ItemTemplate == null && ItemTemplateSelector == null))
            {
                return;
            }

            foreach (object item in ItemsSource)
            {
                CreatePin(item);
            }
        }

        private void CreatePin(object newItem)
        {
            DataTemplate itemTemplate = ItemTemplate;
            if (itemTemplate == null)
                itemTemplate = ItemTemplateSelector?.SelectTemplate(newItem, this);

            var pin = (Pin)itemTemplate.CreateContent();
            pin.BindingContext = newItem;
            _pins.Add(pin);
        }

        private void RemovePin(object itemToRemove)
        {
            Pin pinToRemove = _pins.FirstOrDefault(pin => pin.BindingContext?.Equals(itemToRemove) == true);
            if (pinToRemove != null)
            {
                _pins.Remove(pinToRemove);
            }
        }

        public Position FromScreenLocation(Point point)
        {
            if (OnFromScreenLocation == null)
            {
                throw new NullReferenceException("OnFromScreenLocation");
            }

            return OnFromScreenLocation.Invoke(point);
        }

        public Point ToScreenLocation(Position position)
        {
            if (OnToScreenLocation == null)
            {
                throw new NullReferenceException("ToScreenLocation");
            }

            return OnToScreenLocation.Invoke(position);
        }
    }
}
