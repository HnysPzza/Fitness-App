using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Fitness_App.Models;
using Fitness_App.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Extensions.DependencyInjection;

namespace Fitness_App.Pages;

public class SportOption
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class SportCategoryGroup : ObservableCollection<SportOption>
{
    public string Category { get; set; } = string.Empty;
    public SportCategoryGroup(string category, IEnumerable<SportOption> sports) : base(sports)
        => Category = category;
}

public sealed class RecordedTrackPoint
{
    public double Lng { get; init; }
    public double Lat { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public double? AccuracyMeters { get; init; }
    public double? AltitudeMeters { get; init; }
    public double? SpeedKmh { get; init; }

    public ActivityRoutePoint ToActivityRoutePoint() => new()
    {
        Lng = Lng,
        Lat = Lat
    };
}

[QueryProperty(nameof(PlannedSport), "plannedSport")]
[QueryProperty(nameof(PlannedWorkoutId), "plannedWorkoutId")]
public partial class RecordPage : ContentPage
{
    private const string SelectedMapStylePreferenceKey = "record_selected_style";
    private const string ThreeDPreferenceKey = "record_three_d_enabled";
    private const uint MapStyleSheetAnimationMs = 280;
    private const uint MapStyleOverlayAnimationMs = 200;
    private const double MapStyleSheetHiddenTranslationY = 400;
    private const int TrackingIntervalMs = 1000;
    private const double MinimumSegmentDistanceKm = 0.0015;   // 1.5 m — was 1.0 m
    private const double MinimumMovementForGpsSaveKm = 0.02;
    private const double MaximumAcceptedAccuracyMeters = 40;  // was 65 — tighter filter
    private const int HeadingUiThrottleMs = 250;
    private const double MaxBearingDeviationDeg = 55;         // lateral drift filter
    private const double StillSpeedThresholdKmh = 0.5;        // stillness guard

    // ── Map ───────────────────────────────────────────────────────────────────
    private StatsService? _statsService;
    private IActivitySaveNotifier? _activitySaveNotifier;
    private IAppNotificationService? _notificationService;
    private bool _mapReady;
    private double _currentLat = 10.315;
    private double _currentLng = 123.885;
    private bool _isPageVisible;
    private double? _currentHeadingDegrees;
    private DateTimeOffset _lastHeadingUiUpdateUtc;
    private string _selectedMapStyle = "outdoors-v12";
    private bool _isThreeDEnabled;
    private bool _isMapStyleSheetOpen;
    private bool _isMapStyleSheetAnimating;
    private bool _isSavingActivity;

    // ── Sport ─────────────────────────────────────────────────────────────────
    private SportOption? _selectedSport;
    private bool _isSheetExpanded;
    private bool _isGpsLocked;
    private List<SportOption> _allSports = new();
    private string _plannedSport = string.Empty;

    public string PlannedSport
    {
        get => _plannedSport;
        set
        {
            _plannedSport = value ?? string.Empty;
            ApplyPlannedSportSelection();
        }
    }

    public string PlannedWorkoutId { get; set; } = string.Empty;

    public ObservableCollection<SportCategoryGroup> FilteredSportGroups { get; } = new();

    // ── Inline recording ──────────────────────────────────────────────────────
    private bool _isRecording;
    private bool _isPaused;
    private bool _isShowingFinish;
    private readonly Stopwatch _stopwatch = new();
    private System.Timers.Timer? _updateTimer;
    private CancellationTokenSource? _trackingLoopCts;
    private Task? _trackingTask;
    private double _recordedDistance;
    private double _previousLat;
    private double _previousLng;
    private readonly List<double[]> _trackedPath = new();
    private readonly List<RecordedTrackPoint> _acceptedTrackPoints = new();
    private readonly Queue<double> _recentSpeedSamplesKmh = new();
    private double _currentSpeedKmh;
    private double _smoothedSpeedKmh;
    private double _averageSpeedKmh;
    private double _maxSpeedKmh;
    private double _elevationGainM;
    private double? _lastAcceptedAltitudeM;

    public RecordPage()
    {
        BindingContext = this;
        InitializeComponent();           // ← XAML fields must exist first
        BuildSportList();                // ← Now it's safe to access named elements
        MapWebView.Navigating += OnMapWebViewNavigating;
        _selectedMapStyle = Preferences.Default.Get(SelectedMapStylePreferenceKey, _selectedMapStyle);
        _isThreeDEnabled = Preferences.Default.Get(ThreeDPreferenceKey, _isThreeDEnabled);
        MapStyleLayer.IsVisible = false;
        MapStyleLayer.InputTransparent = true;
        MapStyleSheet.TranslationY = MapStyleSheetHiddenTranslationY;
        MapStyleSheet.InputTransparent = true;
        MapStyleBackdrop.Opacity = 0;
        MapStyleBackdrop.InputTransparent = true;
        UpdateRecStyleCards();
        UpdateThreeDToggle();
        UpdateLiveMetricPresentation();
        ResolveServices();
    }

