using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class AccentColorSelectionPage : ContentPage
{
    private readonly ISettingsService _settings;

    private readonly Dictionary<string, (Border swatch, Label check)> _colorMap;

    public AccentColorSelectionPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;

        _colorMap = new()
        {
            { "#FC5200", (Color1, Check1) },
            { "#2196F3", (Color2, Check2) },
            { "#4CAF50", (Color3, Check3) },
            { "#9C27B0", (Color4, Check4) },
            { "#F44336", (Color5, Check5) },
            { "#FF9800", (Color6, Check6) },
            { "#00BCD4", (Color7, Check7) },
            { "#E91E63", (Color8, Check8) },
            { "#607D8B", (Color9, Check9) },
            { "#795548", (Color10, Check10) },
            { "#3F51B5", (Color11, Check11) },
            { "#009688", (Color12, Check12) },
        };

        var current = _settings.AccentColor;
        UpdateUI(current);
    }

    private void OnColorSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string color)
        {
            _settings.AccentColor = color;
            UpdateUI(color);
        }
    }

    private void UpdateUI(string selectedColor)
    {
        foreach (var kvp in _colorMap)
        {
            kvp.Value.check.IsVisible = kvp.Key == selectedColor;
        }

        if (Color.TryParse(selectedColor, out var color))
        {
            PreviewButton.BackgroundColor = color;
        }
    }
}
