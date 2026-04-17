namespace Fitness_App.Models;

public sealed class WorkoutPlanTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public List<WorkoutTemplateEntry> Entries { get; set; } = new();
}

public sealed class WorkoutTemplateEntry
{
    public int DayOffset { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Sport { get; set; } = string.Empty;
    public double? PlannedDistanceKm { get; set; }
    public int? PlannedDurationMinutes { get; set; }
}
