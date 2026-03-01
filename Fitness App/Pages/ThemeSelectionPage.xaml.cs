using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class ThemeSelectionPage : ContentPage
{
    private readonly ISettingsService _settings;
    private string _selectedTheme;

    public ThemeSelectionPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _selectedTheme = _settings.AppThemePreference;
        UpdateUI(_selectedTheme);
    }

    private void OnThemeSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string theme)
        {
            _selectedTheme = theme;
            _settings.AppThemePreference = theme;
            UpdateUI(theme);
            ApplyTheme(theme);
        }
    }

    private void UpdateUI(string theme)
    {
        LightCheck.IsVisible = theme == "Light";
        LightUncheck.IsVisible = theme != "Light";
        DarkCheck.IsVisible = theme == "Dark";
        DarkUncheck.IsVisible = theme != "Dark";
        SystemCheck.IsVisible = theme == "System Default";
        SystemUncheck.IsVisible = theme != "System Default";
    }

    private void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;
        Application.Current.UserAppTheme = theme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
