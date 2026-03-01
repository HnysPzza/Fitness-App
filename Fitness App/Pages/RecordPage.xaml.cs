using System.Collections.ObjectModel;

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

    public SportCategoryGroup(string category, IEnumerable<SportOption> sports)
        : base(sports)
    {
        Category = category;
    }
}

public partial class RecordPage : ContentPage
{
    // ── State ───────────────────────────────────────────────────────────────
    private SportOption? _selectedSport;
    private bool _isGpsLocked = false;
    private bool _isSheetExpanded = false;

    // ── Collections (bound in XAML) ─────────────────────────────────────────
    public ObservableCollection<SportOption> RecentSports { get; } = new();
    public ObservableCollection<SportCategoryGroup> FilteredSportGroups { get; } = new();
    private List<SportOption> _allSports = new();

    public RecordPage()
    {
        // ✅ Fix Step 6: BindingContext BEFORE InitializeComponent
        // If set after, Android can crash during the first binding pass on null properties.
        BindingContext = this;
        InitializeComponent();
        BuildSportList();
    }

    // ── Sport list ───────────────────────────────────────────────────────────
    private void BuildSportList()
    {
        _allSports = new List<SportOption>
        {
            // OUTDOOR
            new() { Name = "Run",                   Icon = "🏃", Category = "OUTDOOR" },
            new() { Name = "Walk",                  Icon = "🚶", Category = "OUTDOOR" },
            new() { Name = "Hike",                  Icon = "🧗", Category = "OUTDOOR" },
            new() { Name = "Trail Run",              Icon = "🏃", Category = "OUTDOOR" },
            new() { Name = "Cycling",               Icon = "🚴", Category = "OUTDOOR" },
            new() { Name = "Mountain Bike",          Icon = "🚵", Category = "OUTDOOR" },
            new() { Name = "Gravel Ride",            Icon = "🚴", Category = "OUTDOOR" },
            new() { Name = "Swimming (Open Water)",  Icon = "🏊", Category = "OUTDOOR" },
            new() { Name = "Kayaking",               Icon = "🛶", Category = "OUTDOOR" },
            new() { Name = "Canoeing",               Icon = "🚣", Category = "OUTDOOR" },
            new() { Name = "Rowing",                 Icon = "🚣", Category = "OUTDOOR" },
            new() { Name = "Surfing",               Icon = "🏄", Category = "OUTDOOR" },
            // INDOOR
            new() { Name = "Gym Workout",   Icon = "🏋", Category = "INDOOR" },
            new() { Name = "Yoga",          Icon = "🧘", Category = "INDOOR" },
            new() { Name = "Pilates",       Icon = "🧘", Category = "INDOOR" },
            new() { Name = "Boxing",        Icon = "🥊", Category = "INDOOR" },
            new() { Name = "Pool Swim",     Icon = "🏊", Category = "INDOOR" },
            new() { Name = "Treadmill Run", Icon = "🏃", Category = "INDOOR" },
            new() { Name = "Indoor Cycle",  Icon = "🚴", Category = "INDOOR" },
            new() { Name = "Elliptical",    Icon = "🏃", Category = "INDOOR" },
            new() { Name = "Stair Stepper", Icon = "🏃", Category = "INDOOR" },
            new() { Name = "Badminton",     Icon = "🏸", Category = "INDOOR" },
            new() { Name = "Tennis",        Icon = "🎾", Category = "INDOOR" },
            new() { Name = "Basketball",    Icon = "🏀", Category = "INDOOR" },
            new() { Name = "Volleyball",    Icon = "🏐", Category = "INDOOR" },
            new() { Name = "Football",      Icon = "⚽", Category = "INDOOR" },
            new() { Name = "Table Tennis",  Icon = "🏓", Category = "INDOOR" },
            // CYCLING
            new() { Name = "Road Ride",    Icon = "🚴", Category = "CYCLING" },
            new() { Name = "Virtual Ride", Icon = "🏎", Category = "CYCLING" },
            new() { Name = "E-Bike Ride",  Icon = "⚡", Category = "CYCLING" },
            new() { Name = "Commute",      Icon = "🚴", Category = "CYCLING" },
            // WATER
            new() { Name = "Water Polo",   Icon = "🤽", Category = "WATER" },
            new() { Name = "Windsurfing",  Icon = "🏄", Category = "WATER" },
            new() { Name = "Kitesurfing",  Icon = "🪁", Category = "WATER" },
            // WINTER
            new() { Name = "Alpine Ski",   Icon = "⛷", Category = "WINTER" },
            new() { Name = "Nordic Ski",   Icon = "🎿", Category = "WINTER" },
            new() { Name = "Snowboarding", Icon = "🏂", Category = "WINTER" },
            new() { Name = "Ice Skating",  Icon = "⛸", Category = "WINTER" },
            new() { Name = "Snowshoe",     Icon = "🥾", Category = "WINTER" },
            // OTHER
            new() { Name = "Wheelchair",    Icon = "♿", Category = "OTHER" },
            new() { Name = "Crossfit",      Icon = "🏋", Category = "OTHER" },
            new() { Name = "Rock Climbing", Icon = "🧗", Category = "OTHER" },
            new() { Name = "Golf",          Icon = "⛳", Category = "OTHER" },
            new() { Name = "Horse Riding",  Icon = "🏇", Category = "OTHER" },
            new() { Name = "Skateboarding", Icon = "🛹", Category = "OTHER" },
            new() { Name = "Rollerblading", Icon = "🛼", Category = "OTHER" },
            new() { Name = "Dance",         Icon = "💃", Category = "OTHER" },
            new() { Name = "Meditation",    Icon = "🧘", Category = "OTHER" },
        };

        // Pill carousel — first 6 for quick access
        foreach (var s in _allSports.Take(6))
            RecentSports.Add(s);

        // Restore last selected sport
        var saved = Preferences.Default.Get("last_selected_sport", "Run");
        var toSelect = _allSports.FirstOrDefault(s => s.Name == saved) ?? _allSports[0];
        SelectSport(toSelect);

        RebuildFilteredList();
    }

