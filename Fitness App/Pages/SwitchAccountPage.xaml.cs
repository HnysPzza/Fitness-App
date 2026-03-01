namespace Fitness_App.Pages;

public partial class SwitchAccountPage : ContentPage
{
    public SwitchAccountPage()
    {
        InitializeComponent();
    }

    private async void OnSwitchAccount(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string email)
        {
            var confirm = await DisplayAlert("Switch Account",
                $"Switch to {email}?", "Switch", "Cancel");
            if (confirm)
            {
                await DisplayAlert("Switched", $"Now signed in as {email}.", "OK");
            }
        }
    }

    private async void OnAddAccount(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await DisplayAlert("Add Account",
            "In a production app, this would launch Google OAuth sign-in for a new account.",
            "OK");
    }
}
