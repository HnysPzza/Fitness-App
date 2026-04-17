using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;

namespace Fitness_App.Pages
{
    public partial class MapsPage : ContentPage
    {
        private const string SelectedStylePreferenceKey = "maps_selected_style";
        private const string ThreeDPreferenceKey = "maps_three_d_enabled";
        private const uint MapStyleSheetAnimationMs = 280;
        private const uint MapStyleOverlayAnimationMs = 200;
        private const double MapStyleSheetHiddenTranslationY = 400;
        private const int LiveLocationRefreshIntervalMs = 1800;
        private const int HeadingUiThrottleMs = 250;
        private bool _isLoaded;
        private bool _mapReady;
        private double _startLng = 123.885;
        private double _startLat = 10.315;
        private double _currentLng = 123.885;
        private double _currentLat = 10.315;
        private double? _currentHeadingDegrees;
        private DateTimeOffset _lastHeadingUiUpdateUtc;
        private string _selectedStyle = "outdoors-v12";
        private bool _isThreeDEnabled;
        private bool _isLocationLocked;
        private bool _isRefreshingLocation;
        private bool _isPageVisible;
        private bool _isMapStyleSheetOpen;
        private bool _isMapStyleSheetAnimating;
        private CancellationTokenSource? _liveLocationCts;
        private ShowSuggestedRouteOnMapMessage? _activeSuggestedRoute;

        public MapsPage()
        {
            InitializeComponent();
            MapWebView.Navigating += OnMapWebViewNavigating;
            _selectedStyle = Preferences.Default.Get(SelectedStylePreferenceKey, _selectedStyle);
            _isThreeDEnabled = Preferences.Default.Get(ThreeDPreferenceKey, _isThreeDEnabled);
            StyleSelectorLayer.IsVisible = false;
            StyleSelectorLayer.InputTransparent = true;
            StyleSelectorSheet.TranslationY = MapStyleSheetHiddenTranslationY;
            StyleSelectorSheet.InputTransparent = true;
            StyleSelectorBackdrop.Opacity = 0;
            StyleSelectorBackdrop.InputTransparent = true;
            UpdateStyleCheckmarks();
            UpdateThreeDToggle();
            UpdateLocationLockToggle();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _isPageVisible = true;
            StartHeadingMonitoring();
            StartLiveLocationLoop();

            if (_isLoaded)
            {
                await CapturePendingSuggestedRouteAsync();
                await ApplyPendingNavigationStateAsync();
                await RefreshCurrentLocationAsync(centerMap: _activeSuggestedRoute == null, lockAfterCenter: _activeSuggestedRoute == null);
                return;
            }

            _isLoaded = true;
            await LoadMapAsync();
        }

        protected override void OnDisappearing()
        {
            _isPageVisible = false;
            StopHeadingMonitoring();
            StopLiveLocationLoop();
            base.OnDisappearing();
        }

