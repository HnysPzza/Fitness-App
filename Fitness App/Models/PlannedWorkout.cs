namespace Fitness_App.Models;

public sealed class PlannedWorkout
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PlanId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Sport { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public double? PlannedDistanceKm { get; set; }
    public int? PlannedDurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsTemplateGenerated { get; set; }

    public string DayLabel => ScheduledDate.ToLocalTime().ToString("ddd");

    public string ScheduledDateLabel => ScheduledDate.ToLocalTime().ToString("MMM d");

    public string TargetLabel
    {
        get
        {
            if (PlannedDistanceKm.HasValue && PlannedDistanceKm.Value > 0)
                return $"{PlannedDistanceKm.Value:F1} km";

            if (PlannedDurationMinutes.HasValue && PlannedDurationMinutes.Value > 0)
                return $"{PlannedDurationMinutes.Value} min";

            return "Flexible";
        }
    }
}
