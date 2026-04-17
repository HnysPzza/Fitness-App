namespace Fitness_App.Services;

public static class WorkoutReminderScheduleHelper
{
    public static DateTime? GetNextReminderDateTimeLocal(ISettingsService settings, DateTime? nowLocal = null)
    {
        if (!settings.PushNotificationsEnabled || !settings.WorkoutRemindersEnabled)
            return null;

        var selectedDays = ParseSelectedDays(settings.WorkoutReminderDays);
        if (selectedDays.Count == 0)
            return null;

        var now = nowLocal ?? DateTime.Now;
        for (var dayOffset = 0; dayOffset < 14; dayOffset++)
        {
            var date = now.Date.AddDays(dayOffset);
            if (!selectedDays.Contains(date.DayOfWeek))
                continue;

            var candidate = date.Add(settings.WorkoutReminderTime);
            if (candidate > now)
                return candidate;
        }

        return null;
    }

    public static string BuildSummaryLabel(ISettingsService settings, DateTime? nowLocal = null)
    {
        var next = GetNextReminderDateTimeLocal(settings, nowLocal);
        if (next == null)
            return "Reminders off";

        var now = nowLocal ?? DateTime.Now;
        var prefix = next.Value.Date == now.Date
            ? "Today"
            : next.Value.Date == now.Date.AddDays(1)
                ? "Tomorrow"
                : next.Value.ToString("ddd");

        return $"{prefix} {next.Value:h:mm tt}";
    }

    public static HashSet<DayOfWeek> ParseSelectedDays(string csv)
    {
        var result = new HashSet<DayOfWeek>();
        if (string.IsNullOrWhiteSpace(csv))
            return result;

        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (token.Trim().ToLowerInvariant())
            {
                case "mon":
                case "monday":
                    result.Add(DayOfWeek.Monday);
                    break;
                case "tue":
                case "tues":
                case "tuesday":
                    result.Add(DayOfWeek.Tuesday);
                    break;
                case "wed":
                case "wednesday":
                    result.Add(DayOfWeek.Wednesday);
                    break;
                case "thu":
                case "thurs":
                case "thursday":
                    result.Add(DayOfWeek.Thursday);
                    break;
                case "fri":
                case "friday":
                    result.Add(DayOfWeek.Friday);
                    break;
                case "sat":
                case "saturday":
                    result.Add(DayOfWeek.Saturday);
                    break;
                case "sun":
                case "sunday":
                    result.Add(DayOfWeek.Sunday);
                    break;
            }
        }

        return result;
    }
}
