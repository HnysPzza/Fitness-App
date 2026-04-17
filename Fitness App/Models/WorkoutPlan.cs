using System.Text.Json.Serialization;

namespace Fitness_App.Models;

public sealed class WorkoutPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsTemplateBased { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public List<PlannedWorkout> Workouts { get; set; } = new();

    [JsonIgnore]
    public int DurationDays => Math.Max(1, (EndDate.Date - StartDate.Date).Days + 1);

    [JsonIgnore]
    public int CompletedCount => Workouts.Count(workout => workout.IsCompleted);

    [JsonIgnore]
    public int TotalCount => Workouts.Count;

    [JsonIgnore]
    public double CompletionRatio => TotalCount == 0 ? 0 : (double)CompletedCount / TotalCount;

    [JsonIgnore]
    public string ProgressText => TotalCount == 0 ? "No workouts yet" : $"{CompletedCount}/{TotalCount} completed";

    [JsonIgnore]
    public bool IsActiveToday
    {
        get
        {
            var today = DateTime.Today;
            return StartDate.Date <= today && EndDate.Date >= today;
        }
    }
}
