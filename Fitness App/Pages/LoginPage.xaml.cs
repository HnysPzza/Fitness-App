using Fitness_App.ViewModels;

namespace Fitness_App.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /// <summary>
    /// Google Sign-In is a P1 task that requires Google Cloud Console
    /// configuration. For now, show a placeholder message.
    /// </summary>
    private async void OnGoogleSignInTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await DisplayAlert(
            "Google Sign-In",
            "Google Sign-In requires additional setup in the Google Cloud Console " +
            "and Supabase Dashboard. Please configure your OAuth credentials first.",
            "OK");
    }

    private async void OnSignUpTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("//register");
    }
}
