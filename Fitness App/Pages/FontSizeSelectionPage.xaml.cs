using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class FontSizeSelectionPage : ContentPage
{
    private readonly ISettingsService _settings;
    private bool _suppressSliderEvent = false;

    private readonly string[] _sizes = { "Small", "Medium", "Large", "Extra Large" };
    private readonly double[] _fontSizes = { 12.0, 16.0, 20.0, 24.0 };

    public FontSizeSelectionPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;

        var currentSize = _settings.FontSizePreference;
        var idx = Array.IndexOf(_sizes, currentSize);
        if (idx < 0) idx = 1; // default Medium

        _suppressSliderEvent = true;
        FontSlider.Value = idx;
        _suppressSliderEvent = false;

        UpdateUI(currentSize);
    }

    private void OnFontSizeSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string size)
        {
            _settings.FontSizePreference = size;

            var idx = Array.IndexOf(_sizes, size);
            _suppressSliderEvent = true;
            FontSlider.Value = idx >= 0 ? idx : 1;
            _suppressSliderEvent = false;

            UpdateUI(size);
        }
    }

    private void OnSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_suppressSliderEvent) return;

        int idx = (int)Math.Round(e.NewValue);
        idx = Math.Clamp(idx, 0, _sizes.Length - 1);
        var size = _sizes[idx];
        _settings.FontSizePreference = size;
        UpdateUI(size);
    }

    private void UpdateUI(string size)
    {
        SmallCheck.IsVisible = size == "Small";
        SmallUncheck.IsVisible = size != "Small";
        MediumCheck.IsVisible = size == "Medium";
        MediumUncheck.IsVisible = size != "Medium";
        LargeCheck.IsVisible = size == "Large";
        LargeUncheck.IsVisible = size != "Large";
        ExtraLargeCheck.IsVisible = size == "Extra Large";
        ExtraLargeUncheck.IsVisible = size != "Extra Large";

        var idx = Array.IndexOf(_sizes, size);
        if (idx >= 0)
            PreviewLabel.FontSize = _fontSizes[idx];
    }
}
