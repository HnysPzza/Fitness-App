using Fitness_App.Services;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Fitness_App.Pages
{
    public partial class YouPage : ContentPage
    {
        // ─── State ───────────────────────────────────────────────────────────
        private IProfileService? _profileService;
        private string _selectedPeriod = "Today";

        // Chart data: list of (label, value) pairs kept simple
        private readonly List<(string Label, float Value)> _chartData = new();
        private Color _accentColor = Color.FromArgb("#FC5200");

        // ─── Construction ───────────────────────────────────────────────────
        public YouPage()
        {
            InitializeComponent();
            // Defer heavy work until the page actually appears
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();

            if (_profileService != null)
            {
                _profileService.ProfileChanged -= OnProfileChanged;
                _profileService.ProfileChanged += OnProfileChanged;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();

            if (_profileService != null)
            {
                _profileService.ProfileChanged -= OnProfileChanged;
                _profileService.ProfileChanged += OnProfileChanged;
                LoadProfileData();
            }

            // Load stats + chart off the main thread
            _ = Task.Run(() =>
            {
                var stats = GenerateStats();
                var data  = BuildChartData(_selectedPeriod);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ApplyStats(stats);
                    _chartData.Clear();
                    _chartData.AddRange(data);
                    ChartCanvas.InvalidateSurface();
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_profileService != null)
                _profileService.ProfileChanged -= OnProfileChanged;
        }

        // ─── Profile ────────────────────────────────────────────────────────
        private void OnProfileChanged(object? sender, EventArgs e)
            => MainThread.BeginInvokeOnMainThread(LoadProfileData);

        private void LoadProfileData()
        {
            if (_profileService == null) return;

            UserNameLabel.Text = $"{_profileService.UserName} 🔥";

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

        // ─── Stats (simulated) ───────────────────────────────────────────────
        private record StatSnapshot(string Km, string Activities, string Time, string Speed);

        private static StatSnapshot GenerateStats()
        {
            var r = new Random();
            double km     = Math.Round(r.NextDouble() * 300 + 50, 1);
            int    acts   = r.Next(10, 80);
            int    hours  = r.Next(5, 40);
            int    mins   = r.Next(0, 59);
            double speed  = Math.Round(r.NextDouble() * 8 + 5, 1);
            return new StatSnapshot($"{km}", $"{acts}", $"{hours}h {mins}m", $"{speed}");
        }

        private void ApplyStats(StatSnapshot s)
        {
            StatKmLabel.Text         = s.Km;
            StatActivitiesLabel.Text = s.Activities;
            StatTimeLabel.Text       = s.Time;
            StatSpeedLabel.Text      = s.Speed;
        }

        // ─── Chart data builders ─────────────────────────────────────────────
        private static List<(string, float)> BuildChartData(string period)
        {
            var r    = new Random();
            var data = new List<(string, float)>();

            switch (period)
            {
                case "Today":
                {
                    var start = DateTime.Today;
                    float cum = 0;
                    for (int i = 0; i <= 6; i++)
                    {
                        cum += (float)(r.Next(20, 80) / 10.0);
                        data.Add((start.AddHours(i * 3).ToString("HH:mm"), MathF.Round(cum, 1)));
                    }
                    break;
                }
                case "Week":
                {
                    var start = DateTime.Today.AddDays(-6);
                    float cum = 0;
                    for (int i = 0; i < 7; i++)
                    {
                        cum += (float)(r.Next(10, 60) / 10.0);
                        data.Add((start.AddDays(i).ToString("ddd"), MathF.Round(cum, 1)));
                    }
                    break;
                }
                case "Month":
                {
                    var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    int days  = DateTime.DaysInMonth(start.Year, start.Month);
                    float cum = 0;
                    // ~6 data points spread through the month
                    int step = Math.Max(days / 6, 1);
                    for (int d = 1; d <= days; d += step)
                    {
                        cum += r.Next(20, 80);
                        data.Add((new DateTime(start.Year, start.Month, d).ToString("MMM dd"), cum));
                    }
                    break;
                }
            }
            return data;
        }

        // ─── Tab control ─────────────────────────────────────────────────────
        private void OnTabToday(object? sender, EventArgs e) => SwitchTab("Today");
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

            // Reset all tabs to transparent / gray
            var normalBg    = Colors.Transparent;
            var normalFg    = Application.Current?.RequestedTheme == AppTheme.Dark
                                ? Color.FromArgb("#94A3B8")
                                : Color.FromArgb("#64748B");

            TabToday.BackgroundColor  = normalBg;
            TabWeek.BackgroundColor   = normalBg;
            TabMonth.BackgroundColor  = normalBg;
            TabTodayLabel.TextColor   = normalFg;
            TabWeekLabel.TextColor    = normalFg;
            TabMonthLabel.TextColor   = normalFg;

            // Highlight chosen tab
            switch (period)
            {
                case "Today":
                    TabToday.BackgroundColor    = _accentColor;
                    TabTodayLabel.TextColor     = Colors.White;
                    break;
                case "Week":
                    TabWeek.BackgroundColor     = _accentColor;
                    TabWeekLabel.TextColor      = Colors.White;
                    break;
                case "Month":
                    TabMonth.BackgroundColor    = _accentColor;
                    TabMonthLabel.TextColor     = Colors.White;
                    break;
            }

            // Rebuild chart data off-thread
            _ = Task.Run(() =>
            {
                var data = BuildChartData(period);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _chartData.Clear();
                    _chartData.AddRange(data);
                    ChartCanvas.InvalidateSurface();
                });
            });
        }

        // ─── SkiaSharp chart renderer ─────────────────────────────────────────
        private void OnChartPaint(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            if (_chartData.Count < 2) return;

            var info = e.Info;
            float w = info.Width;
            float h = info.Height;

            float padL = 12f, padR = 12f, padT = 20f, padB = 28f;
            float chartW = w - padL - padR;
            float chartH = h - padT - padB;

            float maxVal = _chartData.Max(d => d.Value);
            if (maxVal <= 0) maxVal = 1;

            // Build point positions
            var pts = new SKPoint[_chartData.Count];
            for (int i = 0; i < _chartData.Count; i++)
            {
                float x = padL + (chartW / (_chartData.Count - 1)) * i;
                float y = padT + chartH - (_chartData[i].Value / maxVal) * chartH;
                pts[i]  = new SKPoint(x, y);
            }

            // Determine accent SKColor
            var hex     = _accentColor.ToHex().TrimStart('#');
            // parse r,g,b from hex (format AARRGGBB or RRGGBB)
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

            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var  gridColor = isDark
                    ? new SKColor(255, 255, 255, 20)
                    : new SKColor(0, 0, 0, 15);

            // 1. Subtle horizontal grid lines
            using var gridPaint = new SKPaint { Color = gridColor, StrokeWidth = 1, IsAntialias = false };
            for (int i = 1; i <= 3; i++)
            {
                float gy = padT + chartH / 4 * i;
                canvas.DrawLine(padL, gy, padL + chartW, gy, gridPaint);
            }

            // 2. Area fill (gradient)
            var path = new SKPath();
            path.MoveTo(pts[0].X, padT + chartH);
            foreach (var p in pts) path.LineTo(p);
            path.LineTo(pts[^1].X, padT + chartH);
            path.Close();

            using var fillShader = SKShader.CreateLinearGradient(
                new SKPoint(0, padT),
                new SKPoint(0, padT + chartH),
                new[] { accent.WithAlpha(100), accent.WithAlpha(5) },
                SKShaderTileMode.Clamp);
            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Shader      = fillShader
            };
            canvas.DrawPath(path, fillPaint);

            // 3. Line
            var linePath = new SKPath();
            linePath.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++)
            {
                // smooth cubic  
                float cx1 = pts[i - 1].X + (pts[i].X - pts[i - 1].X) / 2;
                float cx2 = cx1;
                linePath.CubicTo(cx1, pts[i - 1].Y, cx2, pts[i].Y, pts[i].X, pts[i].Y);
            }

            using var linePaint = new SKPaint
            {
                Color       = accent,
                StrokeWidth = 2.5f,
                IsStroke    = true,
                IsAntialias = true,
                StrokeCap   = SKStrokeCap.Round,
                StrokeJoin  = SKStrokeJoin.Round
            };
            canvas.DrawPath(linePath, linePaint);

            // 4. Dots
            using var dotFill = new SKPaint { Color = accent, IsAntialias = true };
            using var dotRing = new SKPaint { Color = isDark ? new SKColor(30,41,59) : SKColors.White,
                                              IsAntialias = true };
            foreach (var p in pts)
            {
                canvas.DrawCircle(p, 5f, dotRing);
                canvas.DrawCircle(p, 3.5f, dotFill);
            }

            // 5. X labels (using modern SKFont API)
            using var textFont  = new SKFont(SKTypeface.Default, 22f);
            using var textPaint = new SKPaint
            {
                Color       = isDark ? new SKColor(148, 163, 184) : new SKColor(100, 116, 139),
                IsAntialias = true
            };
            for (int i = 0; i < pts.Length; i++)
            {
                // show every other label to avoid crowding
                if (i % 2 != 0 && pts.Length > 4) continue;
                canvas.DrawText(_chartData[i].Label, pts[i].X, h - 4, SKTextAlign.Center, textFont, textPaint);
            }
        }

        // ─── Navigation ──────────────────────────────────────────────────────
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