    // ── Sport list ────────────────────────────────────────────────────────────
    private void BuildSportList()
    {
        _allSports = new List<SportOption>
        {
            new() { Name="Walk",           Icon="🚶", Category="OUTDOOR" },
            new() { Name="Run",            Icon="🏃", Category="OUTDOOR" },
            new() { Name="Hike",           Icon="🧗", Category="OUTDOOR" },
            new() { Name="Trail Run",      Icon="🏃", Category="OUTDOOR" },
            new() { Name="Cycling",        Icon="🚴", Category="OUTDOOR" },
            new() { Name="Mountain Bike",  Icon="🚵", Category="OUTDOOR" },
            new() { Name="Gravel Ride",    Icon="🚴", Category="OUTDOOR" },
            new() { Name="Open Water Swim",Icon="🏊", Category="OUTDOOR" },
            new() { Name="Kayaking",       Icon="🛶", Category="OUTDOOR" },
            new() { Name="Rowing",         Icon="🚣", Category="OUTDOOR" },
            new() { Name="Surfing",        Icon="🏄", Category="OUTDOOR" },
            new() { Name="Gym Workout",    Icon="🏋", Category="INDOOR" },
            new() { Name="Yoga",           Icon="🧘", Category="INDOOR" },
            new() { Name="Pilates",        Icon="🧘", Category="INDOOR" },
            new() { Name="Boxing",         Icon="🥊", Category="INDOOR" },
            new() { Name="Pool Swim",      Icon="🏊", Category="INDOOR" },
            new() { Name="Treadmill Run",  Icon="🏃", Category="INDOOR" },
            new() { Name="Indoor Cycle",   Icon="🚴", Category="INDOOR" },
            new() { Name="Badminton",      Icon="🏸", Category="INDOOR" },
            new() { Name="Tennis",         Icon="🎾", Category="INDOOR" },
            new() { Name="Basketball",     Icon="🏀", Category="INDOOR" },
            new() { Name="Road Ride",      Icon="🚴", Category="CYCLING" },
            new() { Name="Virtual Ride",   Icon="🏎", Category="CYCLING" },
            new() { Name="E-Bike Ride",    Icon="⚡", Category="CYCLING" },
            new() { Name="Alpine Ski",     Icon="⛷", Category="WINTER" },
            new() { Name="Snowboarding",   Icon="🏂", Category="WINTER" },
            new() { Name="Ice Skating",    Icon="⛸", Category="WINTER" },
            new() { Name="Crossfit",       Icon="🏋", Category="OTHER" },
            new() { Name="Rock Climbing",  Icon="🧗", Category="OTHER" },
            new() { Name="Golf",           Icon="⛳", Category="OTHER" },
            new() { Name="Skateboarding",  Icon="🛹", Category="OTHER" },
            new() { Name="Dance",          Icon="💃", Category="OTHER" },
        };

        foreach (var sport in _allSports)
            sport.Icon = GetSportIcon(sport.Name);

        var saved = Preferences.Default.Get("last_selected_sport", "Walk");
        var toSelect = _allSports.FirstOrDefault(s => s.Name == saved)
                    ?? _allSports.First();
        SelectSportInternal(toSelect);
        RebuildFilteredList();
    }

    private static string GetSportIcon(string sportName) => sportName switch
    {
        "Walk" => UI.Icons.MaterialSymbols.Directions_walk,
        "Run" or "Trail Run" or "Treadmill Run" => UI.Icons.MaterialSymbols.Directions_run,
        "Hike" or "Rock Climbing" => UI.Icons.MaterialSymbols.Hiking,
        "Cycling" or "Mountain Bike" or "Gravel Ride" or "Road Ride" or "Virtual Ride" or "Indoor Cycle" => UI.Icons.MaterialSymbols.Pedal_bike,
        "Open Water Swim" or "Pool Swim" or "Surfing" => UI.Icons.MaterialSymbols.Pool,
        "E-Bike Ride" => UI.Icons.MaterialSymbols.Bolt,
        "Alpine Ski" or "Snowboarding" => UI.Icons.MaterialSymbols.Landscape,
        "Kayaking" or "Rowing" => UI.Icons.MaterialSymbols.Route,
        "Gym Workout" or "Yoga" or "Pilates" or "Crossfit" or "Dance" => UI.Icons.MaterialSymbols.Favorite,
        _ => UI.Icons.MaterialSymbols.Sports_score
    };

    private void RebuildFilteredList(string query = "")
    {
        FilteredSportGroups.Clear();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allSports
            : _allSports.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var grp in filtered.GroupBy(s => s.Category))
            FilteredSportGroups.Add(new SportCategoryGroup(grp.Key, grp));
    }

