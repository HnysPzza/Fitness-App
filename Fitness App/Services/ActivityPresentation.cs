using Fitness_App.Models;

namespace Fitness_App.Services;

public static class ActivityPresentation
{
    public static string GetSportEmoji(string sport) => sport switch
    {
        "Running" or "Run" or "Trail Run" or "Virtual Run" => "🏃",
        "Cycling" or "Mountain Bike" or "Gravel Ride" or "E-Bike" or "E-Bike Ride" or "Road Ride" or "Commute" => "🚴",
        "Walking" or "Walk" or "Hike" => "🚶",
        "Swimming (Open Water)" or "Pool Swim" or "Swim" => "🏊",
        _ => "🏃"
    };

    public static string GetSportIcon(string sport) => sport switch
    {
        "Running" or "Run" or "Trail Run" or "Virtual Run" => Fitness_App.UI.Icons.MaterialSymbols.Directions_run,
        "Cycling" or "Mountain Bike" or "Gravel Ride" or "E-Bike" or "E-Bike Ride" or "Road Ride" or "Commute" => Fitness_App.UI.Icons.MaterialSymbols.Pedal_bike,
        "Walking" or "Walk" or "Hike" => Fitness_App.UI.Icons.MaterialSymbols.Directions_walk,
        "Swimming (Open Water)" or "Pool Swim" or "Swim" => Fitness_App.UI.Icons.MaterialSymbols.Pool,
        _ => Fitness_App.UI.Icons.MaterialSymbols.Directions_run
    };

    public static Color GetBadgeColor(string sport) => sport switch
    {
        "Running" or "Run" or "Trail Run" or "Virtual Run" => Color.FromArgb("#FEF3C7"),
        "Cycling" or "Mountain Bike" or "Gravel Ride" or "E-Bike" or "E-Bike Ride" or "Road Ride" or "Commute" => Color.FromArgb("#D1FAE5"),
        "Walking" or "Walk" or "Hike" => Color.FromArgb("#DBEAFE"),
        "Swimming (Open Water)" or "Pool Swim" or "Swim" => Color.FromArgb("#E0E7FF"),
        _ => Color.FromArgb("#FEF3C7")
    };

    public static string GetRouteColor(string sport) => sport switch
    {
        "Run" or "Trail Run" or "Virtual Run" => "#00F5FF",
        "Cycling" or "Mountain Bike" or "Gravel Ride" or "E-Bike" or "E-Bike Ride" or "Road Ride" or "Commute" => "#38BDF8",
        "Walk" or "Hike" => "#32CD32",
        _ => "#00F5FF"
    };

    public static string GetMetricLabel(UserActivity activity)
    {
        if (IsElevationSport(activity.Sport))
            return "Elevation Gain";

        if (IsPaceSport(activity.Sport))
            return "Average Pace";

        return "Highest Speed";
    }

    public static string GetMetricValue(UserActivity activity)
    {
        if (IsElevationSport(activity.Sport))
            return activity.ElevationGainM.HasValue ? $"{activity.ElevationGainM.Value:F0} m" : "--";

        if (IsPaceSport(activity.Sport))
            return FormatPace(activity.DistanceKm, activity.DurationTicks);

        if (activity.MaxSpeedKmh.HasValue)
            return $"{activity.MaxSpeedKmh.Value:F1} km/h";

        if (activity.AvgSpeedKmh.HasValue)
            return $"{activity.AvgSpeedKmh.Value:F1} km/h";

        return "--.- km/h";
    }

    public static string FormatDuration(long durationTicks)
    {
        var ts = TimeSpan.FromTicks(durationTicks);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:00}m";
        return $"{(int)ts.TotalMinutes} min";
    }

    public static string FormatPace(double distanceKm, long durationTicks)
    {
        if (distanceKm <= 0.01)
            return "--:-- /km";

        var duration = TimeSpan.FromTicks(durationTicks);
        if (duration.TotalMinutes <= 0)
            return "--:-- /km";

        var pace = duration.TotalMinutes / distanceKm;
        var minutes = (int)Math.Floor(pace);
        var seconds = (int)Math.Floor((pace - minutes) * 60);
        return $"{minutes}:{seconds:00} /km";
    }

    public static bool IsPaceSport(string sport) => sport switch
    {
        "Walk" or "Walking" or "Hike" or "Open Water Swim" or "Swimming (Open Water)" or "Kayaking" or "SUP" or "Rowing" => true,
        _ => false
    };

    public static bool IsElevationSport(string sport) => sport switch
    {
        "Hike" or "Alpine Ski" or "Snowboard" or "Cross-Country Ski" or "Snowshoe" or "Ice Climb" or "Rock Climb" or "Rock Climbing" => true,
        _ => false
    };
}
