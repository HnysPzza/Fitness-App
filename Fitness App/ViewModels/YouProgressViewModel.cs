using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fitness_App.Models;
using Syncfusion.Maui.Charts;

namespace Fitness_App.ViewModels;

public partial class YouProgressViewModel : ObservableObject
{
    public ObservableCollection<FitnessData> Points { get; } = new();

    [ObservableProperty]
    private DateTimeIntervalType xAxisIntervalType;

    [ObservableProperty]
    private double xAxisInterval;

    [ObservableProperty]
    private string xAxisLabelFormat = string.Empty;

    [ObservableProperty]
    private double yAxisMaximum;

    [ObservableProperty]
    private bool yAxisIsVisible = false;

    [ObservableProperty]
    private string selectedView = "Today";

    [ObservableProperty]
    private Color accentColor = Color.FromArgb("#FC5200");

    [ObservableProperty]
    private Brush areaFill = new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#66FC5200"), 0),
            new GradientStop(Color.FromArgb("#00FC5200"), 1)
        },
        new Point(0, 0),
        new Point(0, 1));

    [ObservableProperty]
    private bool enableInitialAnimation;

    public IRelayCommand<string> SwitchViewCommand { get; }

    public YouProgressViewModel()
    {
        SwitchViewCommand = new RelayCommand<string>(SwitchView);
        LoadToday(animated: true);
    }

    private void SwitchView(string? view)
    {
        if (string.IsNullOrWhiteSpace(view))
            return;

        SelectedView = view;

        switch (view)
        {
            case "Today":
                AccentColor = Color.FromArgb("#FC5200");
                AreaFill = CreateFill("#FC5200");
                LoadToday(animated: false);
                break;
            case "Week":
                AccentColor = Color.FromArgb("#10B981");
                AreaFill = CreateFill("#10B981");
                LoadWeek(animated: false);
                break;
            case "Month":
                AccentColor = Color.FromArgb("#2563EB");
                AreaFill = CreateFill("#2563EB");
                LoadMonth(animated: false);
                break;
        }
    }

    private static Brush CreateFill(string hex)
    {
        var top = Color.FromArgb($"66{hex.TrimStart('#')}");
        var bottom = Color.FromArgb($"00{hex.TrimStart('#')}");

        return new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(top, 0),
                new GradientStop(bottom, 1)
            },
            new Point(0, 0),
            new Point(0, 1));
    }

    private void LoadToday(bool animated)
    {
        Points.Clear();
        var start = DateTime.Today;
        var r = new Random();
        double val = 0;

        for (var i = 0; i < 6; i++)
        {
            val += r.Next(100, 500) / 100.0;
            Points.Add(new FitnessData
            {
                Time = start.AddHours(i * 4),
                Value = Math.Round(val, 2)
            });
        }

        XAxisIntervalType = DateTimeIntervalType.Hours;
        XAxisInterval = 4;
        XAxisLabelFormat = "HH:mm";
        YAxisMaximum = val > 0 ? val * 1.2 : 10;
        EnableInitialAnimation = animated;
    }

    private void LoadWeek(bool animated)
    {
        Points.Clear();
        var start = DateTime.Today.AddDays(-6);
        var r = new Random();
        double val = 0;

        for (var i = 0; i < 7; i++)
        {
            val += r.Next(20, 80) / 10.0;
            Points.Add(new FitnessData
            {
                Time = start.AddDays(i),
                Value = Math.Round(val, 2)
            });
        }

        XAxisIntervalType = DateTimeIntervalType.Days;
        XAxisInterval = 1;
        XAxisLabelFormat = "ddd";
        YAxisMaximum = val > 0 ? val * 1.2 : 50;
        EnableInitialAnimation = animated;
    }

    private void LoadMonth(bool animated)
    {
        Points.Clear();
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var days = DateTime.DaysInMonth(start.Year, start.Month);
        var r = new Random();
        double val = 0;

        for (var d = 1; d <= days; d += 5)
        {
            val += r.Next(15, 60);
            Points.Add(new FitnessData
            {
                Time = new DateTime(start.Year, start.Month, d),
                Value = val
            });
        }
        
        var lastTime = Points.LastOrDefault()?.Time;
        if (lastTime.HasValue && lastTime.Value.Day != days)
        {
            val += r.Next(5, 15);
            Points.Add(new FitnessData
            {
                Time = new DateTime(start.Year, start.Month, days),
                Value = val
            });
        }

        XAxisIntervalType = DateTimeIntervalType.Days;
        XAxisInterval = 7;
        XAxisLabelFormat = "MMM dd";
        YAxisMaximum = val > 0 ? val * 1.2 : 1500;
        EnableInitialAnimation = animated;
    }
}
