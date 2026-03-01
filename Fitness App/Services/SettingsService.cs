namespace Fitness_App.Services
{
    public interface ISettingsService
    {
        // Notifications
        bool PushNotificationsEnabled { get; set; }
        bool WorkoutRemindersEnabled { get; set; }
        bool AchievementAlertsEnabled { get; set; }
        bool WeeklySummaryEnabled { get; set; }
        TimeSpan WorkoutReminderTime { get; set; }
        string WorkoutReminderMessage { get; set; }
        string WorkoutReminderDays { get; set; }

        // Appearance
        string AppThemePreference { get; set; }
        string AccentColor { get; set; }
        string FontSizePreference { get; set; }

        // Language & Region
        string Language { get; set; }
        string DistanceUnit { get; set; }
        string WeightUnit { get; set; }
        string ElevationUnit { get; set; }
        string DateFormat { get; set; }
        string TimeFormat { get; set; }

        // Privacy & Security
        bool TwoFactorEnabled { get; set; }
        bool ShareAnalytics { get; set; }
        string ActivityVisibility { get; set; }
        string ProfileVisibility { get; set; }
        bool LocationSharing { get; set; }

        // Accessibility
        bool ScreenReaderEnabled { get; set; }
        bool HighContrastEnabled { get; set; }
        bool ReduceMotionEnabled { get; set; }
    }

    public class SettingsService : ISettingsService
    {
        // Notifications
        public bool PushNotificationsEnabled
        {
            get => Preferences.Default.Get("push_notifications", true);
            set => Preferences.Default.Set("push_notifications", value);
        }

        public bool WorkoutRemindersEnabled
        {
            get => Preferences.Default.Get("workout_reminders", true);
            set => Preferences.Default.Set("workout_reminders", value);
        }

        public bool AchievementAlertsEnabled
        {
            get => Preferences.Default.Get("achievement_alerts", true);
            set => Preferences.Default.Set("achievement_alerts", value);
        }

        public bool WeeklySummaryEnabled
        {
            get => Preferences.Default.Get("weekly_summary", true);
            set => Preferences.Default.Set("weekly_summary", value);
        }

        public TimeSpan WorkoutReminderTime
        {
            get
            {
                var str = Preferences.Default.Get("reminder_time", "07:00:00");
                return TimeSpan.TryParse(str, out var ts) ? ts : new TimeSpan(7, 0, 0);
            }
            set => Preferences.Default.Set("reminder_time", value.ToString());
        }

        public string WorkoutReminderMessage
        {
            get => Preferences.Default.Get("reminder_message", "Time to get moving!");
            set => Preferences.Default.Set("reminder_message", value);
        }

        public string WorkoutReminderDays
        {
            get => Preferences.Default.Get("reminder_days", "Mon,Wed,Fri");
            set => Preferences.Default.Set("reminder_days", value);
        }

        // Appearance
        public string AppThemePreference
        {
            get => Preferences.Default.Get("app_theme", "System Default");
            set => Preferences.Default.Set("app_theme", value);
        }

        public string AccentColor
        {
            get => Preferences.Default.Get("accent_color", "#FC5200");
            set => Preferences.Default.Set("accent_color", value);
        }

        public string FontSizePreference
        {
            get => Preferences.Default.Get("font_size", "Medium");
            set => Preferences.Default.Set("font_size", value);
        }

        // Language & Region
        public string Language
        {
            get => Preferences.Default.Get("language", "English");
            set => Preferences.Default.Set("language", value);
        }

        public string DistanceUnit
        {
            get => Preferences.Default.Get("distance_unit", "Kilometers (km)");
            set => Preferences.Default.Set("distance_unit", value);
        }

        public string WeightUnit
        {
            get => Preferences.Default.Get("weight_unit", "Kilograms (kg)");
            set => Preferences.Default.Set("weight_unit", value);
        }

        public string ElevationUnit
        {
            get => Preferences.Default.Get("elevation_unit", "Meters (m)");
            set => Preferences.Default.Set("elevation_unit", value);
        }

        public string DateFormat
        {
            get => Preferences.Default.Get("date_format", "DD/MM/YYYY");
            set => Preferences.Default.Set("date_format", value);
        }

        public string TimeFormat
        {
            get => Preferences.Default.Get("time_format", "12-Hour (AM/PM)");
            set => Preferences.Default.Set("time_format", value);
        }

        // Privacy & Security
        public bool TwoFactorEnabled
        {
            get => Preferences.Default.Get("two_factor", false);
            set => Preferences.Default.Set("two_factor", value);
        }

        public bool ShareAnalytics
        {
            get => Preferences.Default.Get("share_analytics", false);
            set => Preferences.Default.Set("share_analytics", value);
        }

        public string ActivityVisibility
        {
            get => Preferences.Default.Get("activity_visibility", "Everyone");
            set => Preferences.Default.Set("activity_visibility", value);
        }

        public string ProfileVisibility
        {
            get => Preferences.Default.Get("profile_visibility", "Friends Only");
            set => Preferences.Default.Set("profile_visibility", value);
        }

        public bool LocationSharing
        {
            get => Preferences.Default.Get("location_sharing", false);
            set => Preferences.Default.Set("location_sharing", value);
        }

        // Accessibility
        public bool ScreenReaderEnabled
        {
            get => Preferences.Default.Get("screen_reader", false);
            set => Preferences.Default.Set("screen_reader", value);
        }

        public bool HighContrastEnabled
        {
            get => Preferences.Default.Get("high_contrast", false);
            set => Preferences.Default.Set("high_contrast", value);
        }

        public bool ReduceMotionEnabled
        {
            get => Preferences.Default.Get("reduce_motion", false);
            set => Preferences.Default.Set("reduce_motion", value);
        }
    }
}