    private void SelectSportInternal(SportOption sport)
    {
        foreach (var s in _allSports) s.IsSelected = false;
        sport.IsSelected = true;
        _selectedSport = sport;
        if (SelectedSportNameLabel != null) SelectedSportNameLabel.Text = sport.Name;
        if (SelectedSportIconLabel != null) SelectedSportIconLabel.Text = sport.Icon;
        Preferences.Default.Set("last_selected_sport", sport.Name);
        UpdateLiveMetricPresentation();
        SetGpsState(_isGpsLocked);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        ResolveServices();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageVisible = true;
        try
        {
            ResolveServices();
            ApplyPlannedSportSelection();
            StartHeadingMonitoring();
            ApplyBottomSafeArea();
            await LoadMapAsync();
            await Task.Delay(150);
            await StartGpsSafeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RecordPage.OnAppearing] {ex.Message}\n{ex.StackTrace}");
        }
    }

    protected override void OnDisappearing()
    {
        _isPageVisible = false;
        StopHeadingMonitoring();
        base.OnDisappearing();
    }

    private void ResolveServices()
    {
        var services = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
        if (services == null)
            return;

        _statsService ??= services.GetService<StatsService>();
        _activitySaveNotifier ??= services.GetService<IActivitySaveNotifier>();
        _notificationService ??= services.GetService<IAppNotificationService>();
    }

    private void ApplyPlannedSportSelection()
    {
        if (string.IsNullOrWhiteSpace(_plannedSport) || _allSports.Count == 0)
            return;

        var sport = _allSports.FirstOrDefault(item => string.Equals(item.Name, _plannedSport, StringComparison.OrdinalIgnoreCase));
        if (sport == null)
            return;

        SelectSportInternal(sport);
    }

    private bool IsCurrentSportPaceDriven() => IsPaceDrivenSport(_selectedSport?.Name);

    private static bool IsPaceDrivenSport(string? sport)
    {
        sport ??= string.Empty;
        return sport == "Run"
            || sport == "Trail Run"
            || ActivityPresentation.IsPaceSport(sport);
    }

    private bool IsCurrentSportGpsDependent()
    {
        var sport = _selectedSport?.Name ?? string.Empty;
        if (_selectedSport?.Category == "INDOOR")
            return false;

        return sport switch
        {
            "Crossfit" or "Dance" or "Gym Workout" or "Yoga" or "Pilates" or "Boxing" or "Pool Swim" or "Treadmill Run" or "Indoor Cycle" or "Badminton" or "Tennis" or "Basketball" => false,
            _ => true
        };
    }

    private bool IsCurrentSportManualSession() => !IsCurrentSportGpsDependent();

    private bool CanStartRecording() => !IsCurrentSportGpsDependent() || _isGpsLocked;

    private void UpdateLiveMetricPresentation()
    {
        if (LiveMetricTitleLabel == null || LivePaceLabel == null)
            return;

        if (IsCurrentSportManualSession())
        {
            LiveMetricTitleLabel.Text = "Session";
            LivePaceLabel.Text = "Manual";
            return;
        }

        LiveMetricTitleLabel.Text = IsCurrentSportPaceDriven() ? "Pace (/km)" : "Speed (km/h)";
        LivePaceLabel.Text = GetLiveMetricValueText();
    }

    private string GetLiveMetricValueText()
    {
        if (IsCurrentSportManualSession())
            return "Manual";

        if (IsCurrentSportPaceDriven())
            return FormatLivePace(_smoothedSpeedKmh);

        return $"{Math.Max(0, _smoothedSpeedKmh):F1}";
    }

    private static string FormatLivePace(double speedKmh)
    {
        if (speedKmh <= 0.05)
            return "--:--";

        var totalMinutes = 60d / speedKmh;
        var minutes = (int)Math.Floor(totalMinutes);
        var seconds = (int)Math.Round((totalMinutes - minutes) * 60d);
        if (seconds == 60)
        {
            minutes += 1;
            seconds = 0;
        }

        return $"{minutes}:{seconds:00}";
    }

    // ── Safe-area spacer ──────────────────────────────────────────────────────
    private void ApplyBottomSafeArea()
    {
        double safeAreaBottom = 16;
#if IOS
        try
        {
            var win = UIKit.UIApplication.SharedApplication.Windows.FirstOrDefault(w => w.IsKeyWindow);
            if (win != null) safeAreaBottom = win.SafeAreaInsets.Bottom;
        }
        catch { safeAreaBottom = 34; }
#elif ANDROID
        try
        {
            var density = DeviceDisplay.MainDisplayInfo.Density;
            var ctx = Android.App.Application.Context;
            var resId = ctx.Resources?.GetIdentifier("navigation_bar_height", "dimen", "android") ?? 0;
            if (resId > 0 && density > 0)
                safeAreaBottom = ctx.Resources!.GetDimensionPixelSize(resId) / density;
        }
        catch { safeAreaBottom = 16; }
#endif
    }

    // ── Map ───────────────────────────────────────────────────────────────────
    private async Task LoadMapAsync()
    {
        try
        {
            _currentLat = Preferences.Default.Get("last_lat", _currentLat);
            _currentLng = Preferences.Default.Get("last_lng", _currentLng);

            var html = await MapboxWebViewHtml
                .BuildMapHtmlAsync("mapbox_map.html", _selectedMapStyle, _currentLng, _currentLat)
                .ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MapWebView.Source = new HtmlWebViewSource { Html = html, BaseUrl = "https://api.mapbox.com/" };
            });
        }
        catch (Exception ex) { Debug.WriteLine($"[RecordPage.LoadMapAsync] {ex.Message}"); }
    }

    private async void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!(e.Url ?? "").Contains("fitnessapp.local/map-ready", StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;
        _mapReady = true;
        await SyncUserLocationAsync(followCamera: true);
        await RunMapScriptAsync($"toggle3D({(_isThreeDEnabled ? "true" : "false")});");
    }

    private async Task RunMapScriptAsync(string js)
    {
        if (!_mapReady) return;
        try { await MainThread.InvokeOnMainThreadAsync(() => MapWebView.EvaluateJavaScriptAsync(js)); }
        catch (Exception ex) { Debug.WriteLine($"[RecordPage.RunMapScript] {ex.Message}"); }
    }

    private static string Inv(double d) => d.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
        if (!_isPageVisible || !_mapReady || !_isGpsLocked)
            return;

        // Do NOT update the user dot from raw compass during an active GPS recording.
        // The dot position is driven exclusively by confirmed track points to prevent drift.
        if (_isRecording && !_isPaused && IsCurrentSportGpsDependent())
            return;

        var now = DateTimeOffset.UtcNow;
        if ((now - _lastHeadingUiUpdateUtc).TotalMilliseconds < HeadingUiThrottleMs)
            return;

        _lastHeadingUiUpdateUtc = now;
        _ = SyncUserLocationAsync(followCamera: false);
    }

    private void UpdateHeadingFromLocation(Location location)
    {
        if (location.Course.HasValue && !double.IsNaN(location.Course.Value) && location.Course.Value >= 0)
            _currentHeadingDegrees = NormalizeHeading(location.Course.Value);
    }

    private async Task SyncUserLocationAsync(bool followCamera)
    {
        string headingText = _currentHeadingDegrees.HasValue ? Inv(_currentHeadingDegrees.Value) : "null";
        await RunMapScriptAsync($"syncUserLocation({Inv(_currentLng)}, {Inv(_currentLat)}, {headingText}, {(followCamera ? "true" : "false")});");
    }

    // ── GPS ───────────────────────────────────────────────────────────────────
    private async Task StartGpsSafeAsync()
    {
        MainThread.BeginInvokeOnMainThread(() => SetGpsState(false));
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GpsStatusLabel.Text = "Location denied";
                    GpsStatusLabel.TextColor = Color.FromArgb("#FF5252");
                });
                return;
            }

            var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                Timeout = TimeSpan.FromSeconds(15)
            });

            if (location != null)
            {
                _currentLat = location.Latitude;
                _currentLng = location.Longitude;
                Preferences.Default.Set("last_lat", _currentLat);
                Preferences.Default.Set("last_lng", _currentLng);
                UpdateHeadingFromLocation(location);
                await SyncUserLocationAsync(followCamera: true);
            }

            MainThread.BeginInvokeOnMainThread(() => SetGpsState(location != null));
        }
        catch (PermissionException)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GpsStatusLabel.Text = "Permission denied";
                GpsStatusLabel.TextColor = Color.FromArgb("#FF5252");
            });
        }
        catch (FeatureNotEnabledException)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GpsStatusLabel.Text = "Location off";
                GpsStatusLabel.TextColor = Color.FromArgb("#FF9800");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GPS ERROR] {ex.Message}");
            await Task.Delay(2000);
            MainThread.BeginInvokeOnMainThread(() => SetGpsState(true)); // allow offline testing
        }
    }

    private void SetGpsState(bool locked)
    {
        _isGpsLocked = locked;
        var gpsRequired = IsCurrentSportGpsDependent();
        var canRecord = !gpsRequired || locked;
        var activeColor = Color.FromArgb(canRecord ? "#4CAF50" : "#FF9800");

        GpsDot1.TextColor = activeColor;

        GpsStatusLabel.Text = gpsRequired
            ? (locked ? "GPS Ready" : "Searching for GPS…")
            : "Manual session";
        GpsStatusLabel.TextColor = canRecord ? Color.FromArgb("#4CAF50") : Color.FromArgb("#94A3B8");

        if (!_isRecording)
            RecordButton.BackgroundColor = Color.FromArgb(canRecord ? "#FC4C02" : "#3A3A4E");
        RecordButton.Opacity = canRecord ? 1.0 : 0.6;
        RecordButtonGlow.Opacity = canRecord ? 1.0 : 0.0;
        RecordButtonIconLabel.TextColor = canRecord ? Colors.White : Color.FromArgb("#88FFFFFF");

        if (canRecord) { try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { } }
    }

    // ── Map Style picker ──────────────────────────────────────────────────────
    private async void OnMapStyleTapped(object sender, TappedEventArgs e)
    {
        if (_isMapStyleSheetAnimating)
            return;

        UpdateRecStyleCards();

        if (_isMapStyleSheetOpen)
        {
            await HideMapStyleSheetAsync();
            return;
        }

        await ShowMapStyleSheetAsync();
    }

    private async void OnMapStyleBackdropTapped(object sender, TappedEventArgs e)
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
            var hiddenOffset = GetMapStyleHiddenTranslationY();
            MapStyleLayer.IsVisible = true;
            MapStyleLayer.InputTransparent = false;
            MapStyleSheet.InputTransparent = false;
            MapStyleBackdrop.InputTransparent = false;
            MapStyleSheet.TranslationY = hiddenOffset;
            MapStyleBackdrop.Opacity = 0;

            var slideUp = MapStyleSheet.TranslateTo(0, 0, MapStyleSheetAnimationMs, Easing.CubicOut);
            var fadeIn = MapStyleBackdrop.FadeTo(1, MapStyleOverlayAnimationMs, Easing.CubicOut);
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
        if (!_isMapStyleSheetOpen || _isMapStyleSheetAnimating)
            return;

        _isMapStyleSheetAnimating = true;

        try
        {
            _isMapStyleSheetOpen = false;

            var slideDown = MapStyleSheet.TranslateTo(0, GetMapStyleHiddenTranslationY(), 250, Easing.CubicIn);
            var fadeOut = MapStyleBackdrop.FadeTo(0, MapStyleOverlayAnimationMs, Easing.CubicIn);
            await Task.WhenAll(slideDown, fadeOut);

            MapStyleBackdrop.InputTransparent = true;
            MapStyleSheet.InputTransparent = true;
            MapStyleLayer.InputTransparent = true;
            MapStyleLayer.IsVisible = false;
        }
        finally
        {
            _isMapStyleSheetAnimating = false;
        }
    }

    private double GetMapStyleHiddenTranslationY()
        => Math.Max(MapStyleSheetHiddenTranslationY, MapStyleSheet.Height > 0 ? MapStyleSheet.Height + 40 : 0);

    // Individual style handlers — avoids CommandParameter parsing entirely
    private async void OnRecOutdoorsTapped(object? sender, EventArgs e)   => await ApplyRecStyleAsync("outdoors-v12");
    private async void OnRecStreetsTapped(object? sender, EventArgs e)    => await ApplyRecStyleAsync("streets-v12");
    private async void OnRecSatelliteTapped(object? sender, EventArgs e)  => await ApplyRecStyleAsync("satellite-streets-v12");
    private async void OnRecDarkTapped(object? sender, EventArgs e)        => await ApplyRecStyleAsync("dark-v11");
    private async void OnRecLightTapped(object? sender, EventArgs e)       => await ApplyRecStyleAsync("light-v11");

    private async Task ApplyRecStyleAsync(string style)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        _selectedMapStyle = style;
        Preferences.Default.Set(SelectedMapStylePreferenceKey, _selectedMapStyle);
        UpdateRecStyleCards();
        string styleJson = JsonSerializer.Serialize(style);
        await RunMapScriptAsync($"changeMapStyle({styleJson});");
        await Task.Delay(200);
        await HideMapStyleSheetAsync();
    }

    private void UpdateRecStyleCards()
    {
        SetRecCardActive(RecOutdoorsCard,  RecOutdoorsCheck, RecOutdoorsBadge,  _selectedMapStyle == "outdoors-v12");
        SetRecCardActive(RecStreetsCard,   RecStreetsCheck, RecStreetsBadge,   _selectedMapStyle == "streets-v12");
        SetRecCardActive(RecSatelliteCard, RecSatelliteCheck, RecSatelliteBadge, _selectedMapStyle == "satellite-streets-v12");
        SetRecCardActive(RecDarkCard,      RecDarkCheck, RecDarkBadge,      _selectedMapStyle == "dark-v11");
        SetRecCardActive(RecLightCard,     RecLightCheck, RecLightBadge,     _selectedMapStyle == "light-v11");
    }

    private static void SetRecCardActive(Border card, Label label, Border badge, bool active)
    {
        card.Stroke = active ? Color.FromArgb("#FC5200") : Colors.Transparent;
        card.StrokeThickness = 2;
        label.TextColor      = active ? Color.FromArgb("#FC5200") : Colors.White;
        label.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        badge.IsVisible      = active;
    }

    // ── 3D toggle ─────────────────────────────────────────────────────────────
    private async void OnThreeDTapped(object sender, TappedEventArgs e)
    {
        _isThreeDEnabled = !_isThreeDEnabled;
        Preferences.Default.Set(ThreeDPreferenceKey, _isThreeDEnabled);
        UpdateThreeDToggle();
        await RunMapScriptAsync($"toggle3D({(_isThreeDEnabled ? "true" : "false")});");
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

    // ── Sport picker ──────────────────────────────────────────────────────────
    private void OnSportPillTapped(object sender, TappedEventArgs e) => OpenExpandedSheet();

    private void OnSportItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SportOption sport)
            SelectSportInternal(sport);
        if (sender is CollectionView cv) cv.SelectedItem = null;
        CollapseExpandedSheet();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        => RebuildFilteredList(e.NewTextValue);

    private void OpenExpandedSheet()
    {
        _isSheetExpanded = true;
        ExpandedSheet.IsVisible = true;
        ExpandedSheet.TranslationY = 800;
        ExpandedSheet.TranslateTo(0, 0, 300, Easing.SpringOut);
        BottomBar.FadeTo(0.4, 200);
    }

    private void CollapseExpandedSheet()
    {
        _isSheetExpanded = false;
        ExpandedSheet.TranslateTo(0, 800, 260, Easing.CubicIn)
            .ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() =>
            {
                ExpandedSheet.IsVisible = false;
                SearchEntry.Text = "";
                SearchEntry.Unfocus();
                BottomBar.FadeTo(1.0, 200);
            }));
    }

    private void OnCollapseTapped(object sender, TappedEventArgs e) => CollapseExpandedSheet();

    private double _expandedSheetStartY;
    private void OnExpandedSheetPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _expandedSheetStartY = ExpandedSheet.TranslationY; break;
            case GestureStatus.Running:
                var newY = _expandedSheetStartY + e.TotalY;
                if (newY > 0) ExpandedSheet.TranslationY = newY; break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (ExpandedSheet.TranslationY > 140) CollapseExpandedSheet();
                else ExpandedSheet.TranslateTo(0, 0, 200, Easing.SpringOut);
                break;
        }
    }

    // ── Inline recording ──────────────────────────────────────────────────────
    private async void OnRecordTapped(object sender, TappedEventArgs e)
    {
        if (_isRecording) { ShowFinishSheet(); return; }
        if (!CanStartRecording()) return;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await RecordButton.ScaleToAsync(0.88, 70);
        await RecordButton.ScaleToAsync(1.0, 70);
        StartInlineRecording();
    }

    private void StartInlineRecording()
    {
        var gpsDependent = IsCurrentSportGpsDependent();
        _isRecording = true;
        _isPaused = false;
        _isShowingFinish = false;
        ResetRecordingMetrics();
        _stopwatch.Restart();

        RecordButtonIconLabel.Text = UI.Icons.MaterialSymbols.Stop;
        RecordButton.BackgroundColor = Color.FromArgb("#EF4444");

        AddRouteBtn.IsVisible = false;
        PauseBtn.IsVisible = true;
        PauseBtnLabel.IsVisible = true;
        PauseBtnIcon.Text = UI.Icons.MaterialSymbols.Pause;
        PauseBtn.BackgroundColor = Color.FromArgb("#F59E0B");
        PauseBtnLabel.Text = "Pause";
        PauseBtnLabel.TextColor = Color.FromArgb("#F59E0B");

        _updateTimer = new System.Timers.Timer(TrackingIntervalMs);
        _updateTimer.Elapsed += OnTimerTick;
        _updateTimer.AutoReset = true;
        _updateTimer.Start();

        WorkoutSessionManager.CommandRequested -= OnWorkoutCommandRequested;
        WorkoutSessionManager.CommandRequested += OnWorkoutCommandRequested;
        WorkoutSessionManager.Start(_selectedSport?.Name ?? "Activity", gpsDependent);
        WorkoutSessionManager.UpdateMetrics(_selectedSport?.Name ?? "Activity", gpsDependent, _recordedDistance, _maxSpeedKmh);
        StartAndroidWorkoutService();

        _trackingLoopCts?.Cancel();
        _trackingLoopCts?.Dispose();
        _trackingLoopCts = null;
        _trackingTask = null;

        if (gpsDependent)
        {
            _trackingLoopCts = new CancellationTokenSource();
            _trackingTask = TrackLocationAsync(_trackingLoopCts.Token);
            _ = RunMapScriptAsync("startTracking();");
            SeedInitialTrackPoint();
        }
        else
        {
            _ = RunMapScriptAsync("resetTracking();");
            UpdateSpeedMetrics(0);
        }

        UpdateLiveStatsUI();
        SetGpsState(_isGpsLocked);
    }

    private void OnTimerTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_isPaused)
            return;

        WorkoutSessionManager.UpdateMetrics(_selectedSport?.Name ?? "Activity", IsCurrentSportGpsDependent(), _recordedDistance, _maxSpeedKmh);
        MainThread.BeginInvokeOnMainThread(UpdateLiveStatsUI);
    }

    private async Task TrackLocationAsync(CancellationToken cancellationToken)
    {
        while (_isRecording && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TrackingIntervalMs, cancellationToken);
                if (!_isRecording || _isPaused || !IsCurrentSportGpsDependent())
                    continue;

                var loc = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(TrackingIntervalMs <= 1000 ? 2 : 3)
                });
                if (loc == null)
                    continue;

                _currentLat = loc.Latitude;
                _currentLng = loc.Longitude;
                Preferences.Default.Set("last_lat", _currentLat);
                Preferences.Default.Set("last_lng", _currentLng);
                UpdateHeadingFromLocation(loc);
                await SyncUserLocationAsync(followCamera: true);

                if (TryAcceptLocation(loc, out var acceptedPoint))
                {
                    await AddAcceptedPointToMapAsync(acceptedPoint!);
                }
                else
                {
                    UpdateSpeedMetrics(0);
                }

                MainThread.BeginInvokeOnMainThread(UpdateLiveStatsUI);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrackLocation] {ex.Message}");
            }
        }
    }

    private void ResetRecordingMetrics()
    {
        _recordedDistance = 0.0;
        _previousLat = _currentLat;
        _previousLng = _currentLng;
        _trackedPath.Clear();
        _acceptedTrackPoints.Clear();
        _recentSpeedSamplesKmh.Clear();
        _currentSpeedKmh = 0;
        _smoothedSpeedKmh = 0;
        _averageSpeedKmh = 0;
        _maxSpeedKmh = 0;
        _elevationGainM = 0;
        _lastAcceptedAltitudeM = null;
    }

    private void SeedInitialTrackPoint()
    {
        if (!IsCurrentSportGpsDependent() || !_isGpsLocked)
            return;

        var seedPoint = new RecordedTrackPoint
        {
            Lng = _currentLng,
            Lat = _currentLat,
            TimestampUtc = DateTimeOffset.UtcNow,
            SpeedKmh = 0
        };

        _acceptedTrackPoints.Add(seedPoint);
        _trackedPath.Add(new[] { seedPoint.Lng, seedPoint.Lat });
        _previousLat = seedPoint.Lat;
        _previousLng = seedPoint.Lng;
        _ = AddAcceptedPointToMapAsync(seedPoint);
    }

    private bool TryAcceptLocation(Location location, out RecordedTrackPoint? acceptedPoint)
    {
        acceptedPoint = null;

        var accuracyMeters = location.Accuracy ?? MaximumAcceptedAccuracyMeters;
        if (accuracyMeters > MaximumAcceptedAccuracyMeters)
        {
            Debug.WriteLine($"[TrackLocation] rejected point due to low accuracy: {accuracyMeters:F1}m");
            return false;
        }

        var timestamp = location.Timestamp == default ? DateTimeOffset.UtcNow : location.Timestamp;
        if (_acceptedTrackPoints.Count == 0)
        {
            acceptedPoint = CreateTrackPoint(location, timestamp, 0);
            RegisterAcceptedPoint(acceptedPoint, 0);
            return true;
        }

        var previous = _acceptedTrackPoints[^1];
        var elapsedSeconds = Math.Max(1, (timestamp - previous.TimestampUtc).TotalSeconds);
        var segmentDistanceKm = HaversineKm(previous.Lat, previous.Lng, location.Latitude, location.Longitude);
        var minDistanceKm = Math.Clamp((accuracyMeters * 0.04) / 1000d, MinimumSegmentDistanceKm, 0.0045);
        var segmentSpeedKmh = segmentDistanceKm / (elapsedSeconds / 3600d);
        var maxAcceptedSpeedKmh = GetMaxAcceptedSpeedKmh();
        var reportedSpeedKmh = location.Speed.HasValue && location.Speed.Value >= 0
            ? location.Speed.Value * 3.6
            : (double?)null;
        var instantSpeedKmh = reportedSpeedKmh.HasValue && reportedSpeedKmh.Value <= maxAcceptedSpeedKmh
            ? reportedSpeedKmh.Value
            : segmentSpeedKmh;
        var segmentDistanceMeters = segmentDistanceKm * 1000d;
        var previousAccuracyMeters = previous.AccuracyMeters ?? accuracyMeters;
        var combinedAccuracyMeters = Math.Max(accuracyMeters, previousAccuracyMeters);

        // ── Stillness guard ────────────────────────────────────────────────
        // If the last N speed samples are all very low, the user is standing
        // still — GPS jitter should NOT accumulate as fake distance.
        bool userIsStill = _recentSpeedSamplesKmh.Count >= 3
            && _recentSpeedSamplesKmh.All(s => s < StillSpeedThresholdKmh)
            && (reportedSpeedKmh == null || reportedSpeedKmh.Value < StillSpeedThresholdKmh);

        if (userIsStill && segmentDistanceMeters < combinedAccuracyMeters)
        {
            Debug.WriteLine($"[TrackLocation] stillness guard: distance={segmentDistanceMeters:F1}m accuracy={combinedAccuracyMeters:F1}m");
            return false;
        }

        // ── Bearing-deviation filter ───────────────────────────────────────
        // When the new segment's bearing deviates sharply from the current
        // travel direction AND the segment is short, it's a lateral drift spike.
        if (_acceptedTrackPoints.Count >= 2 && segmentDistanceMeters < 25)
        {
            var prev2 = _acceptedTrackPoints[^2];
            var travelBearing = BearingDeg(prev2.Lat, prev2.Lng, previous.Lat, previous.Lng);
            var newBearing    = BearingDeg(previous.Lat, previous.Lng, location.Latitude, location.Longitude);
            var bearingDelta  = Math.Abs(((newBearing - travelBearing + 540) % 360) - 180);

            if (bearingDelta > MaxBearingDeviationDeg)
            {
                Debug.WriteLine($"[TrackLocation] bearing deviation rejected: delta={bearingDelta:F0}° distance={segmentDistanceMeters:F1}m");
                return false;
            }
        }

        var hasEnoughMovement = segmentDistanceKm >= minDistanceKm
            || (elapsedSeconds >= 3 && segmentDistanceKm >= MinimumSegmentDistanceKm * 0.75);

        if (!hasEnoughMovement)
        {
            Debug.WriteLine($"[TrackLocation] waiting for more movement: distance={segmentDistanceKm * 1000:F1}m threshold={minDistanceKm * 1000:F1}m elapsed={elapsedSeconds:F1}s accuracy={accuracyMeters:F1}m");
            return false;
        }

        if (combinedAccuracyMeters >= 35 && elapsedSeconds <= 4 && segmentDistanceMeters <= combinedAccuracyMeters * 0.65)
        {
            Debug.WriteLine($"[TrackLocation] rejected jitter within accuracy bubble: distance={segmentDistanceMeters:F1}m combinedAccuracy={combinedAccuracyMeters:F1}m elapsed={elapsedSeconds:F1}s");
            return false;
        }

        var driftJumpMeters = Math.Max(combinedAccuracyMeters * 2.4, 32);
        if (elapsedSeconds <= 3 && segmentDistanceMeters > driftJumpMeters && (!reportedSpeedKmh.HasValue || reportedSpeedKmh.Value < 1.5))
        {
            Debug.WriteLine($"[TrackLocation] rejected drift jump: distance={segmentDistanceMeters:F1}m limit={driftJumpMeters:F1}m elapsed={elapsedSeconds:F1}s speed={(reportedSpeedKmh ?? 0):F1} km/h");
            return false;
        }

        if (instantSpeedKmh > maxAcceptedSpeedKmh)
        {
            Debug.WriteLine($"[TrackLocation] rejected speed spike: {instantSpeedKmh:F1} km/h > {maxAcceptedSpeedKmh:F1} km/h");
            return false;
        }

        acceptedPoint = CreateTrackPoint(location, timestamp, instantSpeedKmh);
        RegisterAcceptedPoint(acceptedPoint, segmentDistanceKm);
        return true;
    }

    /// <summary>Calculates the forward azimuth (bearing) in degrees [0–360] between two coordinates.</summary>
    private static double BearingDeg(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var rLat1 = lat1 * Math.PI / 180.0;
        var rLat2 = lat2 * Math.PI / 180.0;
        var y = Math.Sin(dLon) * Math.Cos(rLat2);
        var x = Math.Cos(rLat1) * Math.Sin(rLat2) - Math.Sin(rLat1) * Math.Cos(rLat2) * Math.Cos(dLon);
        var bearing = Math.Atan2(y, x) * 180.0 / Math.PI;
        return (bearing + 360) % 360;
    }

    private RecordedTrackPoint CreateTrackPoint(Location location, DateTimeOffset timestamp, double instantSpeedKmh)
    {
        return new RecordedTrackPoint
        {
            Lng = location.Longitude,
            Lat = location.Latitude,
            TimestampUtc = timestamp,
            AccuracyMeters = location.Accuracy,
            AltitudeMeters = location.Altitude,
            SpeedKmh = instantSpeedKmh
        };
    }

    private void RegisterAcceptedPoint(RecordedTrackPoint point, double segmentDistanceKm)
    {
        if (segmentDistanceKm > 0)
            _recordedDistance += segmentDistanceKm;

        if (_lastAcceptedAltitudeM.HasValue && point.AltitudeMeters.HasValue)
        {
            var climb = point.AltitudeMeters.Value - _lastAcceptedAltitudeM.Value;
            if (climb > 0.8)
                _elevationGainM += climb;
        }

        if (point.AltitudeMeters.HasValue)
            _lastAcceptedAltitudeM = point.AltitudeMeters;

        _currentLat = point.Lat;
        _currentLng = point.Lng;
        _previousLat = point.Lat;
        _previousLng = point.Lng;
        _acceptedTrackPoints.Add(point);
        _trackedPath.Add(new[] { point.Lng, point.Lat });

        UpdateSpeedMetrics(point.SpeedKmh ?? 0);
        _averageSpeedKmh = _stopwatch.Elapsed.TotalHours > 0.0001
            ? _recordedDistance / _stopwatch.Elapsed.TotalHours
            : 0;
        WorkoutSessionManager.UpdateMetrics(_selectedSport?.Name ?? "Activity", IsCurrentSportGpsDependent(), _recordedDistance, _maxSpeedKmh);
    }

    private void UpdateSpeedMetrics(double instantSpeedKmh)
    {
        _currentSpeedKmh = Math.Max(0, instantSpeedKmh);
        _recentSpeedSamplesKmh.Enqueue(_currentSpeedKmh);
        while (_recentSpeedSamplesKmh.Count > 5)
            _recentSpeedSamplesKmh.Dequeue();

        _smoothedSpeedKmh = _recentSpeedSamplesKmh.Count > 0 ? _recentSpeedSamplesKmh.Average() : 0;
        _maxSpeedKmh = Math.Max(_maxSpeedKmh, _currentSpeedKmh);
        UpdateLiveMetricPresentation();
    }

    private double GetMaxAcceptedSpeedKmh()
    {
        var sport = _selectedSport?.Name ?? string.Empty;
        return sport switch
        {
            "Cycling" or "Mountain Bike" or "Gravel Ride" or "Road Ride" or "E-Bike Ride" or "Virtual Ride" => 90,
            "Run" or "Trail Run" => 35,
            "Walk" or "Hike" => 18,
            "Open Water Swim" => 12,
            "Kayaking" or "Rowing" => 30,
            _ => 55
        };
    }

    private async Task AddAcceptedPointToMapAsync(RecordedTrackPoint point)
    {
        await RunMapScriptAsync($"addCoordinate({Inv(point.Lng)}, {Inv(point.Lat)});");
    }

    private void UpdateLiveStatsUI()
    {
        var elapsed = _stopwatch.Elapsed;
        LiveTimeLabel.Text = elapsed.Hours > 0
            ? $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        LiveDistanceLabel.Text = _recordedDistance <= 0 ? "0.00" : $"{_recordedDistance:F2}";
        UpdateLiveMetricPresentation();
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private void StopRecordingTimers()
    {
        _stopwatch.Stop();
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _updateTimer = null;
        _trackingLoopCts?.Cancel();
        _trackingLoopCts?.Dispose();
        _trackingLoopCts = null;
        _trackingTask = null;
    }

    // ── Pause ─────────────────────────────────────────────────────────────────
    private void OnPauseTapped(object sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        TogglePauseState();
    }

    private void TogglePauseState()
    {
        _isPaused = !_isPaused;
        if (_isPaused)
        {
            _stopwatch.Stop();
            WorkoutSessionManager.Pause();
            PauseBtnIcon.Text = UI.Icons.MaterialSymbols.Play_arrow;
            PauseBtn.BackgroundColor = Color.FromArgb("#10B981");
            PauseBtnLabel.Text = "Resume";
            PauseBtnLabel.TextColor = Color.FromArgb("#10B981");
        }
        else
        {
            _stopwatch.Start();
            WorkoutSessionManager.Resume();
            PauseBtnIcon.Text = UI.Icons.MaterialSymbols.Pause;
            PauseBtn.BackgroundColor = Color.FromArgb("#F59E0B");
            PauseBtnLabel.Text = "Pause";
            PauseBtnLabel.TextColor = Color.FromArgb("#F59E0B");
        }
    }

    private void OnWorkoutCommandRequested(object? sender, WorkoutCommand command)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (!_isRecording)
                return;

            if (command == WorkoutCommand.TogglePauseResume)
            {
                TogglePauseState();
                return;
            }

            if (!_isPageVisible)
            {
                await SaveActivityAsync(showSavedSheet: false);
                return;
            }

            ShowFinishSheet();
        });
    }

    // ── Finish / Save sheets ──────────────────────────────────────────────────
    private void ShowFinishSheet()
    {
        if (_isShowingFinish) return;
        _isShowingFinish = true;
        var el = _stopwatch.Elapsed;
        FinishTimeLabel.Text = $"{el.Hours:00}:{el.Minutes:00}:{el.Seconds:00}";
        FinishDistanceLabel.Text = $"{_recordedDistance:F2}";
        if (_acceptedTrackPoints.Count > 0)
            _ = RunMapScriptAsync($"stopTracking({Inv(_currentLng)}, {Inv(_currentLat)});");
        FinishBackdrop.InputTransparent = false;
        FinishBackdrop.IsVisible = true;
        FinishConfirmSheet.TranslationY = 600;
        FinishConfirmSheet.IsVisible = true;
        FinishConfirmSheet.TranslateTo(0, 0, 280, Easing.CubicOut);
    }

    private async Task HideFinishSheet()
    {
        await FinishConfirmSheet.TranslateTo(0, 600, 240, Easing.CubicIn);
        FinishConfirmSheet.IsVisible = false;
        FinishBackdrop.IsVisible = false;
        FinishBackdrop.InputTransparent = true;
        _isShowingFinish = false;
    }

    private async void OnFinishBackdropTapped(object sender, TappedEventArgs e) => await HideFinishSheet();
    private async void OnKeepRecordingClicked(object sender, EventArgs e) => await HideFinishSheet();

    private async void OnSaveActivityClicked(object sender, EventArgs e)
        => await SaveActivityAsync(showSavedSheet: true);

    private async Task SaveActivityAsync(bool showSavedSheet)
    {
        if (_isSavingActivity)
            return;

        var gpsDependent = IsCurrentSportGpsDependent();
        if (gpsDependent && _recordedDistance < MinimumMovementForGpsSaveKm)
        {
            if (!_isPageVisible)
            {
                StopRecordingTimers();
                _isRecording = false;
                WorkoutSessionManager.Stop();
                WorkoutSessionManager.CommandRequested -= OnWorkoutCommandRequested;
                StopAndroidWorkoutService();
                ResetRecordingUI();
                return;
            }

            if (_isPageVisible)
                await DisplayAlert("Track more movement", "Outdoor GPS activities need actual tracked movement before saving.", "OK");
            return;
        }

        var activity = BuildCompletedActivity();
        UserActivity? savedActivity = null;
        _isSavingActivity = true;
        try
        {
            savedActivity = _statsService == null ? null : await _statsService.SaveActivityAsync(activity);
            if (savedActivity == null)
            {
                var message = string.IsNullOrWhiteSpace(_statsService?.LastSaveError)
                    ? "The activity could not be saved right now."
                    : _statsService!.LastSaveError!;
                if (_isPageVisible)
                    await DisplayAlert("Save failed", message, "OK");
                return;
            }
        }
        finally
        {
            _isSavingActivity = false;
        }

        CompleteRecordingAfterSave();

        _activitySaveNotifier?.NotifyActivitySaved();
        if (_notificationService != null)
            await _notificationService.ShowRecordingCompletedAsync(savedActivity);

        PlannedSport = string.Empty;
        PlannedWorkoutId = string.Empty;

        if (!showSavedSheet)
        {
            ResetRecordingUI();
            return;
        }

        SavedSportLabel.Text    = _selectedSport?.Name ?? "Activity";
        SavedTimeLabel.Text     = FinishTimeLabel.Text;
        SavedDistanceLabel.Text = gpsDependent ? $"{_recordedDistance:F2} km" : "Manual session";
        await HideFinishSheet();
        await Task.Delay(300);
        ShowSavedSheet();
    }

    private void CompleteRecordingAfterSave()
    {
        StopRecordingTimers();
        _isRecording = false;
        WorkoutSessionManager.Stop();
        WorkoutSessionManager.CommandRequested -= OnWorkoutCommandRequested;
        StopAndroidWorkoutService();
    }

    private UserActivity BuildCompletedActivity()
    {
        var duration = _stopwatch.Elapsed;
        return new UserActivity
        {
            Sport = _selectedSport?.Name ?? "Activity",
            DistanceKm = _recordedDistance,
            DurationTicks = duration.Ticks,
            CreatedAt = DateTime.UtcNow,
            AvgSpeedKmh = _averageSpeedKmh > 0 ? _averageSpeedKmh : null,
            MaxSpeedKmh = _maxSpeedKmh > 0 ? _maxSpeedKmh : null,
            ElevationGainM = _elevationGainM > 0 ? _elevationGainM : null,
            CoordinatesJson = ActivityRouteCodec.Serialize(
                _acceptedTrackPoints.Select(point => point.ToActivityRoutePoint()),
                _maxSpeedKmh > 0 ? _maxSpeedKmh : null,
                _averageSpeedKmh > 0 ? _averageSpeedKmh : null,
                _elevationGainM > 0 ? _elevationGainM : null)
        };
    }

    private void ShowSavedSheet()
    {
        SavedBackdrop.InputTransparent = false;
        SavedBackdrop.IsVisible = true;
        SavedSheet.TranslationY = 600;
        SavedSheet.IsVisible = true;
        SavedSheet.TranslateTo(0, 0, 280, Easing.CubicOut);
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        await SavedSheet.TranslateTo(0, 600, 240, Easing.CubicIn);
        SavedSheet.IsVisible = false;
        SavedBackdrop.IsVisible = false;
        SavedBackdrop.InputTransparent = true;
        ResetRecordingUI();
    }

    private void ResetRecordingUI()
    {
        ResetRecordingMetrics();
        AddRouteBtn.IsVisible = true;
        PauseBtn.IsVisible = false;
        PauseBtnLabel.IsVisible = false;
        RecordButtonIconLabel.Text = UI.Icons.MaterialSymbols.Play_arrow;
        LiveTimeLabel.Text = "00:00";
        LiveDistanceLabel.Text = "0.00";
        UpdateLiveMetricPresentation();
        RecordButton.BackgroundColor = CanStartRecording() ? Color.FromArgb("#FC4C02") : Color.FromArgb("#3A3A4E");
        SetGpsState(_isGpsLocked);
        _ = RunMapScriptAsync("resetTracking();");
        _ = SyncUserLocationAsync(followCamera: true);
    }

#if ANDROID
    private static void StartAndroidWorkoutService()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var intent = new global::Android.Content.Intent(context, typeof(global::Fitness_App.Platforms.Android.WorkoutTrackingForegroundService));
            intent.SetAction(global::Fitness_App.Platforms.Android.WorkoutTrackingForegroundService.ActionRefresh);

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RecordPage.StartAndroidWorkoutService] {ex.Message}");
        }
    }

    private static void StopAndroidWorkoutService()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var intent = new global::Android.Content.Intent(context, typeof(global::Fitness_App.Platforms.Android.WorkoutTrackingForegroundService));
            context.StopService(intent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RecordPage.StopAndroidWorkoutService] {ex.Message}");
        }
    }
#else
    private static void StartAndroidWorkoutService()
    {
    }

    private static void StopAndroidWorkoutService()
    {
    }
#endif

    // ── Settings ──────────────────────────────────────────────────────────────
    private async void OnSettingsTapped(object sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("settings");
    }
}
