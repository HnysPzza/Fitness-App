using System.Diagnostics;

namespace Fitness_App.Pages;

[QueryProperty(nameof(SportType), "sportType")]
[QueryProperty(nameof(SportIconValue), "sportIcon")]
public partial class ActiveRecordingPage : ContentPage
{
    private string _sportType = "Running";
    public string SportType
    {
        get => _sportType;
        set
        {
            _sportType = value;
            SportNameLabel.Text = _sportType;
        }
    }

    private string _sportIconValue = "\ue566"; // default directions_run
    public string SportIconValue
    {
        get => _sportIconValue;
        set
        {
            _sportIconValue = value;
            SportIcon.Text = _sportIconValue;
        }
    }

    private bool _isPaused = false;
    private Stopwatch _stopwatch = new();
    private System.Timers.Timer? _updateTimer;
    private double _distance = 0.0;

    public ActiveRecordingPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartRecording();
    }

    private void StartRecording()
    {
        _stopwatch.Start();

        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(UpdateStats);
        };
        _updateTimer.Start();
    }

    private void UpdateStats()
    {
        if (_isPaused) return;

        var elapsed = _stopwatch.Elapsed;
        TimerLabel.Text = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

        // Simulating values based on a standard 6 min/km pace
        // 1 km = 6 mins (360s) -> distance per second = 1/360 km
        _distance += 1.0 / 360.0;
        DistanceLabel.Text = $"{_distance:F2} km";

        PaceLabel.Text = "6:00 /km";
        HrLabel.Text = "142 bpm";
    }

    private async void OnPauseResumeClicked(object sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        _isPaused = !_isPaused;

        if (_isPaused)
        {
            _stopwatch.Stop();
            PauseResumeButton.Text = "▶ Resume";
            PauseResumeButton.BackgroundColor = Color.FromArgb("#10B981");
        }
        else
        {
            _stopwatch.Start();
            PauseResumeButton.Text = "⏸ Pause";
            PauseResumeButton.BackgroundColor = Color.FromArgb("#F59E0B");
        }
    }

    private async void OnFinishClicked(object sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var result = await DisplayAlert(
            "Finish Activity",
            "Are you sure you want to finish and save this session?",
            "Finish",
            "Cancel");

        if (!result) return;

        _stopwatch.Stop();
        _updateTimer?.Stop();
        _updateTimer?.Dispose();

        await DisplayAlert("Activity Saved! 🎉", "Great job! Your activity has been logged.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _stopwatch.Stop();
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
