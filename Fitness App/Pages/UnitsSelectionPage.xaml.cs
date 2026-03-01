using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class UnitsSelectionPage : ContentPage
{
    private readonly ISettingsService _settings;

    public UnitsSelectionPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        UpdateDistanceUI(_settings.DistanceUnit);
        UpdateWeightUI(_settings.WeightUnit);
        UpdateElevationUI(_settings.ElevationUnit);
    }

    private void OnDistanceSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string unit)
        {
            _settings.DistanceUnit = unit;
            UpdateDistanceUI(unit);
        }
    }

    private void OnWeightSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string unit)
        {
            _settings.WeightUnit = unit;
            UpdateWeightUI(unit);
        }
    }

    private void OnElevationSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string unit)
        {
            _settings.ElevationUnit = unit;
            UpdateElevationUI(unit);
        }
    }

    private void UpdateDistanceUI(string unit)
    {
        KmCheck.IsVisible = unit == "Kilometers (km)";
        KmUncheck.IsVisible = unit != "Kilometers (km)";
        MiCheck.IsVisible = unit == "Miles (mi)";
        MiUncheck.IsVisible = unit != "Miles (mi)";
    }

    private void UpdateWeightUI(string unit)
    {
        KgCheck.IsVisible = unit == "Kilograms (kg)";
        KgUncheck.IsVisible = unit != "Kilograms (kg)";
        LbsCheck.IsVisible = unit == "Pounds (lbs)";
        LbsUncheck.IsVisible = unit != "Pounds (lbs)";
    }

    private void UpdateElevationUI(string unit)
    {
        MCheck.IsVisible = unit == "Meters (m)";
        MUncheck.IsVisible = unit != "Meters (m)";
        FtCheck.IsVisible = unit == "Feet (ft)";
        FtUncheck.IsVisible = unit != "Feet (ft)";
    }
}
