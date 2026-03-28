using System.Diagnostics;

namespace Fitness_App.Pages;

[QueryProperty(nameof(SportType), "sportType")]
[QueryProperty(nameof(SportIconValue), "sportIcon")]
public partial class ActiveRecordingPage : ContentPage
{
    // ── Query properties (set by Shell navigation) ───────────────────────────
    private string _sportType = "Running";
    public string SportType
    {
        get => _sportType;
        set => _sportType = Uri.UnescapeDataString(value ?? "Running");
    }

    private string _sportIconValue = "🏃";
    public string SportIconValue
    {
        get => _sportIconValue;
        set => _sportIconValue = Uri.UnescapeDataString(value ?? "🏃");
    }

    // ── Recording state ───────────────────────────────────────────────────────
    private bool _isPaused            = false;
    private bool _isShowingFinish     = false;
    private readonly Stopwatch _stopwatch  = new();
    private System.Timers.Timer? _updateTimer;
    private double _distance = 0.0;

    public ActiveRecordingPage()
    {
        InitializeComponent();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Apply query property values now — elements are fully inflated
        SportNameLabel.Text  = _sportType;
        SportIcon.Text       = _sportIconValue;

        StartRecording();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTimers();
    }

    // ── Timer / stats ─────────────────────────────────────────────────────────
    private void StartRecording()
    {
        _stopwatch.Restart();
        _distance = 0.0;

        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += (_, _) => MainThread.BeginInvokeOnMainThread(UpdateStats);
        _updateTimer.Start();
    }

    private void StopTimers()
    {
        _stopwatch.Stop();
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _updateTimer = null;
    }

    private void UpdateStats()
    {
        if (_isPaused) return;

        var elapsed = _stopwatch.Elapsed;
        TimerLabel.Text   = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        _distance        += 1.0 / 360.0; // simulated 6 min/km pace
        DistanceLabel.Text = $"{_distance:F2} km";
        PaceLabel.Text     = "6:00 /km";
        HrLabel.Text       = "142 bpm";
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────
    private async void OnPauseResumeClicked(object sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        _isPaused = !_isPaused;

        if (_isPaused)
        {
            _stopwatch.Stop();
            PauseResumeButton.Text            = "▶  Resume";
            PauseResumeButton.BackgroundColor = Color.FromArgb("#10B981");
        }
        else
        {
            _stopwatch.Start();
            PauseResumeButton.Text            = "⏸  Pause";
            PauseResumeButton.BackgroundColor = Color.FromArgb("#F59E0B");
        }

        await Task.CompletedTask;
    }

    // ── Finish button ─────────────────────────────────────────────────────────
    private async void OnFinishClicked(object sender, EventArgs e)
    {
        if (_isShowingFinish) return;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        // Snapshot current stats into the confirm sheet before it animates in
        var elapsed = _stopwatch.Elapsed;
        FinishTimeLabel.Text     = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        FinishDistanceLabel.Text = $"{_distance:F2} km";

        await ShowFinishSheetAsync();
    }

    // ── Keep recording (dismiss sheet) ───────────────────────────────────────
    private async void OnKeepRecordingClicked(object sender, EventArgs e)
        => await HideFinishSheetAsync();

    private async void OnFinishBackdropTapped(object sender, TappedEventArgs e)
        => await HideFinishSheetAsync();

    // ── Save ──────────────────────────────────────────────────────────────────
    private async void OnSaveActivityClicked(object sender, EventArgs e)
    {
        // Stop timers so elapsed is frozen
        StopTimers();

        // Snapshot into the "saved" sheet
        SavedSportLabel.Text   = _sportType;
        SavedTimeLabel.Text    = FinishTimeLabel.Text;
        SavedDistanceLabel.Text = FinishDistanceLabel.Text;

        // Close confirm, open saved — both within the page (no DisplayAlert!)
        await HideFinishSheetAsync();
        await ShowSavedSheetAsync();
    }

    // ── Done (after save) ─────────────────────────────────────────────────────
    private async void OnDoneClicked(object sender, EventArgs e)
    {
        await HideSavedSheetAsync();
        await Shell.Current.GoToAsync("..");
    }

    // ── Sheet helpers ─────────────────────────────────────────────────────────
    private async Task ShowFinishSheetAsync()
    {
        _isShowingFinish = true;

        // Show backdrop first with a solid dark colour so the background never
        // shows as gray — the backdrop itself IS the background during transition
        FinishBackdrop.InputTransparent = false;
        FinishBackdrop.IsVisible        = true;

        FinishConfirmSheet.TranslationY = 600;
        FinishConfirmSheet.IsVisible    = true;

        // Give MAUI one frame to measure the sheet before animating
        await Task.Delay(16);

        var anim = FinishConfirmSheet.TranslateTo(0, 0, 280, Easing.CubicOut);
        await Task.WhenAny(anim, Task.Delay(400));
        FinishConfirmSheet.TranslationY = 0; // guarantee visible
    }

    private async Task HideFinishSheetAsync()
    {
        var anim = FinishConfirmSheet.TranslateTo(0, 600, 240, Easing.CubicIn);
        await Task.WhenAny(anim, Task.Delay(350));

        FinishConfirmSheet.IsVisible    = false;
        FinishBackdrop.IsVisible        = false;
        FinishBackdrop.InputTransparent = true;
        _isShowingFinish = false;
    }

    private async Task ShowSavedSheetAsync()
    {
        SavedBackdrop.InputTransparent = false;
        SavedBackdrop.IsVisible        = true;

        SavedSheet.TranslationY = 600;
        SavedSheet.IsVisible    = true;

        await Task.Delay(16);

        var anim = SavedSheet.TranslateTo(0, 0, 280, Easing.CubicOut);
        await Task.WhenAny(anim, Task.Delay(400));
        SavedSheet.TranslationY = 0;
    }

    private async Task HideSavedSheetAsync()
    {
        var anim = SavedSheet.TranslateTo(0, 600, 240, Easing.CubicIn);
        await Task.WhenAny(anim, Task.Delay(350));

        SavedSheet.IsVisible        = false;
        SavedBackdrop.IsVisible     = false;
        SavedBackdrop.InputTransparent = true;
    }
}
