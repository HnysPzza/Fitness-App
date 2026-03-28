namespace Fitness_App.Models;

/// <summary>Aggregated stats for a set of UserActivity records.</summary>
public class UserStats
{
    public double   TotalKm         { get; set; }
    public int      TotalActivities { get; set; }
    public TimeSpan TotalTime       { get; set; }
    public double   AvgSpeedKmh     { get; set; }

    // Display helpers
    public string TotalKmDisplay   => TotalKm.ToString("F1");
    public string TotalTimeDisplay
    {
        get
        {
            if (TotalTime.TotalHours >= 1)
                return $"{(int)TotalTime.TotalHours}h {TotalTime.Minutes:00}m";
            return $"{TotalTime.Minutes}m";
        }
    }
    public string AvgSpeedDisplay  => AvgSpeedKmh.ToString("F1");
    public string ActivitiesDisplay => TotalActivities.ToString();
}
