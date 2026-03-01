using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class SettingsPage : ContentPage
{
    private IProfileService? _profileService;

    public SettingsPage()
    {
        InitializeComponent();
        InitializeSettings();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();

        if (_profileService != null)
            _profileService.ProfileChanged += OnProfileChanged;

        LoadProfile();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();
        LoadProfile();
    }

    private IProfileService? GetProfileService()
    {
        _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();
        return _profileService;
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(LoadProfile);
    }

    private void LoadProfile()
    {
        var profile = GetProfileService();
        if (profile == null) return;

        // Update name label
        SettingsUserNameLabel.Text = profile.FullName;

        // Update profile photo
        var photoPath = profile.ProfilePhotoPath;
        if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
        {
            ProfileIcon.IsVisible = false;
            ProfileIconContainer.IsVisible = false;
            ProfileImage.IsVisible = true;
            ProfileImage.Source = ImageSource.FromStream(() => File.OpenRead(photoPath));
        }
        else
        {
            ProfileIcon.IsVisible = true;
            ProfileIconContainer.IsVisible = true;
            ProfileImage.IsVisible = false;
        }
    }

    private void InitializeSettings()
    {
        // Set dark mode switch based on current theme
        var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
        if (currentTheme == AppTheme.Unspecified)
        {
            currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
        }

        DarkModeSwitch.IsToggled = currentTheme == AppTheme.Dark;
        UpdateThemeUI(currentTheme == AppTheme.Dark);
    }

    private async void OnEditProfileClicked(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        try
        {
            await Shell.Current.Navigation.PopAsync();
            await Shell.Current.GoToAsync("editprofile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private void OnDarkModeToggled(object? sender, ToggledEventArgs e)
    {
        var isDark = e.Value;
        Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
        UpdateThemeUI(isDark);

        // Persist theme preference
        Preferences.Default.Set("dark_mode", isDark);
    }

    private void UpdateThemeUI(bool isDark)
    {
        if (isDark)
        {
            ThemeIcon.Text = UI.Icons.MaterialSymbols.Dark_mode;
            ThemeStatusLabel.Text = "Dark theme enabled";
        }
        else
        {
            ThemeIcon.Text = UI.Icons.MaterialSymbols.Light_mode;
            ThemeStatusLabel.Text = "Light theme enabled";
        }
    }

    private void OnDailyGoalChanged(object? sender, ValueChangedEventArgs e)
    {
        var goal = (int)e.NewValue;
        DailyGoalLabel.Text = $"{goal} km per day";

        // Persist daily goal
        Preferences.Default.Set("daily_goal", goal);
    }

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        var result = await DisplayAlert(
            "Clear Activity History",
            "Are you sure you want to delete all your activity history? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!result)
            return;

        // TODO: Clear activity history from database

        await DisplayAlert(
            "History Cleared",
            "Your activity history has been successfully deleted.",
            "OK");
    }
}
