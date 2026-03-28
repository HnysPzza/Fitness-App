using System.Text;
using System.Text.Json;
using Fitness_App.Models;
using Fitness_App.Services;

namespace Fitness_App.Pages;

[QueryProperty(nameof(ActivityId), "activityId")]
public partial class ActivityDetailPage : ContentPage
{
    private readonly StatsService _statsService;
    private bool _mapReady;
    private bool _isLoading;
    private UserActivity? _activity;
    private string _activityId = string.Empty;

    public string ActivityId
    {
        get => _activityId;
        set
        {
            _activityId = Uri.UnescapeDataString(value ?? string.Empty);
            if (IsLoaded)
                _ = LoadActivityAsync();
        }
    }

    public ActivityDetailPage(StatsService statsService)
    {
        InitializeComponent();
        _statsService = statsService;
        MapWebView.Navigating += OnMapWebViewNavigating;
        MapWebView.Navigated += OnMapWebViewNavigated;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_activity == null || !string.Equals(_activity.Id, ActivityId, StringComparison.Ordinal))
            await LoadActivityAsync();
    }

    private async Task LoadActivityAsync()
    {
        if (_isLoading || string.IsNullOrWhiteSpace(ActivityId))
            return;

        _isLoading = true;
        try
        {
            MapLoadingOverlay.IsVisible = true;
            _activity = await _statsService.GetActivityByIdAsync(ActivityId);
            if (_activity == null)
            {
                await DisplayAlert("Activity unavailable", "This activity could not be loaded.", "OK");
                await GoBackAsync();
                return;
            }

            BindActivity(_activity);
            await LoadMapAsync(_activity);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void BindActivity(UserActivity activity)
    {
        TitleLabel.Text = string.IsNullOrWhiteSpace(activity.Sport) ? "Activity" : activity.Sport;
        DateLabel.Text = activity.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy • hh:mm tt");
        DistanceLabel.Text = $"{activity.DistanceKm:F2} km";
        DurationLabel.Text = ActivityPresentation.FormatDuration(activity.DurationTicks);
        MetricTitleLabel.Text = ActivityPresentation.GetMetricLabel(activity);
        MetricValueLabel.Text = ActivityPresentation.GetMetricValue(activity);
        PathSummaryLabel.Text = activity.RoutePoints.Count == 1
            ? "1 point saved"
            : $"{activity.RoutePoints.Count} points saved";
        RouteOverviewLabel.Text = activity.RoutePoints.Count > 1
            ? "Full recorded path highlighted from start to finish, framed to the complete route bounds."
            : "This activity only has a single recorded position, so the map centers on that saved point.";
    }

    private async Task LoadMapAsync(UserActivity activity)
    {
        _mapReady = false;
        MapLoadingOverlay.IsVisible = true;

        var coords = ActivityRouteCodec.ExtractCoordinates(activity.CoordinatesJson);
        var start = coords.FirstOrDefault();
        var startLng = start != null && start.Length >= 2 ? start[0] : Preferences.Default.Get("last_lng", 121.0);
        var startLat = start != null && start.Length >= 2 ? start[1] : Preferences.Default.Get("last_lat", 14.6);
        var html = await MapboxWebViewHtml.BuildMapHtmlAsync("mapbox_map.html", "outdoors-v12", startLng, startLat).ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            MapWebView.Source = new HtmlWebViewSource
            {
                Html = html,
                BaseUrl = "https://api.mapbox.com/"
            };
        });
    }

    private async void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        var url = e.Url ?? string.Empty;
        if (!url.Contains("fitnessapp.local/map-ready", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;
        _mapReady = true;
        MapLoadingOverlay.IsVisible = false;
        await RenderRouteAsync();
    }

    private void OnMapWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success)
            System.Diagnostics.Debug.WriteLine($"[ActivityDetailPage] WebView nav: {e.Result} {e.Url}");
    }

    private async Task RenderRouteAsync()
    {
        if (!_mapReady || _activity == null)
            return;

        var coords = ActivityRouteCodec.ExtractCoordinates(_activity.CoordinatesJson);
        if (coords.Length == 0)
            return;

        var json = JsonSerializer.Serialize(coords);
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var color = ActivityPresentation.GetRouteColor(_activity.Sport);

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                MapWebView.EvaluateJavaScriptAsync($"showRecordedRouteFromBase64({JsonSerializer.Serialize(b64)}, {JsonSerializer.Serialize(color)});"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ActivityDetailPage] Render route: {ex.Message}");
        }
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
        => await GoBackAsync();

    private static Task GoBackAsync()
    {
        if (Shell.Current != null)
            return Shell.Current.GoToAsync("..");
        return Task.CompletedTask;
    }
}
