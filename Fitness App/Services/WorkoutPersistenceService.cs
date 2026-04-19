using System.Text.Json;
using System.Text.Json.Serialization;
using Fitness_App.Models;

namespace Fitness_App.Services;

public enum WorkoutRecordingState
{
    Recording,
    FinishPending
}

public sealed class StoredWorkoutTrackPoint
{
    public double Lng { get; set; }
    public double Lat { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? AltitudeMeters { get; set; }
    public double? SpeedKmh { get; set; }
}

public sealed class ActiveWorkoutSession
{
    public string Sport { get; set; } = "Activity";
    public bool IsGpsDependent { get; set; }
    public WorkoutRecordingState State { get; set; } = WorkoutRecordingState.Recording;
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PausedAtUtc { get; set; }
    public long PausedDurationTicks { get; set; }
    public long ElapsedTicks { get; set; }
    public double DistanceKm { get; set; }
    public double CurrentSpeedKmh { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double MaxSpeedKmh { get; set; }
    public double ElevationGainM { get; set; }
    public double? LastAcceptedAltitudeM { get; set; }
    public List<StoredWorkoutTrackPoint> TrackPoints { get; set; } = new();
}

public sealed class PendingStoredActivity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Sport { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public long DurationTicks { get; set; }
    public double? AvgSpeedKmh { get; set; }
    public double? MaxSpeedKmh { get; set; }
    public double? ElevationGainM { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CoordinatesJson { get; set; } = string.Empty;
    public DateTimeOffset PendingSavedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string FailureReason { get; set; } = string.Empty;

    public UserActivity ToUserActivity() => new()
    {
        Id = Id,
        UserId = UserId,
        Sport = Sport,
        DistanceKm = DistanceKm,
        DurationTicks = DurationTicks,
        AvgSpeedKmh = AvgSpeedKmh,
        MaxSpeedKmh = MaxSpeedKmh,
        ElevationGainM = ElevationGainM,
        CreatedAt = CreatedAt,
        CoordinatesJson = CoordinatesJson
    };

    public static PendingStoredActivity FromUserActivity(UserActivity activity, string failureReason) => new()
    {
        Id = activity.Id,
        UserId = activity.UserId,
        Sport = activity.Sport,
        DistanceKm = activity.DistanceKm,
        DurationTicks = activity.DurationTicks,
        AvgSpeedKmh = activity.AvgSpeedKmh,
        MaxSpeedKmh = activity.MaxSpeedKmh,
        ElevationGainM = activity.ElevationGainM,
        CreatedAt = activity.CreatedAt,
        CoordinatesJson = activity.CoordinatesJson,
        PendingSavedAtUtc = DateTimeOffset.UtcNow,
        FailureReason = failureReason
    };
}

public sealed class WorkoutPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static string ActiveSessionPath => Path.Combine(FileSystem.AppDataDirectory, "active-workout-session.json");
    private static string PendingActivitiesPath => Path.Combine(FileSystem.AppDataDirectory, "pending-activities.json");

    public async Task<ActiveWorkoutSession?> LoadActiveSessionAsync()
    {
        try
        {
            if (!File.Exists(ActiveSessionPath))
                return null;

            await using var stream = File.OpenRead(ActiveSessionPath);
            var session = await JsonSerializer.DeserializeAsync<ActiveWorkoutSession>(stream, JsonOptions);
            if (session == null || string.IsNullOrWhiteSpace(session.Sport))
                return null;

            session.TrackPoints ??= new List<StoredWorkoutTrackPoint>();
            return session;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkoutPersistence] LoadActiveSession: {ex.Message}");
            return null;
        }
    }

    public async Task SaveActiveSessionAsync(ActiveWorkoutSession session)
    {
        try
        {
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            await using var stream = File.Create(ActiveSessionPath);
            await JsonSerializer.SerializeAsync(stream, session, JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkoutPersistence] SaveActiveSession: {ex.Message}");
        }
    }

    public Task ClearActiveSessionAsync()
    {
        try
        {
            if (File.Exists(ActiveSessionPath))
                File.Delete(ActiveSessionPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkoutPersistence] ClearActiveSession: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public async Task MarkActiveSessionFinishPendingAsync()
    {
        var session = await LoadActiveSessionAsync();
        if (session == null)
            return;

        if (!session.PausedAtUtc.HasValue && session.StartedAtUtc != default)
        {
            var pausedDuration = TimeSpan.FromTicks(Math.Max(0, session.PausedDurationTicks));
            var elapsed = DateTimeOffset.UtcNow - session.StartedAtUtc - pausedDuration;
            if (elapsed > TimeSpan.Zero && elapsed.Ticks > session.ElapsedTicks)
                session.ElapsedTicks = elapsed.Ticks;
        }

        session.State = WorkoutRecordingState.FinishPending;
        session.PausedAtUtc ??= DateTimeOffset.UtcNow;
        await SaveActiveSessionAsync(session);
    }

    public async Task<IReadOnlyList<PendingStoredActivity>> LoadPendingActivitiesAsync()
    {
        try
        {
            if (!File.Exists(PendingActivitiesPath))
                return Array.Empty<PendingStoredActivity>();

            await using var stream = File.OpenRead(PendingActivitiesPath);
            var activities = await JsonSerializer.DeserializeAsync<List<PendingStoredActivity>>(stream, JsonOptions);
            return activities ?? new List<PendingStoredActivity>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkoutPersistence] LoadPendingActivities: {ex.Message}");
            return Array.Empty<PendingStoredActivity>();
        }
    }

    public async Task AddPendingActivityAsync(UserActivity activity, string failureReason)
    {
        var pending = (await LoadPendingActivitiesAsync()).ToList();
        pending.RemoveAll(item => string.Equals(item.Id, activity.Id, StringComparison.OrdinalIgnoreCase));
        pending.Add(PendingStoredActivity.FromUserActivity(activity, failureReason));
        await SavePendingActivitiesAsync(pending);
    }

    public async Task RemovePendingActivityAsync(string activityId)
    {
        var pending = (await LoadPendingActivitiesAsync()).ToList();
        pending.RemoveAll(item => string.Equals(item.Id, activityId, StringComparison.OrdinalIgnoreCase));
        await SavePendingActivitiesAsync(pending);
    }

    private async Task SavePendingActivitiesAsync(List<PendingStoredActivity> pending)
    {
        try
        {
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            await using var stream = File.Create(PendingActivitiesPath);
            await JsonSerializer.SerializeAsync(stream, pending, JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkoutPersistence] SavePendingActivities: {ex.Message}");
        }
    }
}
