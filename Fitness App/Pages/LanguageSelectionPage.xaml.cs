using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class LanguageSelectionPage : ContentPage
{
    private readonly ISettingsService _settings;
    private string _selectedLanguage;

    private readonly List<string> _allLanguages = new()
    {
        "English", "Filipino", "Spanish", "French", "Japanese",
        "Korean", "Chinese (Simplified)", "German", "Italian",
        "Portuguese", "Arabic", "Hindi", "Thai", "Vietnamese",
        "Indonesian", "Dutch", "Russian", "Turkish"
    };

    public LanguageSelectionPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _selectedLanguage = _settings.Language;
        LanguageList.ItemsSource = _allLanguages;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrEmpty(query))
            LanguageList.ItemsSource = _allLanguages;
        else
            LanguageList.ItemsSource = _allLanguages
                .Where(l => l.ToLowerInvariant().Contains(query))
                .ToList();
    }

    private async void OnLanguageSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string language && language != _selectedLanguage)
        {
            var confirm = await DisplayAlert(
                "Change Language",
                "Restart required to apply language change. Continue?",
                "Continue",
                "Cancel");

            if (confirm)
            {
                _selectedLanguage = language;
                _settings.Language = language;
                await DisplayAlert("Language Changed", $"Language set to {language}. Please restart the app.", "OK");
            }
        }
    }
}
