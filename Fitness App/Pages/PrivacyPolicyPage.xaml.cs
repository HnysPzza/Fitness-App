namespace Fitness_App.Pages;

public partial class PrivacyPolicyPage : ContentPage
{
    public PrivacyPolicyPage()
    {
        InitializeComponent();
    }

    private async void OnOpenInBrowser(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        try
        {
            await Browser.Default.OpenAsync("https://fitnessapp.example.com/privacy", BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            await DisplayAlert("Error", "Could not open the browser.", "OK");
        }
    }
}
