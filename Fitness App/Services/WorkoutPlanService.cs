using System.Text.Json;
using Fitness_App.Models;

namespace Fitness_App.Services;

public interface IWorkoutPlanService
{
    Task<WorkoutPlan?> GetCurrentPlanAsync();
    Task<IReadOnlyList<WorkoutPlanTemplate>> GetTemplatesAsync();
    Task<WorkoutPlan> CreateTemplatePlanAsync(string templateId, DateTime? startDate = null);
    Task<WorkoutPlan> SaveCustomPlanAsync(string title, DateTime startDate, int durationDays, IReadOnlyList<PlannedWorkout> workouts);
    Task<bool> ToggleWorkoutCompletionAsync(string plannedWorkoutId);
    Task DeleteCurrentPlanAsync();
    PlannedWorkout? GetNextPlannedWorkout(WorkoutPlan? plan, DateTime? nowLocal = null);
    string BuildReminderMessage(string fallbackMessage, WorkoutPlan? plan, DateTime? nowLocal = null);
}

public sealed class WorkoutPlanService : IWorkoutPlanService
{
    public const string StorageKey = "home_workout_plan";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public Task<WorkoutPlan?> GetCurrentPlanAsync()
    {
        try
        {
            var raw = Preferences.Default.Get(StorageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return Task.FromResult<WorkoutPlan?>(null);

            var plan = JsonSerializer.Deserialize<WorkoutPlan>(raw, SerializerOptions);
            return Task.FromResult(plan);
        }
        catch
        {
            return Task.FromResult<WorkoutPlan?>(null);
        }
    }

    public Task<IReadOnlyList<WorkoutPlanTemplate>> GetTemplatesAsync()
        => Task.FromResult<IReadOnlyList<WorkoutPlanTemplate>>(BuildTemplates());

    public async Task<WorkoutPlan> CreateTemplatePlanAsync(string templateId, DateTime? startDate = null)
    {
        var template = BuildTemplates().FirstOrDefault(item => item.Id == templateId)
            ?? BuildTemplates().First();

        var localStart = (startDate ?? DateTime.Today).Date;
        var workouts = template.Entries
            .Select(entry => new PlannedWorkout
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = entry.Title,
                Sport = entry.Sport,
                ScheduledDate = localStart.AddDays(entry.DayOffset),
                PlannedDistanceKm = entry.PlannedDistanceKm,
                PlannedDurationMinutes = entry.PlannedDurationMinutes,
                IsTemplateGenerated = true
            })
            .ToList();

        return await SavePlanAsync(new WorkoutPlan
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = template.Title,
            StartDate = localStart,
            EndDate = localStart.AddDays(Math.Max(0, template.DurationDays - 1)),
            IsTemplateBased = true,
            TemplateId = template.Id,
            Workouts = workouts
        });
    }

    public async Task<WorkoutPlan> SaveCustomPlanAsync(string title, DateTime startDate, int durationDays, IReadOnlyList<PlannedWorkout> workouts)
    {
        var normalizedWorkouts = workouts
            .Select(workout => new PlannedWorkout
            {
                Id = string.IsNullOrWhiteSpace(workout.Id) ? Guid.NewGuid().ToString("N") : workout.Id,
                PlanId = workout.PlanId,
                Title = workout.Title,
                Sport = workout.Sport,
                ScheduledDate = workout.ScheduledDate,
                PlannedDistanceKm = workout.PlannedDistanceKm,
                PlannedDurationMinutes = workout.PlannedDurationMinutes,
                IsCompleted = workout.IsCompleted,
                IsTemplateGenerated = false
            })
            .OrderBy(workout => workout.ScheduledDate)
            .ToList();

        return await SavePlanAsync(new WorkoutPlan
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = string.IsNullOrWhiteSpace(title) ? "Custom Plan" : title.Trim(),
            StartDate = startDate.Date,
            EndDate = startDate.Date.AddDays(Math.Max(0, durationDays - 1)),
            IsTemplateBased = false,
            Workouts = normalizedWorkouts
        });
    }

    public async Task<bool> ToggleWorkoutCompletionAsync(string plannedWorkoutId)
    {
        var plan = await GetCurrentPlanAsync();
        if (plan == null)
            return false;

        var workout = plan.Workouts.FirstOrDefault(item => item.Id == plannedWorkoutId);
        if (workout == null)
            return false;

        workout.IsCompleted = !workout.IsCompleted;
        await SavePlanAsync(plan);
        return true;
    }

    public Task DeleteCurrentPlanAsync()
    {
        Preferences.Default.Remove(StorageKey);
        return Task.CompletedTask;
    }

    public PlannedWorkout? GetNextPlannedWorkout(WorkoutPlan? plan, DateTime? nowLocal = null)
    {
        if (plan == null)
            return null;

        var now = (nowLocal ?? DateTime.Now).Date;
        return plan.Workouts
            .Where(workout => !workout.IsCompleted)
            .OrderBy(workout => workout.ScheduledDate)
            .FirstOrDefault(workout => workout.ScheduledDate.Date >= now)
            ?? plan.Workouts
                .Where(workout => !workout.IsCompleted)
                .OrderBy(workout => workout.ScheduledDate)
                .FirstOrDefault();
    }

    public string BuildReminderMessage(string fallbackMessage, WorkoutPlan? plan, DateTime? nowLocal = null)
    {
        var nextWorkout = GetNextPlannedWorkout(plan, nowLocal);
        if (nextWorkout == null)
            return fallbackMessage;

        var when = nextWorkout.ScheduledDate.Date == (nowLocal ?? DateTime.Now).Date
            ? "today"
            : $"on {nextWorkout.ScheduledDate:ddd}";

        return $"Time for your planned {nextWorkout.Sport.ToLowerInvariant()} {when}!";
    }

    private static Task<WorkoutPlan> SavePlanAsync(WorkoutPlan plan)
    {
        foreach (var workout in plan.Workouts)
        {
            workout.PlanId = plan.Id;
        }

        var json = JsonSerializer.Serialize(plan, SerializerOptions);
        Preferences.Default.Set(StorageKey, json);
        return Task.FromResult(plan);
    }

    private static List<WorkoutPlanTemplate> BuildTemplates()
    {
        return new List<WorkoutPlanTemplate>
        {
            new()
            {
                Id = "balanced-week",
                Title = "Balanced Week",
                Description = "A simple mixed week with cardio and recovery.",
                DurationDays = 7,
                Entries = new List<WorkoutTemplateEntry>
                {
                    new() { DayOffset = 0, Title = "Easy Run", Sport = "Run", PlannedDistanceKm = 3.5 },
                    new() { DayOffset = 1, Title = "Mobility", Sport = "Yoga", PlannedDurationMinutes = 25 },
                    new() { DayOffset = 3, Title = "Bike Session", Sport = "Cycling", PlannedDurationMinutes = 45 },
                    new() { DayOffset = 5, Title = "Long Walk", Sport = "Walk", PlannedDistanceKm = 5.0 },
                    new() { DayOffset = 6, Title = "Recovery", Sport = "Pilates", PlannedDurationMinutes = 20 }
                }
            },
            new()
            {
                Id = "runner-starter",
                Title = "Runner Starter",
                Description = "A practical week focused on building run consistency.",
                DurationDays = 7,
                Entries = new List<WorkoutTemplateEntry>
                {
                    new() { DayOffset = 0, Title = "Easy Run", Sport = "Run", PlannedDistanceKm = 4.0 },
                    new() { DayOffset = 2, Title = "Tempo Run", Sport = "Run", PlannedDurationMinutes = 30 },
                    new() { DayOffset = 4, Title = "Strength", Sport = "Gym Workout", PlannedDurationMinutes = 35 },
                    new() { DayOffset = 6, Title = "Long Run", Sport = "Run", PlannedDistanceKm = 7.0 }
                }
            },
            new()
            {
                Id = "bike-builder",
                Title = "Bike Builder",
                Description = "A low-friction cycling plan for weekday consistency.",
                DurationDays = 7,
                Entries = new List<WorkoutTemplateEntry>
                {
                    new() { DayOffset = 1, Title = "Recovery Ride", Sport = "Cycling", PlannedDurationMinutes = 35 },
                    new() { DayOffset = 3, Title = "Intervals", Sport = "Cycling", PlannedDurationMinutes = 45 },
                    new() { DayOffset = 5, Title = "Gym Support", Sport = "Gym Workout", PlannedDurationMinutes = 30 },
                    new() { DayOffset = 6, Title = "Weekend Ride", Sport = "Cycling", PlannedDistanceKm = 20.0 }
                }
            }
        };
    }
}