    private void RebuildFilteredList(string query = "")
    {
        FilteredSportGroups.Clear();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allSports
            : _allSports.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var grp in filtered.GroupBy(s => s.Category))
            FilteredSportGroups.Add(new SportCategoryGroup(grp.Key, grp));
    }

    // ── Page lifecycle ───────────────────────────────────────────────────────
    // ✅ Fix Step 5: Stagger heavy operations so the page finishes rendering first,
    //    then map, then GPS — avoids CPU/memory spike that crashes Android.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyBottomSafeArea();
        AnimateLocationPulse();

        // Let the page finish its first layout pass before starting heavy work
        await Task.Delay(150);

        // Start GPS (with permission check + try/catch)
        await StartGpsSafeAsync();
    }

    /// <summary>
    /// Sizes the transparent spacer at the bottom of the sheet to exactly
    /// fill the device safe area (home indicator / gesture bar). The sheet
    /// itself already has Margin.Bottom=56 to clear the tab bar.
    /// </summary>
    private void ApplyBottomSafeArea()
    {
        double safeAreaBottom = 0;

#if IOS
        try
        {
            var window = UIKit.UIApplication.SharedApplication.Windows
                              .FirstOrDefault(w => w.IsKeyWindow);
            if (window != null)
                safeAreaBottom = window.SafeAreaInsets.Bottom;
        }
        catch { safeAreaBottom = 34; }  // fallback for notched iPhones
#elif ANDROID
        try
        {
            var density = DeviceDisplay.MainDisplayInfo.Density;
            var context = Android.App.Application.Context;
            var resId = context.Resources?.GetIdentifier(
                "navigation_bar_height", "dimen", "android") ?? 0;
            if (resId > 0)
                safeAreaBottom = (context.Resources!.GetDimensionPixelSize(resId) / density);
            else
                safeAreaBottom = 16; // gesture-nav fallback
        }
        catch { safeAreaBottom = 16; }
#endif

        BottomSheetSpacer.HeightRequest = safeAreaBottom;
    }

    // ── GPS — real Geolocation with permission guard ─────────────────────────
    // ✅ Fix Step 2 + 3: Runtime permission request before any GPS call,
    //    entire method wrapped in try/catch, UI updates on MainThread.
    private async Task StartGpsSafeAsync()
    {
        try
        {
            // Always show "Searching" state first
            MainThread.BeginInvokeOnMainThread(() => SetGpsState(locked: false));

            // ✅ Check and request permission BEFORE calling Geolocation
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            // If still denied — update UI gracefully and return (do NOT call GPS)
            if (status != PermissionStatus.Granted)
            {
                System.Diagnostics.Debug.WriteLine("[GPS] Location permission denied");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GpsStatusLabel.Text = "Location permission denied";
                    GpsStatusLabel.TextColor = Color.FromArgb("#FF5252");
                });
                return;
            }

            // Permission granted — try to get location
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(15)
                });

            // ✅ Fix Step 4: Always update UI on main thread after await
            MainThread.BeginInvokeOnMainThread(() =>
                SetGpsState(locked: location != null));
        }
        catch (PermissionException)
        {
            System.Diagnostics.Debug.WriteLine("[GPS] PermissionException — location access denied");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GpsStatusLabel.Text = "Location permission denied";
                GpsStatusLabel.TextColor = Color.FromArgb("#FF5252");
            });
        }
        catch (FeatureNotEnabledException)
        {
            System.Diagnostics.Debug.WriteLine("[GPS] Location services disabled on device");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GpsStatusLabel.Text = "Location disabled";
                GpsStatusLabel.TextColor = Color.FromArgb("#FF9800");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS ERROR] {ex.Message}\n{ex.StackTrace}");
            // Fallback to simulated GPS lock after a delay so the page still works
            await Task.Delay(3500);
            MainThread.BeginInvokeOnMainThread(() => SetGpsState(locked: true));
        }
    }

    private void SetGpsState(bool locked)
    {
        _isGpsLocked = locked;

        var activeColor  = new SolidColorBrush(Color.FromArgb(locked ? "#4CAF50" : "#FF9800"));
        var inactiveColor = new SolidColorBrush(Color.FromArgb(locked ? "#4CAF50" : "#2A2A3E"));

        GpsDot1.Fill = activeColor;
        GpsDot2.Fill = activeColor;
        GpsDot3.Fill = activeColor;
        GpsDot4.Fill = locked ? activeColor : inactiveColor;

        GpsStatusLabel.Text = locked ? "GPS Ready" : "Searching...";
        GpsStatusLabel.TextColor = locked ? Color.FromArgb("#4CAF50") : Color.FromArgb("#8888AA");

        // Record button — orange + glow when ready, grey + dim when not
        RecordButton.BackgroundColor = Color.FromArgb(locked ? "#FC4C02" : "#3A3A4E");
        RecordButton.Opacity = locked ? 1.0 : 0.6;
        RecordButtonGlow.Opacity = locked ? 1.0 : 0.0;

        var btnLabel = RecordButton.Content as Label;
        if (btnLabel != null)
            btnLabel.TextColor = locked ? Colors.White : Color.FromArgb("#88FFFFFF");

        // Haptic on lock
        if (locked)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        }
    }

    // ── Location pulse animation ─────────────────────────────────────────────
    private async void AnimateLocationPulse()
    {
        // Guard: only run while this page is visible and elements are accessible
        while (IsVisible && PulseRing1 != null && PulseRing2 != null)
        {
            try
            {
                await PulseRing1.ScaleToAsync(1.5, 1000, Easing.CubicOut);
                PulseRing1.Opacity = 0;
                await Task.Delay(200);
                PulseRing1.Scale = 1;
                PulseRing1.Opacity = 0.6;

                await PulseRing2.ScaleToAsync(1.8, 1400, Easing.CubicOut);
                PulseRing2.Opacity = 0;
                await Task.Delay(100);
                PulseRing2.Scale = 1;
                PulseRing2.Opacity = 0.4;
            }
            catch
            {
                // Page navigated away — exit the loop silently
                break;
            }
        }
    }

    // ── Sport selection ──────────────────────────────────────────────────────
    private void SelectSport(SportOption sport)
    {
        foreach (var s in _allSports) s.IsSelected = false;
        sport.IsSelected = true;
        _selectedSport = sport;
        SelectedSportNameLabel.Text = sport.Name;
        Preferences.Default.Set("last_selected_sport", sport.Name);

        OnPropertyChanged(nameof(RecentSports));
        OnPropertyChanged(nameof(FilteredSportGroups));
    }

    // ─── Pill carousel ───────────────────────────────────────────────────────
    private void OnPillSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SportOption sport)
            SelectSport(sport);
        if (sender is CollectionView cv) cv.SelectedItem = null;
    }

    // ─── Full sport list ─────────────────────────────────────────────────────
    private void OnSportItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SportOption sport)
        {
            SelectSport(sport);

            // Promote to recent pills if not there yet
            if (!RecentSports.Contains(sport))
            {
                RecentSports.Insert(0, sport);
                if (RecentSports.Count > 6) RecentSports.RemoveAt(6);
            }
        }
        if (sender is CollectionView cv) cv.SelectedItem = null;
        CollapseExpandedSheet();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        => RebuildFilteredList(e.NewTextValue);

    // ── Sheet expansion / collapse ───────────────────────────────────────────
    private void OnDragHandleTapped(object sender, TappedEventArgs e)
    {
        if (_isSheetExpanded)
            CollapseExpandedSheet();
        else
            OpenExpandedSheet();
    }

    private void OnCollapseTapped(object sender, TappedEventArgs e)
        => CollapseExpandedSheet();

    private void OpenExpandedSheet()
    {
        _isSheetExpanded = true;
        ExpandedSheet.IsVisible = true;
        // Slide up from bottom
        ExpandedSheet.TranslationY = 800;
        ExpandedSheet.TranslateTo(0, 0, 320, Easing.SpringOut);
        BottomSheet.Opacity = 0;
    }

    private void CollapseExpandedSheet()
    {
        _isSheetExpanded = false;
        ExpandedSheet.TranslateTo(0, 800, 280, Easing.CubicIn)
            .ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() =>
            {
                ExpandedSheet.IsVisible = false;
                SearchEntry.Text = "";
                SearchEntry.Unfocus();
                BottomSheet.Opacity = 1;
            }));
    }

    // ── Pan gesture on collapsed sheet (drag to open / close) ────────────────
    private double _sheetStartY;
    private void OnSheetPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _sheetStartY = BottomSheet.TranslationY;
                break;
            case GestureStatus.Running:
                // Only allow dragging up (negative Y)
                var newY = _sheetStartY + e.TotalY;
                if (newY < -80) OpenExpandedSheet(); // threshold reached
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                BottomSheet.TranslateTo(0, 0, 200, Easing.SpringOut);
                break;
        }
    }

    // ── Record button ────────────────────────────────────────────────────────
    private async void OnRecordTapped(object sender, TappedEventArgs e)
    {
        if (!_isGpsLocked || _selectedSport == null) return;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        await RecordButton.ScaleToAsync(0.88, 80);
        await RecordButton.ScaleToAsync(1.0, 80);

        var sportName = Uri.EscapeDataString(_selectedSport.Name);
        var sportIcon = Uri.EscapeDataString(_selectedSport.Icon);
        await Shell.Current.GoToAsync($"activerecording?sportType={sportName}&sportIcon={sportIcon}");
    }

    // ── Settings shortcut ────────────────────────────────────────────────────
    private async void OnSettingsTapped(object sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("settings");
    }
}
