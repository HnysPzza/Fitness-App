namespace Fitness_App.Pages;

public partial class LinkedAppsPage : ContentPage
{
    public LinkedAppsPage()
    {
        InitializeComponent();
    }

    private async void OnUnlinkAppleHealth(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        var confirm = await DisplayAlert("Unlink Apple Health",
            "Are you sure you want to unlink Apple Health? This will stop syncing data.",
            "Unlink", "Cancel");
        if (confirm)
        {
            await DisplayAlert("Unlinked", "Apple Health has been unlinked.", "OK");
        }
    }

    private async void OnLinkApp(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (sender is Button btn && btn.CommandParameter is string appName)
        {
            await DisplayAlert($"Link {appName}",
                $"In a production app, this would start the OAuth flow to connect {appName}.",
                "OK");
        }
    }
}
