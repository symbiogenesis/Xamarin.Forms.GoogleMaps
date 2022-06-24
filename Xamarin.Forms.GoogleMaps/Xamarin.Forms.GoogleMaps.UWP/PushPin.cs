using System;

using Microsoft.Toolkit.Uwp.UI;

using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

using Xamarin.Forms.GoogleMaps.UWP.Extensions;
using Xamarin.Forms.Platform.UWP;

#if WINDOWS_UWP

namespace Xamarin.Forms.GoogleMaps.UWP
#else

namespace Xamarin.Forms.Maps.WinRT
#endif
{
    internal class PushPin : ContentControl
    {
        private static readonly ColorConverter _colorConverter = new();
        private static readonly ViewToRendererConverter _viewToRendererConverter = new();
        private static readonly Windows.Foundation.Point _anchor = new(0.5, 1);
        private static readonly Windows.UI.Xaml.Thickness _detailsPadding = new(5);
        private static readonly Windows.UI.Xaml.Media.SolidColorBrush _whiteBrush = new(Colors.White);
        private static readonly Windows.UI.Xaml.Media.SolidColorBrush _blackBrush = new(Colors.Black);

        private static Windows.UI.Xaml.DataTemplate _template;

        private readonly Pin _pin;

        internal PushPin(Pin pin)
        {
            if (pin == null)
                throw new ArgumentNullException();

            if (_template == null)
                _template = Windows.UI.Xaml.Application.Current.Resources["PushPinTemplate"] as Windows.UI.Xaml.DataTemplate;

            SetupDetailsView(pin);
            UpdateIcon(pin);

            Content = Root;

            Id = Guid.NewGuid();
            DataContext = _pin = pin;

            UpdateLocation();

            pin.NativeObject = this;
        }

        ~PushPin()
        {
            try
            {
                DetailsView.Tapped -= DetailsViewOnTapped;
            }
            catch { }
        }

        public Guid Id { get; set; }

        public StackPanel Root { get; set; } = new() { Width = 250 };
        public StackPanel DetailsView { get; set; }
        public TextBlock PinLabel { get; set; }
        public TextBlock Address { get; set; }
        public FrameworkElement Icon { get; set; }

        public event EventHandler<TappedRoutedEventArgs> InfoWindowClicked;

        private void SetupDetailsView(Pin pin)
        {
            //Setup details view
            DetailsView = new StackPanel()
            {
                Width = 250,
                Height = 70,
                Opacity = 0.7,
                Padding = _detailsPadding,
                Background = _whiteBrush
            };
            PinLabel = new TextBlock()
            {
                Text = pin.Label,
                Foreground = _blackBrush,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.WrapWholeWords,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Address = new TextBlock()
            {
                Text = pin.Address ?? string.Empty,
                Foreground = _blackBrush,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            DetailsView.Children.Add(PinLabel);
            if (!string.IsNullOrEmpty(pin.Address))
            {
                DetailsView.Children.Add(Address);
            }
            else
            {
                DetailsView.Height = 35;
            }
            DetailsView.Visibility = Visibility.Collapsed;
            DetailsView.Tapped += DetailsViewOnTapped;
            Root.Children.Add(DetailsView);
        }

        public void UpdateIcon(Pin pin)
        {
            if (pin.Icon == null || pin.Icon.Type == BitmapDescriptorType.Default)
            {
                var content = _template.LoadContent();
                if (content is Path path)
                {
                    if (pin.Icon != null && pin.Icon.Color != Color.Black)
                    {
                        path.Fill = (Windows.UI.Xaml.Media.SolidColorBrush)_colorConverter.Convert(pin.Icon.Color, null, null, null);
                    }
                    if (Icon != null)
                    {
                        Root.Children.Remove(Icon);
                    }
                    Icon = path;
                    Root.Children.Add(Icon);
                }
            }
            else
            {
                if (pin.Icon.Type != BitmapDescriptorType.View)
                {
                    var image = new Windows.UI.Xaml.Controls.Image()
                    {
                        Source = pin.Icon.ToBitmapDescriptor(),
                        Width = 50,
                    };
                    if (Icon != null)
                    {
                        Root.Children.Remove(Icon);
                    }
                    Icon = image;
                    Root.Children.Add(Icon);
                }
                else
                {
                    TransformXamarinViewToUWPBitmap(pin);
                }
            }

            FrameworkElementExtensions.SetCursor(Icon, Windows.UI.Core.CoreCursorType.Hand);
        }

        public void UpdateLocation()
        {
            var location = new Geopoint(new BasicGeoposition
            {
                Latitude = _pin.Position.Latitude,
                Longitude = _pin.Position.Longitude
            });
            MapControl.SetLocation(this, location);
            MapControl.SetNormalizedAnchorPoint(this, _anchor);
        }

        private void TransformXamarinViewToUWPBitmap(Pin outerItem)
        {
            if (outerItem?.Icon?.Type == BitmapDescriptorType.View && outerItem?.Icon?.View != null)
            {
                var iconView = outerItem.Icon.View;

                var frameworkElement = (FrameworkElement)_viewToRendererConverter.Convert(iconView, null, null, null);

                frameworkElement.Height = iconView.HeightRequest;
                frameworkElement.Width = iconView.WidthRequest;

                if (Icon != null)
                {
                    Root.Children.Remove(Icon);
                }
                Icon = frameworkElement;
                Root.Children.Add(Icon);
            }
        }

        private void DetailsViewOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            InfoWindowClicked?.Invoke(_pin, tappedRoutedEventArgs);
        }
    }
}