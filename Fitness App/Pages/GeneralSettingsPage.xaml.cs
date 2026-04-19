using Fitness_App.Services;
using Microsoft.Extensions.DependencyInjection;
#if IOS
using StoreKit;
#endif

namespace Fitness_App.Pages;

public partial class GeneralSettingsPage : ContentPage
{
    private readonly ISettingsService _settings;
    private readonly IAppNotificationService _notifications;
    private ISupabaseService? _supabase;
    private bool _isLoadingSettings;

    public GeneralSettingsPage(ISettingsService settings, IAppNotificationService notifications)
    {
        InitializeComponent();
        _settings = settings;
        _notifications = notifications;
        LoadSavedToggles();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSavedToggles();

        // Lazy-resolve so the DI container is ready
        _supabase ??= Handler?.MauiContext?.Services?.GetService<ISupabaseService>()
                   ?? Application.Current?.Handler?.MauiContext?.Services?.GetService<ISupabaseService>();
    }

    private void LoadSavedToggles()
    {
        _isLoadingSettings = true;
        try
        {
            PushNotificationsSwitch.IsToggled = _settings.PushNotificationsEnabled;
            WorkoutRemindersSwitch.IsToggled = _settings.WorkoutRemindersEnabled;
            AchievementAlertsSwitch.IsToggled = _settings.AchievementAlertsEnabled;
            WeeklySummarySwitch.IsToggled = _settings.WeeklySummaryEnabled;
            ScreenReaderSwitch.IsToggled = _settings.ScreenReaderEnabled;
            HighContrastSwitch.IsToggled = _settings.HighContrastEnabled;
            ReduceMotionSwitch.IsToggled = _settings.ReduceMotionEnabled;

            UpdateNotificationSubRows(_settings.PushNotificationsEnabled);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    // ── NOTIFICATIONS ──────────────────────────────────────────────────────────

    private async void OnPushNotificationsToggled(object? sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        _settings.PushNotificationsEnabled = e.Value;
        UpdateNotificationSubRows(e.Value);
        await _notifications.RefreshWorkoutReminderScheduleAsync();
    }

    private void UpdateNotificationSubRows(bool enabled)
    {
        WorkoutRemindersRow.Opacity = enabled ? 1.0 : 0.4;
        WorkoutRemindersRow.InputTransparent = !enabled;
        AchievementAlertsRow.Opacity = enabled ? 1.0 : 0.4;
        AchievementAlertsRow.InputTransparent = !enabled;
        WeeklySummaryRow.Opacity = enabled ? 1.0 : 0.4;
        WeeklySummaryRow.InputTransparent = !enabled;
    }

    private async void OnWorkoutRemindersToggled(object? sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        _settings.WorkoutRemindersEnabled = e.Value;
        await _notifications.RefreshWorkoutReminderScheduleAsync();
    }

    private void OnAchievementAlertsToggled(object? sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        _settings.AchievementAlertsEnabled = e.Value;
    }

    private void OnWeeklySummaryToggled(object? sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        _settings.WeeklySummaryEnabled = e.Value;
    }

    private async void OnWorkoutRemindersDetail(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("workoutreminders");
    }

    // ── APPEARANCE ─────────────────────────────────────────────────────────────

    private async void OnThemeTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("themeselection");
    }

    private async void OnAccentColorTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("accentcolor");
    }

    private async void OnFontSizeTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("fontsize");
    }

    // ── LANGUAGE & REGION ──────────────────────────────────────────────────────

    private async void OnLanguageTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("language");
    }

    private async void OnUnitsTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("units");
    }

    private async void OnDateTimeTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("datetime");
    }

    // ── PRIVACY & SECURITY ─────────────────────────────────────────────────────

    private async void OnChangePasswordTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("changepassword");
    }

    private async void OnTwoFactorTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("twofactor");
    }

    private async void OnDataSharingTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("datasharing");
    }

    private async void OnDownloadDataTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("downloaddata");
    }

    // ── ACCESSIBILITY ──────────────────────────────────────────────────────────

    private void OnScreenReaderToggled(object? sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        _settings.ScreenReaderEnabled = e.Value;
    }

    private void OnHighContrastToggled(object? sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        _settings.HighContrastEnabled = e.Value;
    }

    private void OnReduceMotionToggled(object? sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        _settings.ReduceMotionEnabled = e.Value;
    }

    // ── CONNECTED SERVICES ─────────────────────────────────────────────────────

    private async void OnLinkedAppsTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("linkedapps");
    }

    private async void OnWearableDevicesTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("wearabledevices");
    }

    // ── ABOUT & SUPPORT ────────────────────────────────────────────────────────

    private async void OnAppVersionLongPress(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
        await DisplayAlert("Build Info", "Build 42 · Production", "OK");
    }

    private async void OnHelpFaqTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("helpfaq");
    }

    private async void OnSendFeedbackTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("sendfeedback");
    }

    private async void OnRateTheAppTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
#if IOS
        SKStoreReviewController.RequestReview();
#elif ANDROID
        await DisplayAlert("Rate the App", "This would trigger the Google Play In-App Review dialog.", "OK");
#else
        await DisplayAlert("Rate the App", "Thank you for your support!", "OK");
#endif
    }

    private async void OnTermsOfServiceTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("termsofservice");
    }

    private async void OnPrivacyPolicyTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("privacypolicy");
    }

    // ── ACCOUNT ────────────────────────────────────────────────────────────────

    private async void OnSwitchAccountTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Shell.Current.GoToAsync("switchaccount");
    }

    private async void OnDeleteAccountClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var step1 = await DisplayAlert("Are you sure?",
            "This will permanently delete your account, all activities, and routes. This cannot be undone.",
            "Continue", "Cancel");

        if (!step1) return;

        var password = await DisplayPromptAsync(
            "Confirm your password to proceed",
            "Enter your current password:",
            keyboard: Keyboard.Default);

        if (string.IsNullOrEmpty(password)) return;

        // In production: call Supabase to delete user data and auth record
        await DisplayAlert("Account Deleted", "Your account has been permanently deleted.", "OK");
        // Navigate to onboarding/login
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var confirm = await DisplayAlert("Log Out",
            "Are you sure you want to log out?",
            "Log Out", "Cancel");

        if (!confirm) return;

        try
        {
            // Sign out from Supabase (clears the remote session token)
            if (_supabase != null)
                await _supabase.SignOutAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GeneralSettings] SignOut error: {ex.Message}");
        }
        finally
        {
            // Always clear local cached credentials so the app doesn't auto-login
            Preferences.Default.Remove("supabase_session");
            Preferences.Default.Remove("last_selected_sport");

            // Navigate back to the login shell route
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync("//login");
            });
        }
    }
}
