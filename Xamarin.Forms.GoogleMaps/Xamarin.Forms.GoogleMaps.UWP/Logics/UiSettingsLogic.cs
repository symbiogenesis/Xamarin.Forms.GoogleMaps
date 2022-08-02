using Windows.UI.Xaml.Controls.Maps;

namespace Xamarin.Forms.GoogleMaps.Logics.UWP
{
    internal sealed class UiSettingsLogic : BaseUiSettingsLogic<MapControl>
    {
        public UiSettingsLogic() : base()
        {
        }

        protected override void OnUpdateCompassEnabled()
        {
            //TODO OnUpdateCompassEnabled
            System.Diagnostics.Debug.WriteLine("TODO: OnUpdateCompassEnabled");
        }

        protected override void OnUpdateRotateGesturesEnabled()
        {
            NativeMap.RotateInteractionMode = Map.UiSettings.RotateGesturesEnabled ?
                MapInteractionMode.Auto : MapInteractionMode.Disabled;

            if (Map.UiSettings.RotateGesturesEnabled != Map.UiSettings.RotateGesturesEnabled)
            {
                Map.UiSettings.RotateGesturesEnabled = Map.UiSettings.RotateGesturesEnabled;
            }
        }

        protected override void OnUpdateMyLocationButtonEnabled()
        {
            //TODO OnUpdateMyLocationButtonEnabled
            System.Diagnostics.Debug.WriteLine("TODO: OnUpdateMyLocationButtonEnabled");
        }

        protected override void OnUpdateIndoorLevelPickerEnabled()
        {
            //TODO OnUpdateIndoorLevelPickerEnabled
            System.Diagnostics.Debug.WriteLine("TODO: OnUpdateIndoorLevelPickerEnabled");
        }

        protected override void OnUpdateScrollGesturesEnabled()
        {
            NativeMap.PanInteractionMode = Map.UiSettings.ScrollGesturesEnabled ?
                MapPanInteractionMode.Auto : MapPanInteractionMode.Disabled;

            if (Map.UiSettings.ScrollGesturesEnabled != Map.UiSettings.ScrollGesturesEnabled)
            {
                Map.UiSettings.ScrollGesturesEnabled = Map.UiSettings.ScrollGesturesEnabled;
            }
        }

        protected override void OnUpdateTiltGesturesEnabled()
        {
            NativeMap.TiltInteractionMode = Map.UiSettings.TiltGesturesEnabled ?
                MapInteractionMode.GestureOnly : MapInteractionMode.Disabled;
        }

        protected override void OnUpdateZoomControlsEnabled()
        {
            UpdateZoomControlAndGesturesEnabled();
        }

        protected override void OnUpdateZoomGesturesEnabled()
        {
            UpdateZoomControlAndGesturesEnabled();
        }

        protected override void OnUpdateMapToolbarEnabled()
        {
            //TODO OnUpdateMapToolbarEnabled
            System.Diagnostics.Debug.WriteLine("TODO: OnUpdateMapToolbarEnabled");
        }

        private void UpdateZoomControlAndGesturesEnabled()
        {
            if (Map.UiSettings.ZoomControlsEnabled && Map.UiSettings.ZoomGesturesEnabled)
            {
                NativeMap.ZoomInteractionMode = MapInteractionMode.GestureAndControl;
            }
            else if (Map.UiSettings.ZoomControlsEnabled && !Map.UiSettings.ZoomGesturesEnabled)
            {
                NativeMap.ZoomInteractionMode = MapInteractionMode.ControlOnly;
            }
            else if (!Map.UiSettings.ZoomControlsEnabled && Map.UiSettings.ZoomGesturesEnabled)
            {
                NativeMap.ZoomInteractionMode = MapInteractionMode.GestureOnly;
            }
            else
            {
                NativeMap.ZoomInteractionMode = MapInteractionMode.Disabled;
            }
        }
    }
}
