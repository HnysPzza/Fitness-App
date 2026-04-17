using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class WorkoutRemindersPage : ContentPage
{
    private readonly ISettingsService _settings;
    private readonly IAppNotificationService _notifications;
    private List<string> _selectedDays;

    public WorkoutRemindersPage(ISettingsService settings, IAppNotificationService notifications)
    {
        InitializeComponent();
        _settings = settings;
        _notifications = notifications;

        EnableSwitch.IsToggled = _settings.WorkoutRemindersEnabled;

        var time = _settings.WorkoutReminderTime;
        var dt = DateTime.Today.Add(time);
        TimeLabel.Text = dt.ToString("h:mm tt");

        _selectedDays = _settings.WorkoutReminderDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        DaysLabel.Text = string.Join(", ", _selectedDays);

        MessageLabel.Text = _settings.WorkoutReminderMessage;

        UpdateContentEnabled(_settings.WorkoutRemindersEnabled);
    }

    private async void OnEnableToggled(object? sender, ToggledEventArgs e)
    {
        _settings.WorkoutRemindersEnabled = e.Value;
        UpdateContentEnabled(e.Value);
        await _notifications.RefreshWorkoutReminderScheduleAsync();
    }

    private void UpdateContentEnabled(bool enabled)
    {
        RemindersContent.Opacity = enabled ? 1.0 : 0.4;
        RemindersContent.InputTransparent = !enabled;
    }

    private async void OnTimeTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        // Show a simple time selection via action sheet (native TimePicker not available inline in MAUI)
        var currentTime = _settings.WorkoutReminderTime;
        var hours = new[] { "5 AM","6 AM","7 AM","8 AM","9 AM","10 AM","11 AM","12 PM","1 PM","2 PM","3 PM","4 PM","5 PM","6 PM","7 PM","8 PM","9 PM","10 PM" };
        var result = await DisplayActionSheet("Select Reminder Time", "Cancel", null, hours);
        if (result is not null && result != "Cancel")
        {
            TimeLabel.Text = result;
            // map back to a timespan
            if (DateTime.TryParse(result.Replace("AM", " AM").Replace("PM", " PM"), out var dt))
            {
                _settings.WorkoutReminderTime = dt.TimeOfDay;
                await _notifications.RefreshWorkoutReminderScheduleAsync();
            }
        }
    }

    private async void OnDaysTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await ShowDayPickerDialog();
    }

    private async Task ShowDayPickerDialog()
    {
        // More user-friendly: show checkboxes by presenting as comma-list prompt
        var result = await DisplayActionSheet("Which days?", "Cancel", "Done",
            "Mon, Wed, Fri (3x/week)",
            "Mon, Tue, Wed, Thu, Fri (Weekdays)",
            "Sat, Sun (Weekends)",
            "Mon, Tue, Wed, Thu, Fri, Sat, Sun (Every day)");

        if (result is null || result == "Cancel" || result == "Done") return;

        var dayStr = result.Split('(')[0].Trim();
        _selectedDays = dayStr.Split(',', StringSplitOptions.TrimEntries).ToList();
        _settings.WorkoutReminderDays = string.Join(",", _selectedDays);
        DaysLabel.Text = dayStr;
        await _notifications.RefreshWorkoutReminderScheduleAsync();
    }

    private async void OnMessageTapped(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        var result = await DisplayPromptAsync(
            "Reminder Message",
            "Enter your reminder message (max 60 characters):",
            maxLength: 60,
            initialValue: _settings.WorkoutReminderMessage);

        if (result is not null)
        {
            _settings.WorkoutReminderMessage = result;
            MessageLabel.Text = result;
            await _notifications.RefreshWorkoutReminderScheduleAsync();
        }
    }
}
