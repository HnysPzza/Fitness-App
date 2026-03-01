using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class DataSharingPage : ContentPage
{
    private readonly ISettingsService _settings;

    public DataSharingPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        AnalyticsSwitch.IsToggled = _settings.ShareAnalytics;
        LocationSwitch.IsToggled = _settings.LocationSharing;
        ActivityVisibilityLabel.Text = _settings.ActivityVisibility;
        ProfileVisibilityLabel.Text = _settings.ProfileVisibility;
    }

    private void OnAnalyticsToggled(object? sender, ToggledEventArgs e)
    {
        _settings.ShareAnalytics = e.Value;
    }

    private void OnLocationToggled(object? sender, ToggledEventArgs e)
    {
        _settings.LocationSharing = e.Value;
    }

    private async void OnActivityVisibilityTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        var result = await DisplayActionSheet("Activity Visibility", "Cancel", null,
            "Everyone", "Friends Only", "Only Me");
        if (result is not null && result != "Cancel")
        {
            _settings.ActivityVisibility = result;
            ActivityVisibilityLabel.Text = result;
        }
    }

    private async void OnProfileVisibilityTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        var result = await DisplayActionSheet("Profile Visibility", "Cancel", null,
            "Everyone", "Friends Only", "Only Me");
        if (result is not null && result != "Cancel")
        {
            _settings.ProfileVisibility = result;
            ProfileVisibilityLabel.Text = result;
        }
    }
}
