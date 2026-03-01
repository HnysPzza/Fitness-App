using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fitness_App.Models;
using Syncfusion.Maui.Charts;

namespace Fitness_App.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    public ObservableCollection<FitnessData> Points { get; } = new();

    [ObservableProperty]
    private ChartAxis xAxis;

    [ObservableProperty]
    private ChartAxis yAxis;

    [ObservableProperty]
    private string selectedView = "Daily";

    public IRelayCommand<string> SwitchViewCommand { get; }

    public ProgressViewModel()
    {
        XAxis = CreateDailyAxis();
        YAxis = CreateDefaultYAxis();

        SwitchViewCommand = new RelayCommand<string>(SwitchView);

        LoadDaily(animated: true);
    }

    private void SwitchView(string? view)
    {
        if (string.IsNullOrWhiteSpace(view))
            return;

        SelectedView = view;

        switch (view)
        {
            case "Daily":
                XAxis = CreateDailyAxis();
                LoadDaily(animated: false);
                break;
            case "Weekly":
                XAxis = CreateWeeklyAxis();
                LoadWeekly(animated: false);
                break;
            case "Monthly":
                XAxis = CreateMonthlyAxis();
                LoadMonthly(animated: false);
                break;
        }
    }

    private static NumericalAxis CreateDefaultYAxis()
    {
        return new NumericalAxis
        {
            IsVisible = false
        };
    }

    private static DateTimeAxis CreateDailyAxis()
    {
        return new DateTimeAxis
        {
            IntervalType = DateTimeIntervalType.Hours,
            Interval = 2,
            IsVisible = true,
            LabelStyle = new ChartAxisLabelStyle { LabelFormat = "HH:mm" },
            MajorGridLineStyle = new ChartLineStyle { StrokeWidth = 0 },
            AxisLineStyle = new ChartLineStyle { StrokeWidth = 0 },
            MajorTickStyle = new ChartAxisTickStyle { StrokeWidth = 0 }
        };
    }

    private static DateTimeAxis CreateWeeklyAxis()
    {
        return new DateTimeAxis
        {
            IntervalType = DateTimeIntervalType.Days,
            Interval = 1,
            IsVisible = true,
            LabelStyle = new ChartAxisLabelStyle { LabelFormat = "ddd" },
            MajorGridLineStyle = new ChartLineStyle { StrokeWidth = 0 },
            AxisLineStyle = new ChartLineStyle { StrokeWidth = 0 },
            MajorTickStyle = new ChartAxisTickStyle { StrokeWidth = 0 }
        };
    }

    private static DateTimeAxis CreateMonthlyAxis()
    {
        return new DateTimeAxis
        {
            IntervalType = DateTimeIntervalType.Days,
            Interval = 5,
            IsVisible = true,
            LabelStyle = new ChartAxisLabelStyle { LabelFormat = "MMM dd" },
            MajorGridLineStyle = new ChartLineStyle { StrokeWidth = 0 },
            AxisLineStyle = new ChartLineStyle { StrokeWidth = 0 },
            MajorTickStyle = new ChartAxisTickStyle { StrokeWidth = 0 }
        };
    }

    private void LoadDaily(bool animated)
    {
        Points.Clear();

        var start = DateTime.Today;
        for (var i = 0; i < 12; i++)
        {
            Points.Add(new FitnessData
            {
                Time = start.AddMinutes(i * 60),
                Value = 2 + Math.Sin(i * 0.6) * 1.2 + (i * 0.15)
            });
        }

        EnableInitialAnimation = animated;
    }

    private void LoadWeekly(bool animated)
    {
        Points.Clear();

        var start = DateTime.Today.AddDays(-6);
        for (var i = 0; i < 7; i++)
        {
            Points.Add(new FitnessData
            {
                Time = start.AddDays(i),
                Value = 3 + Math.Abs(Math.Sin(i)) * 4
            });
        }

        EnableInitialAnimation = animated;
    }

    private void LoadMonthly(bool animated)
    {
        Points.Clear();

        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var days = DateTime.DaysInMonth(start.Year, start.Month);

        for (var d = 1; d <= days; d += 2)
        {
            Points.Add(new FitnessData
            {
                Time = new DateTime(start.Year, start.Month, d),
                Value = 2 + (d * 0.12) + (Math.Sin(d * 0.35) * 1.5)
            });
        }

        EnableInitialAnimation = animated;
    }

    [ObservableProperty]
    private bool enableInitialAnimation;
}
