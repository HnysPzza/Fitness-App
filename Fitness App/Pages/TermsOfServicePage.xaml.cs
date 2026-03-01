namespace Fitness_App.Pages;

public partial class TermsOfServicePage : ContentPage
{
    public TermsOfServicePage()
    {
        InitializeComponent();
    }

    private async void OnOpenInBrowser(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        try
        {
            await Browser.Default.OpenAsync("https://fitnessapp.example.com/terms", BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            await DisplayAlert("Error", "Could not open the browser.", "OK");
        }
    }
}
