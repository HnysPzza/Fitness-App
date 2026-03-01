using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class TwoFactorAuthPage : ContentPage
{
    private readonly ISettingsService _settings;

    public TwoFactorAuthPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        UpdateView(_settings.TwoFactorEnabled);
    }

    private void UpdateView(bool isEnabled)
    {
        DisabledView.IsVisible = !isEnabled;
        EnabledView.IsVisible = isEnabled;
    }

    private async void OnEnable2FA(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await DisplayAlert("Set Up 2FA", "In a production app, this would launch an authenticator setup flow (QR code scan). 2FA enabled for demonstration.", "OK");
        _settings.TwoFactorEnabled = true;
        UpdateView(true);
    }

    private async void OnDisable2FA(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        var confirm = await DisplayAlert("Disable 2FA",
            "Are you sure you want to disable Two-Factor Authentication? This will make your account less secure.",
            "Disable", "Cancel");
        if (confirm)
        {
            _settings.TwoFactorEnabled = false;
            UpdateView(false);
        }
    }

    private async void OnViewRecoveryCodes(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await DisplayAlert("Recovery Codes",
            "ABCD-1234\nEFGH-5678\nIJKL-9012\nMNOP-3456\nQRST-7890\n\nStore these codes in a safe place. Each can only be used once.",
            "OK");
    }
}
