namespace Fitness_App.Services;

public enum WorkoutCommand
{
    TogglePauseResume,
    Stop
}

public sealed record WorkoutSessionSnapshot(
    bool IsActive,
    bool IsPaused,
    string Sport,
    bool IsGpsDependent,
    TimeSpan Elapsed,
    double DistanceKm,
    double MaxSpeedKmh,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? PausedAtUtc);

public static class WorkoutSessionManager
{
    private static readonly object Sync = new();
    private static DateTimeOffset _startedAtUtc;
    private static DateTimeOffset? _pausedAtUtc;
    private static TimeSpan _pausedDuration = TimeSpan.Zero;
    private static string _sport = "Activity";
    private static bool _isGpsDependent;
    private static double _distanceKm;
    private static double _maxSpeedKmh;

    public static event EventHandler<WorkoutCommand>? CommandRequested;

    public static void Start(string sport, bool isGpsDependent)
    {
        lock (Sync)
        {
            _startedAtUtc = DateTimeOffset.UtcNow;
            _pausedAtUtc = null;
            _pausedDuration = TimeSpan.Zero;
            _sport = string.IsNullOrWhiteSpace(sport) ? "Activity" : sport;
            _isGpsDependent = isGpsDependent;
            _distanceKm = 0;
            _maxSpeedKmh = 0;
        }
    }

    public static void UpdateMetrics(string sport, bool isGpsDependent, double distanceKm, double maxSpeedKmh)
    {
        lock (Sync)
        {
            if (_startedAtUtc == default)
                return;

            _sport = string.IsNullOrWhiteSpace(sport) ? _sport : sport;
            _isGpsDependent = isGpsDependent;
            _distanceKm = Math.Max(0, distanceKm);
            _maxSpeedKmh = Math.Max(0, maxSpeedKmh);
        }
    }

    public static void Pause()
    {
        lock (Sync)
        {
            if (_startedAtUtc == default || _pausedAtUtc.HasValue)
                return;

            _pausedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public static void Resume()
    {
        lock (Sync)
        {
            if (_startedAtUtc == default || !_pausedAtUtc.HasValue)
                return;

            _pausedDuration += DateTimeOffset.UtcNow - _pausedAtUtc.Value;
            _pausedAtUtc = null;
        }
    }

    public static void Stop()
    {
        lock (Sync)
        {
            _startedAtUtc = default;
            _pausedAtUtc = null;
            _pausedDuration = TimeSpan.Zero;
            _sport = "Activity";
            _isGpsDependent = false;
            _distanceKm = 0;
            _maxSpeedKmh = 0;
        }
    }

    public static WorkoutSessionSnapshot GetSnapshot()
    {
        lock (Sync)
        {
            if (_startedAtUtc == default)
            {
                return new WorkoutSessionSnapshot(
                    false,
                    false,
                    "Activity",
                    false,
                    TimeSpan.Zero,
                    0,
                    0,
                    default,
                    null);
            }

            var now = DateTimeOffset.UtcNow;
            var pausedAt = _pausedAtUtc;
            var pausedDuration = _pausedDuration;
            if (pausedAt.HasValue)
                pausedDuration += now - pausedAt.Value;

            var elapsed = now - _startedAtUtc - pausedDuration;
            if (elapsed < TimeSpan.Zero)
                elapsed = TimeSpan.Zero;

            return new WorkoutSessionSnapshot(
                true,
                pausedAt.HasValue,
                _sport,
                _isGpsDependent,
                elapsed,
                _distanceKm,
                _maxSpeedKmh,
                _startedAtUtc,
                pausedAt);
        }
    }

    public static void RequestCommand(WorkoutCommand command)
    {
        CommandRequested?.Invoke(null, command);
    }
}
