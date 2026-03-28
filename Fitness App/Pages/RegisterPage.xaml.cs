using Fitness_App.ViewModels;

namespace Fitness_App.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnGoogleSignUpTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await DisplayAlert(
            "Google Sign-Up",
            "Google Sign-In requires additional setup in the Google Cloud Console " +
            "and Supabase Dashboard. Please configure your OAuth credentials first.",
            "OK");
    }
}
