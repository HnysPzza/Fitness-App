using System.Text.RegularExpressions;

namespace Fitness_App.Pages;

public partial class ChangePasswordPage : ContentPage
{
    private bool _showCurrent = false;
    private bool _showNew = false;
    private bool _showConfirm = false;

    public ChangePasswordPage()
    {
        InitializeComponent();
    }

    private void OnToggleCurrentPassword(object? sender, EventArgs e)
    {
        _showCurrent = !_showCurrent;
        CurrentPasswordEntry.IsPassword = !_showCurrent;
    }

    private void OnToggleNewPassword(object? sender, EventArgs e)
    {
        _showNew = !_showNew;
        NewPasswordEntry.IsPassword = !_showNew;
    }

    private void OnToggleConfirmPassword(object? sender, EventArgs e)
    {
        _showConfirm = !_showConfirm;
        ConfirmPasswordEntry.IsPassword = !_showConfirm;
    }

    private void OnPasswordTextChanged(object? sender, TextChangedEventArgs e)
    {
        var newPwd = NewPasswordEntry.Text ?? string.Empty;

        bool has8 = newPwd.Length >= 8;
        bool hasUpper = Regex.IsMatch(newPwd, "[A-Z]");
        bool hasNumber = Regex.IsMatch(newPwd, "[0-9]");
        bool hasSpecial = Regex.IsMatch(newPwd, @"[!@#$%^&*(),.?\"":{}|<>]");

        SetCheckUI(Check8Chars, has8);
        SetCheckUI(CheckUppercase, hasUpper);
        SetCheckUI(CheckNumber, hasNumber);
        SetCheckUI(CheckSpecial, hasSpecial);

        bool allValid = has8 && hasUpper && hasNumber && hasSpecial;
        bool confirmsMatch = !string.IsNullOrEmpty(ConfirmPasswordEntry.Text)
            && ConfirmPasswordEntry.Text == newPwd;
        bool currentFilled = !string.IsNullOrEmpty(CurrentPasswordEntry.Text);

        bool canUpdate = allValid && confirmsMatch && currentFilled;
        UpdateButton.IsEnabled = canUpdate;
        UpdateButton.Opacity = canUpdate ? 1.0 : 0.5;
    }

    private void SetCheckUI(Label label, bool passed)
    {
        label.Text = passed ? "●" : "○";
        label.TextColor = passed ? Color.FromArgb("#4CAF50") : Color.FromArgb("#BDBDBD");
    }

    private async void OnUpdatePasswordClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        // In a real app you'd call the auth service here
        await DisplayAlert("Success", "Password updated successfully.", "OK");

        // Clear fields
        CurrentPasswordEntry.Text = string.Empty;
        NewPasswordEntry.Text = string.Empty;
        ConfirmPasswordEntry.Text = string.Empty;

        await Navigation.PopAsync();
    }
}
