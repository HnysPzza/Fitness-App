using Fitness_App.Services;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Fitness_App.Pages
{
    public partial class YouPage : ContentPage
    {
        // ─── State ───────────────────────────────────────────────────────────
        private IProfileService? _profileService;
        private StatsService? _statsService;
        private IActivitySaveNotifier? _activitySaveNotifier;
        private readonly List<UserActivity> _recentActivities = new();
        private bool _isRefreshing;
        private bool _refreshAgainRequested;
        private bool _hasLoadedStats;
        private bool _isPageVisible;
        private CancellationTokenSource? _deferredRefreshCts;
        private string _selectedPeriod = "Week";

        // Chart data: list of (label, value) pairs kept simple
        private readonly List<(string Label, float Value)> _chartData = new();
        private Color _accentColor = Color.FromArgb("#FC5200");

        // ─── Construction ───────────────────────────────────────────────────
        public YouPage()
        {
            InitializeComponent();
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            ResolveServices();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isPageVisible = true;

            ResolveServices();
            LoadProfileData();
            if (!_hasLoadedStats)
                ShowSkeletonValues();

            QueueDeferredRefresh();
        }

        private void ShowSkeletonValues()
        {
            try
            {
                StatKmLabel.Text         = "—";
                StatActivitiesLabel.Text = "—";
                StatTimeLabel.Text       = "—";
                StatSpeedLabel.Text      = "—";
                KmTrendLabel.Text        = "Loading…";
                KmTrendIcon.IsVisible    = false;
            }
            catch { /* not yet attached to visual tree */ }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isPageVisible = false;

            if (_profileService != null)
                _profileService.ProfileChanged -= OnProfileChanged;

            if (_activitySaveNotifier != null)
                _activitySaveNotifier.ActivitySaved -= OnActivitySaved;

            _deferredRefreshCts?.Cancel();
            _deferredRefreshCts?.Dispose();
            _deferredRefreshCts = null;
        }

        private void ResolveServices()
        {
            var services = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
            if (services == null)
                return;

            var profileService = services.GetService<IProfileService>();
            if (!ReferenceEquals(_profileService, profileService) && _profileService != null)
                _profileService.ProfileChanged -= OnProfileChanged;

            _profileService = profileService;

            if (_profileService != null)
            {
                _profileService.ProfileChanged -= OnProfileChanged;
                _profileService.ProfileChanged += OnProfileChanged;
            }

            _statsService ??= services.GetService<StatsService>();

            var activitySaveNotifier = services.GetService<IActivitySaveNotifier>();
            if (!ReferenceEquals(_activitySaveNotifier, activitySaveNotifier) && _activitySaveNotifier != null)
                _activitySaveNotifier.ActivitySaved -= OnActivitySaved;

            _activitySaveNotifier = activitySaveNotifier;

            if (_activitySaveNotifier != null)
            {
                _activitySaveNotifier.ActivitySaved -= OnActivitySaved;
                _activitySaveNotifier.ActivitySaved += OnActivitySaved;
            }
        }

        // ─── Profile ────────────────────────────────────────────────────────
        private void OnProfileChanged(object? sender, EventArgs e)
            => MainThread.BeginInvokeOnMainThread(LoadProfileData);

        private void LoadProfileData()
        {
            if (_profileService == null) return;

            UserNameLabel.Text = _profileService.UserName ?? "Athlete";

            if (!string.IsNullOrEmpty(_profileService.ProfilePhotoPath)
                && File.Exists(_profileService.ProfilePhotoPath))
            {
                ProfileImage.Source    = ImageSource.FromFile(_profileService.ProfilePhotoPath);
                ProfileImage.IsVisible = true;
                ProfileIcon.IsVisible  = false;
                ProfileIconContainer.IsVisible = false;
            }
            else
            {
                ProfileImage.IsVisible = false;
                ProfileIcon.IsVisible  = true;
                ProfileIconContainer.IsVisible = true;
            }
        }

        private void OnActivitySaved(object? sender, EventArgs e)
            => MainThread.BeginInvokeOnMainThread(QueueDeferredRefresh);

        private void QueueDeferredRefresh()
        {
            _deferredRefreshCts?.Cancel();
            _deferredRefreshCts?.Dispose();

            var cts = new CancellationTokenSource();
            _deferredRefreshCts = cts;
            _ = RunDeferredRefreshAsync(cts.Token);
        }

        private async Task RunDeferredRefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(120, cancellationToken);
                await RefreshAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YouPage] Deferred refresh: {ex.Message}");
            }
        }

        // ─── Stats (simulated) ───────────────────────────────────────────────
        private async Task RefreshAsync()
        {
            if (_statsService == null)
                return;

            if (_isRefreshing)
            {
                _refreshAgainRequested = true;
                return;
            }

            do
            {
                _refreshAgainRequested = false;
                _isRefreshing = true;
                try
                {
                    await _statsService.SyncPendingActivitiesAsync();

                    var now              = DateTime.Now;
                    var thisMonthStart   = new DateTime(now.Year, now.Month, 1);
                    var statsTask        = _statsService.GetAllTimeStatsAsync();
                    var thisMonthTask    = _statsService.GetStatsInRangeAsync(thisMonthStart, thisMonthStart.AddMonths(1));
                    var chartTask        = _statsService.GetChartDataAsync(_selectedPeriod);
                    var recentTask       = _statsService.GetRecentActivitiesAsync(3);
                    var trendTask        = _statsService.GetMonthlyKmComparisonAsync();

                    await Task.WhenAll(statsTask, thisMonthTask, chartTask, recentTask, trendTask);

                    var stats            = await statsTask;
                    var thisMonthStats   = await thisMonthTask;
                    var chartData        = await chartTask;
                    var recentActivities = await recentTask;
                    var trend            = await trendTask;

                    if (!_isPageVisible)
                        return;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try { ApplyStats(stats, thisMonthStats, trend); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[YouPage] ApplyStats: {ex.Message}"); }

                        try { ApplyChartData(chartData); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[YouPage] ApplyChartData: {ex.Message}"); }

                        try { ApplyRecentActivities(recentActivities); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[YouPage] ApplyRecentActivities: {ex.Message}"); }

                        _hasLoadedStats = true;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[YouPage] Refresh: {ex.Message}");
                }
                finally
                {
                    _isRefreshing = false;
                }
            } while (_refreshAgainRequested);
        }

        private void ApplyStats(UserStats stats, UserStats thisMonthStats, (double CurrentMonthKm, double LastMonthKm, double PercentChange) trend)
        {
            StatKmLabel.Text         = stats.TotalKmDisplay;
            StatActivitiesLabel.Text = stats.ActivitiesDisplay;
            StatTimeLabel.Text       = stats.TotalTimeDisplay;
            StatSpeedLabel.Text      = stats.AvgSpeedDisplay;
            StatActiveDaysLabel.Text = thisMonthStats.ActiveDaysDisplay;

            // ── Dynamic trend indicator ───────────────────────────────────
            var pct = trend.PercentChange;

            if (trend.CurrentMonthKm == 0 && trend.LastMonthKm == 0)
            {
                // No data at all — hide the trend row
                KmTrendIcon.IsVisible  = false;
                KmTrendLabel.IsVisible = false;
            }
            else
            {
                KmTrendIcon.IsVisible  = true;
                KmTrendLabel.IsVisible = true;

                var accentOrange = Color.FromArgb("#FC5200");
                var accentGreen  = Color.FromArgb("#22C55E");
                var accentRed    = Color.FromArgb("#EF4444");

                if (pct > 0)
                {
                    KmTrendIcon.Text      = UI.Icons.MaterialSymbols.Trending_up;
                    KmTrendIcon.TextColor  = accentGreen;
                    KmTrendLabel.TextColor = accentGreen;
                    KmTrendLabel.Text      = $"+{pct:F0}% vs last month";
                }
                else if (pct < 0)
                {
                    KmTrendIcon.Text      = UI.Icons.MaterialSymbols.Trending_down;
                    KmTrendIcon.TextColor  = accentRed;
                    KmTrendLabel.TextColor = accentRed;
                    KmTrendLabel.Text      = $"{pct:F0}% vs last month";
                }
                else
                {
                    KmTrendIcon.Text      = UI.Icons.MaterialSymbols.Trending_flat;
                    KmTrendIcon.TextColor  = accentOrange;
                    KmTrendLabel.TextColor = accentOrange;
                    KmTrendLabel.Text      = "No change vs last month";
                }
            }
        }

        // ─── Chart data builders ─────────────────────────────────────────────
        private void ApplyChartData(List<(string Label, float Value)> data)
        {
            _chartData.Clear();
            _chartData.AddRange(data);
            ChartCanvas.InvalidateSurface();
        }

        // ─── Tab control ─────────────────────────────────────────────────────
        private void OnTabWeek(object? sender, EventArgs e)  => SwitchTab("Week");
        private void OnTabMonth(object? sender, EventArgs e) => SwitchTab("Month");

        private void SwitchTab(string period)
        {
            if (_selectedPeriod == period) return;
            _selectedPeriod = period;

            _accentColor = period switch
            {
                "Week"  => Color.FromArgb("#10B981"),
                "Month" => Color.FromArgb("#2563EB"),
                _       => Color.FromArgb("#FC5200"),
            };

            var normalBg    = Colors.Transparent;
            var normalFg    = GetSecondaryTextColor();

            TabWeek.BackgroundColor   = normalBg;
            TabMonth.BackgroundColor  = normalBg;
            TabWeekLabel.TextColor    = normalFg;
            TabMonthLabel.TextColor   = normalFg;

            switch (period)
            {
                case "Week":
                    TabWeek.BackgroundColor     = _accentColor;
                    TabWeekLabel.TextColor      = Colors.White;
                    break;
                case "Month":
                    TabMonth.BackgroundColor    = _accentColor;
                    TabMonthLabel.TextColor     = Colors.White;
                    break;
            }

            _ = RefreshChartAsync(period);
        }

        private async Task RefreshChartAsync(string period)
        {
            if (_statsService == null)
                return;

            try
            {
                var data = await _statsService.GetChartDataAsync(period);
                await MainThread.InvokeOnMainThreadAsync(() => ApplyChartData(data));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YouPage] Refresh chart: {ex.Message}");
            }
        }

        private void ApplyRecentActivities(List<UserActivity> activities)
        {
            _recentActivities.Clear();
            _recentActivities.AddRange(activities);

            var cards = new[] { ActivityCard1, ActivityCard2, ActivityCard3 };
            var badges = new[] { ActivityBadge1, ActivityBadge2, ActivityBadge3 };
            var emojis = new[] { ActivityEmoji1, ActivityEmoji2, ActivityEmoji3 };
            var titles = new[] { ActivityTitle1, ActivityTitle2, ActivityTitle3 };
            var metas = new[] { ActivityMeta1, ActivityMeta2, ActivityMeta3 };

            RecentActivitiesEmptyLabel.IsVisible = activities.Count == 0;

            for (var i = 0; i < cards.Length; i++)
            {
                if (i < activities.Count)
                {
                    var activity = activities[i];
                    cards[i].IsVisible = true;
                    cards[i].HeightRequest = -1; // Auto height
                    cards[i].BindingContext = activity;
                    badges[i].BackgroundColor = GetMutedSurfaceColor();
                    emojis[i].Text = ActivityPresentation.GetSportIcon(activity.Sport);
                    titles[i].Text = string.IsNullOrWhiteSpace(activity.Sport) ? "Activity" : activity.Sport;

                    var daysAgo = (DateTime.UtcNow - activity.CreatedAt.ToUniversalTime()).Days;
                    string timeAgoStr = daysAgo switch
                    {
                        0 => "TODAY",
                        1 => "YESTERDAY",
                        _ => $"{daysAgo} DAYS AGO"
                    };
                    metas[i].Text = $"{activity.DistanceKm:F1} km • {ActivityPresentation.FormatDuration(activity.DurationTicks)} • {timeAgoStr}";
                }
                else
                {
                    cards[i].IsVisible = false;
                    cards[i].HeightRequest = 0;
                    cards[i].BindingContext = null;
                }
            }
        }

        // ─── SkiaSharp chart renderer ─────────────────────────────────────────
        private void OnChartPaint(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            if (_chartData.Count == 0) return;

            var info = e.Info;
            float w = info.Width;
            float h = info.Height;

            float padL = 40f, padR = 40f, padT = 54f, padB = 42f;
            float chartW = w - padL - padR;
            float chartH = h - padT - padB;

            float maxVal = _chartData.Max(d => d.Value);
            if (maxVal <= 0) maxVal = 1;

            // Determine accent color
            var hex = _accentColor.ToHex().TrimStart('#');
            SKColor accent;
            if (hex.Length == 8)
                accent = new SKColor(
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
            else
                accent = new SKColor(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));

            var normalBarColor = IsLightTheme()
                ? new SKColor(226, 232, 240) // #E2E8F0
                : new SKColor(27, 38, 59);   // #1B263B
            
            // Fonts and Paints
            using var textFont = new SKFont(SKTypeface.Default, 24f); 
            using var labelPaint = new SKPaint
            {
                Color = IsLightTheme()
                    ? new SKColor(100, 116, 139) // #64748B
                    : new SKColor(148, 163, 184), // #94A3B8
                IsAntialias = true
            };
            using var normalBarPaint = new SKPaint
            {
                Color = normalBarColor,
                IsAntialias = true
            };

            float barWidth = Math.Min(32f, chartW / _chartData.Count * 0.6f);
            int maxIdx = _chartData.FindIndex(d => d.Value == maxVal);

            for (int i = 0; i < _chartData.Count; i++)
            {
                float xCenter = padL + (chartW / Math.Max(1, _chartData.Count - 1)) * i;
                float barH = (_chartData[i].Value / maxVal) * chartH;
                if (barH < 4) barH = 4; // minimum height
                
                float xStart = xCenter - (barWidth / 2f);
                float yStart = padT + chartH - barH;

                var rect = new SKRect(xStart, yStart, xStart + barWidth, padT + chartH);

                if (i == maxIdx)
                {
                    // Gradient for the highlighted bar
                    using var activeShader = SKShader.CreateLinearGradient(
                        new SKPoint(0, yStart),
                        new SKPoint(0, padT + chartH),
                        new[] { accent, accent.WithAlpha(150) },
                        SKShaderTileMode.Clamp);
                        
                    using var activeBarPaint = new SKPaint
                    {
                        Shader = activeShader,
                        IsAntialias = true
                    };
                    canvas.DrawRoundRect(rect, barWidth / 2f, barWidth / 2f, activeBarPaint);
                }
                else
                {
                    canvas.DrawRoundRect(rect, barWidth / 2f, barWidth / 2f, normalBarPaint);
                }

                // Top Floating Label for Highlighted Bar
                if (i == maxIdx)
                {
                    using var tagPaint = new SKPaint { Color = accent.WithAlpha(40), IsAntialias = true };
                    using var tagTextPaint = new SKPaint { Color = accent, IsAntialias = true };
                    
                    const float tagHalfWidth = 34f;
                    var tagRect = new SKRect(xCenter - tagHalfWidth, yStart - 36f, xCenter + tagHalfWidth, yStart - 10f);
                    canvas.DrawRoundRect(tagRect, 4f, 4f, tagPaint);
                    
                    using var valueFont = new SKFont(SKTypeface.Default, 18f);
                    canvas.DrawText(_chartData[i].Value.ToString("0.0"), xCenter, yStart - 17f, SKTextAlign.Center, valueFont, tagTextPaint);
                }

                // X-Axis Text Labels
                string labelPrefix = _chartData[i].Label.Length > 3 ? _chartData[i].Label.Substring(0, 3).ToUpper() : _chartData[i].Label.ToUpper();
                canvas.DrawText(labelPrefix, xCenter, h - 8, SKTextAlign.Center, textFont, labelPaint);
            }
        }

        private static bool IsLightTheme()
        {
            return Application.Current?.RequestedTheme == AppTheme.Light;
        }

        private static Color GetSecondaryTextColor()
        {
            return IsLightTheme()
                ? Color.FromArgb("#64748B")
                : Color.FromArgb("#94A3B8");
        }

        private static Color GetMutedSurfaceColor()
        {
            return IsLightTheme()
                ? Color.FromArgb("#F1F5F9")
                : Color.FromArgb("#1B263B");
        }

        // ─── Navigation ──────────────────────────────────────────────────────
        private async void OnRecentActivityTapped(object? sender, TappedEventArgs e)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            if (sender is not BindableObject bindable || bindable.BindingContext is not UserActivity activity || string.IsNullOrWhiteSpace(activity.Id))
                return;

            try
            {
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync($"activitydetail?activityId={Uri.EscapeDataString(activity.Id)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            try
            {
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("generalsettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
    }
}
