# Task: Debug Record Page Crash — Android Only

---

## Goal
Find and fix the root cause of the Record Page crash on Android.
Follow each step in order. Stop when the crash is resolved.

---

## Step 1 — Get the Crash Log

Do this first. Never debug blind.

Open a terminal and run:
```bash
# Clear old logs
adb logcat -c

# Start capturing crash output
adb logcat AndroidRuntime:E DOTNET:E System.err:W *:S
```

Open the app, navigate to the Record Page, let it crash, then
copy everything in the terminal from `FATAL EXCEPTION` down to
the end of the stack trace and paste it somewhere you can read it.

This single step will tell you exactly which of the fixes below
you actually need.

---

## Step 2 — Check Android Permissions

This is the most common cause of crashes on Android specifically.
Missing location permissions cause an immediate unhandled exception
the moment GPS is accessed.

### AndroidManifest.xml
Open `Platforms/Android/AndroidManifest.xml` and verify these
two lines exist inside `<manifest>`:

```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION"/>
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION"/>
<uses-permission android:name="android.permission.INTERNET"/>
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE"/>
```

If any are missing, add them, then clean and rebuild.

### Runtime Permission Request
Android 6.0+ requires permissions to be requested at runtime,
not just declared in the manifest. Verify your GPS code checks
permission before calling Geolocation:

```csharp
private async Task StartGpsAsync()
{
    // ✅ Check BEFORE calling GetLocationAsync
    var status = await Permissions
        .CheckStatusAsync<Permissions.LocationWhenInUse>();

    if (status != PermissionStatus.Granted)
    {
        status = await Permissions
            .RequestAsync<Permissions.LocationWhenInUse>();
    }

    // If still denied — return safely, do NOT call GPS
    if (status != PermissionStatus.Granted)
    {
        GpsStatusText = "Location permission denied";
        return;
    }

    // Safe to call now
    var location = await Geolocation.GetLocationAsync(
        new GeolocationRequest
        {
            DesiredAccuracy = GeolocationAccuracy.Best,
            Timeout = TimeSpan.FromSeconds(15)
        });
}
```

---

## Step 3 — Wrap Every Heavy Call in Try/Catch

On Android, unhandled exceptions from background tasks
crash the entire app with no visible error in the UI.
Wrap the three heaviest operations on the Record Page:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();

    // Run all three safely — none should be able to crash the app
    await InitializeMapSafeAsync();
    await StartGpsSafeAsync();
}

private async Task InitializeMapSafeAsync()
{
    try
    {
        await RecordMap.LoadStyleAsync(
            "mapbox://styles/mapbox/dark-v11");

        var last = await Geolocation.GetLastKnownLocationAsync();
        if (last != null)
            RecordMap.CenterOn(last.Latitude, last.Longitude, zoom: 15);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[MAP ERROR] {ex.Message}\n{ex.StackTrace}");

        // Hide map on failure — don't crash
        RecordMap.IsVisible = false;
    }
}

private async Task StartGpsSafeAsync()
{
    try
    {
        var status = await Permissions
            .CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
            status = await Permissions
                .RequestAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
        {
            UpdateGpsUI(ready: false, text: "Permission denied");
            return;
        }

        var location = await Geolocation.GetLocationAsync(
            new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                Timeout = TimeSpan.FromSeconds(15)
            });

        // ✅ Always update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
            UpdateGpsUI(ready: location != null,
                        text: location != null
                            ? "GPS Ready"
                            : "Searching..."));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[GPS ERROR] {ex.Message}\n{ex.StackTrace}");

        MainThread.BeginInvokeOnMainThread(() =>
            UpdateGpsUI(ready: false, text: "GPS unavailable"));
    }
}
```

---

## Step 4 — Fix Thread Violations

Android crashes immediately if you update the UI from a
background thread. This is silent and hard to spot.

```csharp
// ❌ Crash — UI update from background thread
var location = await Geolocation.GetLocationAsync(...);
GpsLabel.Text = "GPS Ready"; // ← crashes on Android

// ✅ Safe — always wrap UI updates in MainThread
var location = await Geolocation.GetLocationAsync(...);
MainThread.BeginInvokeOnMainThread(() =>
{
    GpsLabel.Text = "GPS Ready";
    GpsDot1.Color = Colors.Green;
    RecordButton.IsEnabled = true;
});
```

Search your `RecordPage.xaml.cs` and `RecordViewModel.cs` for
any property or UI update that happens after an `await` call
and wrap it in `MainThread.BeginInvokeOnMainThread`.

---

## Step 5 — Stagger Heavy Operations on Page Load

Mapbox + GPS loading simultaneously on `OnAppearing` spikes
memory and CPU on Android. Stagger them with small delays:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();

    // Let the page finish rendering first
    await Task.Delay(150);

    // Then load the map
    await InitializeMapSafeAsync();

    // Then start GPS after map is up
    await Task.Delay(100);
    await StartGpsSafeAsync();
}
```

---

## Step 6 — Verify ViewModel Is Initialized Before Binding

If `BindingContext` is set after `InitializeComponent`, Android
can crash during the first binding pass on null properties.

```csharp
public RecordPage()
{
    // ✅ BindingContext BEFORE InitializeComponent
    BindingContext = new RecordViewModel();
    InitializeComponent();
}
```

And in the ViewModel constructor, initialize everything:

```csharp
public RecordViewModel()
{
    // ✅ Never leave these null
    Sports = new ObservableCollection<SportItem>();
    SelectedSport = "Run";
    GpsStatusText = "Searching...";
    GpsColor = Colors.Orange;
    IsRecordButtonEnabled = false;

    LoadSports();
}
```

---

## Step 7 — Check CollectionView Layout

A `CollectionView` inside a `ScrollView` causes an infinite
measure loop on Android that eventually crashes the app.

```xml
<!-- ❌ Never do this on Android -->
<ScrollView>
    <StackLayout>
        <CollectionView ItemsSource="{Binding Sports}"/>
    </StackLayout>
</ScrollView>

<!-- ✅ CollectionView must have explicit HeightRequest -->
<CollectionView ItemsSource="{Binding Sports}"
                ItemsLayout="HorizontalList"
                HeightRequest="44"/>
```

---

## Quick Reference — Most Likely Causes by Crash Timing

| When does it crash? | Most likely cause |
|---|---|
| Instantly on page open | Missing permissions or null BindingContext |
| 1–2 seconds after open | GPS permission exception |
| During map load | Mapbox token invalid or no internet permission |
| When scrolling sport pills | CollectionView layout issue |
| Randomly after a few seconds | Thread violation or memory spike |

---

## Acceptance Criteria

- [ ] ADB logcat stack trace captured and root cause identified
- [ ] `ACCESS_FINE_LOCATION` and `ACCESS_COARSE_LOCATION` in manifest
- [ ] Runtime permission requested before any GPS call
- [ ] Map init and GPS start both wrapped in try/catch
- [ ] All UI updates after await calls use `MainThread.BeginInvokeOnMainThread`
- [ ] `BindingContext` set before `InitializeComponent` in RecordPage
- [ ] `Sports` collection initialized in ViewModel constructor
- [ ] No `CollectionView` nested inside `ScrollView`
- [ ] Record Page loads and runs without crashing on Android device