        private async Task LoadMapAsync()
        {
            MapLoadingOverlay.IsVisible = true;

            _startLat = Preferences.Default.Get("last_lat", 10.315);
            _startLng = Preferences.Default.Get("last_lng", 123.885);
            _currentLat = _startLat;
            _currentLng = _startLng;

            try
            {
                var html = await MapboxWebViewHtml.BuildMapHtmlAsync("maps.html", _selectedStyle, _startLng, _startLat);
                MapWebView.Source = new HtmlWebViewSource
                {
                    Html = html,
                    BaseUrl = "https://api.mapbox.com/"
                };

                // 4-second fallback — hide overlay even if JS map-ready never fires
                _ = Task.Delay(4000).ContinueWith(_ =>
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (MapLoadingOverlay.IsVisible)
                        {
                            MapLoadingOverlay.IsVisible = false;
                            _mapReady = true;
                        }
                    }));
            }
            catch
            {
                MapLoadingOverlay.IsVisible = false;
            }
        }

        // ── WebView events ────────────────────────────────────────────────────
        private async void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            var url = e.Url ?? string.Empty;
            if (!url.Contains("fitnessapp.local/map-ready", StringComparison.OrdinalIgnoreCase))
                return;

            e.Cancel = true;
            _mapReady = true;
            MapLoadingOverlay.IsVisible = false;
            await CapturePendingSuggestedRouteAsync();
            await ApplyCurrentMapPresentationAsync();
            await ApplyPendingNavigationStateAsync();
            await RefreshCurrentLocationAsync(centerMap: _activeSuggestedRoute == null, lockAfterCenter: _activeSuggestedRoute == null);
        }

        private static double NormalizeHeading(double heading)
        {
            var normalized = heading % 360d;
            return normalized < 0 ? normalized + 360d : normalized;
        }

        private void StartHeadingMonitoring()
        {
            try
            {
                Compass.Default.ReadingChanged -= OnCompassReadingChanged;
                Compass.Default.ReadingChanged += OnCompassReadingChanged;
                if (!Compass.Default.IsMonitoring)
                    Compass.Default.Start(SensorSpeed.UI);
            }
            catch
            {
            }
        }

        private void StopHeadingMonitoring()
        {
            try
            {
                Compass.Default.ReadingChanged -= OnCompassReadingChanged;
                if (Compass.Default.IsMonitoring)
                    Compass.Default.Stop();
            }
            catch
            {
            }
        }

        private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            _currentHeadingDegrees = NormalizeHeading(e.Reading.HeadingMagneticNorth);
            if (!_isPageVisible || !_mapReady)
                return;

            var now = DateTimeOffset.UtcNow;
            if ((now - _lastHeadingUiUpdateUtc).TotalMilliseconds < HeadingUiThrottleMs)
                return;

            _lastHeadingUiUpdateUtc = now;
            _ = SyncUserLocationAsync(_isLocationLocked);
        }

        private void UpdateHeadingFromLocation(Location location)
        {
            if (location.Course.HasValue && !double.IsNaN(location.Course.Value) && location.Course.Value >= 0)
                _currentHeadingDegrees = NormalizeHeading(location.Course.Value);
        }

        private void StartLiveLocationLoop()
        {
            StopLiveLocationLoop();
            _liveLocationCts = new CancellationTokenSource();
            _ = RunLiveLocationLoopAsync(_liveLocationCts.Token);
        }

        private void StopLiveLocationLoop()
        {
            _liveLocationCts?.Cancel();
            _liveLocationCts?.Dispose();
            _liveLocationCts = null;
        }

        private async Task RunLiveLocationLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshCurrentLocationAsync(centerMap: _isLocationLocked, lockAfterCenter: _isLocationLocked);
                }
                catch
                {
                }

                try
                {
                    await Task.Delay(LiveLocationRefreshIntervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // ── Suggested route ───────────────────────────────────────────────────
        private Task CapturePendingSuggestedRouteAsync()
        {
            if (MapNavigationState.PendingSuggestedRoute != null)
                _activeSuggestedRoute = MapNavigationState.PendingSuggestedRoute;
            return Task.CompletedTask;
        }

        private async Task ApplyPendingNavigationStateAsync()
        {
            if (!_mapReady) return;

            var routeToShow = _activeSuggestedRoute;
            if (routeToShow == null)
            {
                _isLocationLocked = true;
                UpdateLocationLockToggle();
                RouteBanner.IsVisible = false;
                try
                {
                    string lngText = _startLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string latText = _startLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    await RunMapScriptAsync("removeSavedRoute('app-suggested-preview');");
                    await RunMapScriptAsync($"centerOnUser({lngText}, {latText});");
                }
                catch { }
                return;
            }

            _isLocationLocked = false;
            UpdateLocationLockToggle();
            RouteTitleLabel.Text = string.IsNullOrWhiteSpace(routeToShow.Title) ? "Suggested route" : routeToShow.Title;
            RouteBanner.IsVisible = true;

            try
            {
                string coordsJson = JsonSerializer.Serialize(routeToShow.CoordinatesJson);
                string colorJson  = JsonSerializer.Serialize(routeToShow.ColorHex);
                string destLng    = routeToShow.DestLng?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
                string destLat    = routeToShow.DestLat?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
                await RunMapScriptAsync($"setSuggestedRoutePreview({coordsJson}, {colorJson}, {destLng}, {destLat});");
            }
            catch { }
            finally
            {
                MapNavigationState.PendingSuggestedRoute = null;
            }
        }

        private async Task ApplyCurrentMapPresentationAsync()
        {
            if (!_mapReady) return;
            string styleJson = JsonSerializer.Serialize(_selectedStyle);
            await RunMapScriptAsync($"changeMapStyle({styleJson});");
            await RunMapScriptAsync($"toggle3D({(_isThreeDEnabled ? "true" : "false")});");
        }

        private async Task RunMapScriptAsync(string script)
        {
            if (!_mapReady) return;
            await MainThread.InvokeOnMainThreadAsync(() => MapWebView.EvaluateJavaScriptAsync(script));
        }

        private static string Inv(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private async Task SyncUserLocationAsync(bool followCamera)
        {
            string headingText = _currentHeadingDegrees.HasValue ? Inv(_currentHeadingDegrees.Value) : "null";
            await RunMapScriptAsync($"syncUserLocation({Inv(_currentLng)}, {Inv(_currentLat)}, {headingText}, {(followCamera ? "true" : "false")});");
        }

        // ── Style sheet open/close ────────────────────────────────────────────
        private async void OnMapStyleIconTapped(object? sender, EventArgs e)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            if (_isMapStyleSheetAnimating)
                return;

            if (_isMapStyleSheetOpen)
            {
                await HideMapStyleSheetAsync();
                return;
            }

            UpdateStyleCheckmarks();
            await ShowMapStyleSheetAsync();
        }

        private async void OnBackdropTapped(object? sender, EventArgs e)
        {
            if (_isMapStyleSheetAnimating)
                return;

            await HideMapStyleSheetAsync();
        }

        private async Task ShowMapStyleSheetAsync()
        {
            if (_isMapStyleSheetOpen || _isMapStyleSheetAnimating)
                return;

            _isMapStyleSheetAnimating = true;

            try
            {
                StyleSelectorLayer.IsVisible = true;
                StyleSelectorLayer.InputTransparent = false;
                StyleSelectorSheet.InputTransparent = false;
                StyleSelectorBackdrop.InputTransparent = false;
                StyleSelectorSheet.TranslationY = MapStyleSheetHiddenTranslationY;
                StyleSelectorBackdrop.Opacity = 0;

                var slideUp = StyleSelectorSheet.TranslateTo(0, 0, MapStyleSheetAnimationMs, Easing.CubicOut);
                var fadeIn = StyleSelectorBackdrop.FadeTo(1, MapStyleOverlayAnimationMs, Easing.CubicOut);
                await Task.WhenAll(slideUp, fadeIn);

                _isMapStyleSheetOpen = true;
            }
            finally
            {
                _isMapStyleSheetAnimating = false;
            }
        }

        private async Task HideMapStyleSheetAsync()
        {
            if ((!_isMapStyleSheetOpen && !_isMapStyleSheetAnimating) || _isMapStyleSheetAnimating && !_isMapStyleSheetOpen)
                return;

            _isMapStyleSheetAnimating = true;

            try
            {
                _isMapStyleSheetOpen = false;

                var slideDown = StyleSelectorSheet.TranslateTo(0, MapStyleSheetHiddenTranslationY, 250, Easing.CubicIn);
                var fadeOut = StyleSelectorBackdrop.FadeTo(0, MapStyleOverlayAnimationMs, Easing.CubicIn);
                await Task.WhenAll(slideDown, fadeOut);

                StyleSelectorBackdrop.InputTransparent = true;
                StyleSelectorSheet.InputTransparent = true;
                StyleSelectorLayer.InputTransparent = true;
                StyleSelectorLayer.IsVisible = false;
            }
            finally
            {
                _isMapStyleSheetAnimating = false;
            }
        }

        // ── Style selection — one handler per style (no CommandParameter) ─────
        private async void OnOutdoorsStyleTapped(object? sender, EventArgs e)   => await ApplyStyleAsync("outdoors-v12");
        private async void OnStreetsStyleTapped(object? sender, EventArgs e)     => await ApplyStyleAsync("streets-v12");
        private async void OnSatelliteStyleTapped(object? sender, EventArgs e)  => await ApplyStyleAsync("satellite-streets-v12");
        private async void OnDarkStyleTapped(object? sender, EventArgs e)        => await ApplyStyleAsync("dark-v11");
        private async void OnLightStyleTapped(object? sender, EventArgs e)       => await ApplyStyleAsync("light-v11");

        private async Task ApplyStyleAsync(string style)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            _selectedStyle = style;
            Preferences.Default.Set(SelectedStylePreferenceKey, _selectedStyle);
            UpdateStyleCheckmarks();

            if (_mapReady)
            {
                string styleJson = JsonSerializer.Serialize(style);
                await RunMapScriptAsync($"changeMapStyle({styleJson});");
            }

            await Task.Delay(200);
            await HideMapStyleSheetAsync();
        }

        // ── 3D toggle ─────────────────────────────────────────────────────────
        private async void OnThreeDClicked(object? sender, EventArgs e)
        {
            _isThreeDEnabled = !_isThreeDEnabled;
            Preferences.Default.Set(ThreeDPreferenceKey, _isThreeDEnabled);
            UpdateThreeDToggle();
            await RunMapScriptAsync($"toggle3D({(_isThreeDEnabled ? "true" : "false")});");
        }

        private async void OnLocationLockTapped(object? sender, EventArgs e)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            if (_isLocationLocked)
            {
                _isLocationLocked = false;
                UpdateLocationLockToggle();
                if (_mapReady)
                    await SyncUserLocationAsync(false);
                return;
            }

            await RefreshCurrentLocationAsync(centerMap: true, lockAfterCenter: true);
        }

        private async Task RefreshCurrentLocationAsync(bool centerMap, bool lockAfterCenter)
        {
            if (_isRefreshingLocation)
                return;

            _isRefreshingLocation = true;

            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    _isLocationLocked = false;
                    UpdateLocationLockToggle();
                    return;
                }

                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(12)
                });

                if (location == null)
                {
                    _isLocationLocked = false;
                    UpdateLocationLockToggle();
                    return;
                }

                _currentLat = location.Latitude;
                _currentLng = location.Longitude;
                _startLat = _currentLat;
                _startLng = _currentLng;
                Preferences.Default.Set("last_lat", _currentLat);
                Preferences.Default.Set("last_lng", _currentLng);
                UpdateHeadingFromLocation(location);

                if (_mapReady)
                {
                    await SyncUserLocationAsync(centerMap);
                }

                _isLocationLocked = lockAfterCenter;
                UpdateLocationLockToggle();
            }
            catch
            {
                _isLocationLocked = false;
                UpdateLocationLockToggle();
            }
            finally
            {
                _isRefreshingLocation = false;
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        private void UpdateStyleCheckmarks()
        {
            SetStyleCardState(OutdoorsCard,  OutdoorsCheck, OutdoorsBadge,  _selectedStyle == "outdoors-v12");
            SetStyleCardState(StreetsCard,   StreetsCheck, StreetsBadge,   _selectedStyle == "streets-v12");
            SetStyleCardState(SatelliteCard, SatelliteCheck, SatelliteBadge, _selectedStyle == "satellite-streets-v12");
            SetStyleCardState(DarkCard,      DarkCheck, DarkBadge,      _selectedStyle == "dark-v11");
            SetStyleCardState(LightCard,     LightCheck, LightBadge,     _selectedStyle == "light-v11");
        }

        private static void SetStyleCardState(Border card, Label label, Border badge, bool isActive)
        {
            card.Stroke          = isActive ? Color.FromArgb("#FC5200") : Colors.Transparent;
            card.StrokeThickness = 2;
            label.Text           = isActive ? "Selected" : "Tap to apply";
            label.TextColor      = isActive ? Color.FromArgb("#FC5200") : Colors.White;
            label.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
            badge.IsVisible      = isActive;
        }

        private void UpdateThreeDToggle()
        {
            var activeColor   = Color.FromArgb("#FC5200");
            var inactiveColor = Colors.White;

            if (FloatingThreeDPath != null)
            {
                FloatingThreeDPath.Stroke = _isThreeDEnabled ? activeColor : inactiveColor;
                FloatingThreeDPath.Opacity = _isThreeDEnabled ? 1.0 : 0.8;
            }
            if (FloatingThreeDLabel != null)
            {
                FloatingThreeDLabel.TextColor = _isThreeDEnabled ? activeColor : inactiveColor;
                FloatingThreeDLabel.Opacity = _isThreeDEnabled ? 1.0 : 0.6;
            }
        }

        private void UpdateLocationLockToggle()
        {
            var activeColor = Color.FromArgb("#FC5200");
            var inactiveColor = Colors.White;

            if (FloatingLocationPath != null)
            {
                FloatingLocationPath.Stroke = _isLocationLocked ? activeColor : inactiveColor;
                FloatingLocationPath.Opacity = _isLocationLocked ? 1.0 : 0.8;
            }
            if (FloatingLocationLabel != null)
            {
                FloatingLocationLabel.Text = _isLocationLocked ? "LIVE" : "FREE";
                FloatingLocationLabel.TextColor = _isLocationLocked ? activeColor : inactiveColor;
                FloatingLocationLabel.Opacity = _isLocationLocked ? 1.0 : 0.6;
            }
        }
    }
}
