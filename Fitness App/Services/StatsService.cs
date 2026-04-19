using Fitness_App.Models;
using Postgrest = Supabase.Postgrest;

namespace Fitness_App.Services;

/// <summary>
/// Queries the Supabase "user_activities" table and returns aggregated stats
/// for the currently logged-in user.
/// </summary>
public class StatsService
{
    private readonly ISupabaseService _supabase;
    private readonly WorkoutPersistenceService _workoutPersistence;

    public StatsService(ISupabaseService supabase, WorkoutPersistenceService workoutPersistence)
    {
        _supabase = supabase;
        _workoutPersistence = workoutPersistence;
    }

    public string? LastSaveError { get; private set; }
    public bool LastSaveWasPending { get; private set; }

    public async Task<UserStats> GetStatsInRangeAsync(DateTime fromUtc, DateTime? toUtc = null)
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return new UserStats();

            var query = _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Filter("created_at", Postgrest.Constants.Operator.GreaterThanOrEqual, fromUtc.ToUniversalTime().ToString("O"));

            if (toUtc.HasValue)
            {
                query = query.Filter("created_at", Postgrest.Constants.Operator.LessThan, toUtc.Value.ToUniversalTime().ToString("O"));
            }

            var result = await query.Get();
            return BuildStats(result.Models);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetStatsInRange: {ex.Message}");
            return new UserStats();
        }
    }

    // ── All-time stats ────────────────────────────────────────────────────

    public async Task<UserStats> GetAllTimeStatsAsync()
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return new UserStats();

            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Get();

            return BuildStats(result.Models);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetAllTimeStats: {ex.Message}");
            return new UserStats();
        }
    }

    // ── Today stats ───────────────────────────────────────────────────────

    public async Task<UserStats> GetTodayStatsAsync()
    {
        try
        {
            return await GetStatsInRangeAsync(DateTime.UtcNow.Date);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetTodayStats: {ex.Message}");
            return new UserStats();
        }
    }

    // ── This-week stats ───────────────────────────────────────────────────

    public async Task<UserStats> GetThisWeekStatsAsync()
    {
        try
        {
            var today  = DateTime.UtcNow.Date;
            int offset = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
            if (offset < 0) offset += 7;
            var monday = today.AddDays(-offset);

            return await GetStatsInRangeAsync(monday);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetThisWeekStats: {ex.Message}");
            return new UserStats();
        }
    }

    // ── Month-over-month KM comparison ────────────────────────────────────

    /// <summary>
    /// Returns (currentMonthKm, lastMonthKm, percentChange).
    /// percentChange is positive when this month is higher than last month.
    /// Returns (0, 0, 0) when there is no data.
    /// </summary>
    public async Task<(double CurrentMonthKm, double LastMonthKm, double PercentChange)> GetMonthlyKmComparisonAsync()
    {
        try
        {
            var now           = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            var thisMonthTask = GetStatsInRangeAsync(thisMonthStart, thisMonthStart.AddMonths(1));
            var lastMonthTask = GetStatsInRangeAsync(lastMonthStart, thisMonthStart);

            await Task.WhenAll(thisMonthTask, lastMonthTask);

            var thisKm = (await thisMonthTask).TotalKm;
            var lastKm = (await lastMonthTask).TotalKm;

            double pct = 0;
            if (lastKm > 0)
                pct = ((thisKm - lastKm) / lastKm) * 100.0;
            else if (thisKm > 0)
                pct = 100.0; // anything vs nothing = +100%

            return (thisKm, lastKm, pct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetMonthlyKmComparison: {ex.Message}");
            return (0, 0, 0);
        }
    }


    public async Task<int> GetCurrentStreakAsync()
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return 0;

            var today = DateTime.Now.Date;
            var from = today.AddDays(-365).ToUniversalTime().ToString("O");
            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Filter("created_at", Postgrest.Constants.Operator.GreaterThanOrEqual, from)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var activities = result.Models.ToList();
            var pendingActivities = await _workoutPersistence.LoadPendingActivitiesAsync();
            activities.AddRange(pendingActivities
                .Where(activity => string.Equals(activity.UserId, _supabase.CurrentUser.Id, StringComparison.OrdinalIgnoreCase))
                .Select(activity => activity.ToUserActivity()));

            var activeDates = activities
                .Select(activity => activity.CreatedAt.ToLocalTime().Date)
                .ToHashSet();

            if (!activeDates.Contains(today))
                return 0;

            var streak = 0;
            var expected = today;
            while (activeDates.Contains(expected))
            {
                streak++;
                expected = expected.AddDays(-1);
            }

            return streak;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetCurrentStreak: {ex.Message}");
            return 0;
        }
    }

    public async Task<List<string>> GetTopSportsAsync(int limit = 4)
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return new List<string>();

            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Limit(50)
                .Get();

            return result.Models
                .Where(activity => !string.IsNullOrWhiteSpace(activity.Sport))
                .GroupBy(activity => activity.Sport)
                .OrderByDescending(group => group.Count())
                .ThenByDescending(group => group.Max(activity => activity.CreatedAt))
                .Take(limit)
                .Select(group => group.Key)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetTopSports: {ex.Message}");
            return new List<string>();
        }
    }

    // ── Yesterday's last activity ─────────────────────────────────────────

    public async Task<UserActivity?> GetYesterdayActivityAsync()
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return null;

            var yesterday = DateTime.UtcNow.Date.AddDays(-1).ToString("O");
            var today     = DateTime.UtcNow.Date.ToString("O");

            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Filter("created_at", Postgrest.Constants.Operator.GreaterThanOrEqual, yesterday)
                .Filter("created_at", Postgrest.Constants.Operator.LessThan, today)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetYesterdayActivity: {ex.Message}");
            return null;
        }
    }

    // ── Recent activities ─────────────────────────────────────────────────

    public async Task<List<UserActivity>> GetRecentActivitiesAsync(int limit = 10)
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return new List<UserActivity>();

            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            return result.Models;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetRecentActivities: {ex.Message}");
            return new List<UserActivity>();
        }
    }

    public async Task<UserActivity?> GetActivityByIdAsync(string activityId)
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null || string.IsNullOrWhiteSpace(activityId))
                return null;

            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Filter("id", Postgrest.Constants.Operator.Equals, activityId)
                .Limit(1)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetActivityById: {ex.Message}");
            return null;
        }
    }

    public async Task<UserActivity?> SaveActivityAsync(UserActivity activity)
    {
        LastSaveError = null;
        LastSaveWasPending = false;

        try
        {
            await _supabase.InitializeAsync();

            if (_supabase.Client == null || _supabase.CurrentUser == null)
            {
                LastSaveError = "You need to be signed in before saving an activity.";
                return null;
            }

            activity.UserId = _supabase.CurrentUser.Id;

            if (string.IsNullOrWhiteSpace(activity.Id))
                activity.Id = Guid.NewGuid().ToString();

            if (activity.CreatedAt == default)
                activity.CreatedAt = DateTime.UtcNow;

            var lastTransientError = string.Empty;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await _supabase.Client
                        .From<UserActivity>()
                        .Insert(activity);

                    return activity;
                }
                catch (Exception ex)
                {
                    if (await ActivityExistsAsync(activity.Id))
                        return activity;

                    if (!IsTransientSaveException(ex))
                        throw;

                    lastTransientError = BuildErrorMessage(ex);
                    System.Diagnostics.Debug.WriteLine($"[StatsService] SaveActivity transient attempt {attempt}: {ex.Message}");
                    if (attempt < 3)
                        await Task.Delay(TimeSpan.FromMilliseconds(350 * attempt));
                }
            }

            await _workoutPersistence.AddPendingActivityAsync(activity, lastTransientError);
            LastSaveWasPending = true;
            LastSaveError = "Saved locally. It will sync automatically when the connection is stable.";
            return activity;
        }
        catch (Exception ex)
        {
            // Surface the deepest exception message so PostgREST schema errors are visible
            // in both the debug output AND the alert shown to the user.
            var inner   = ex.InnerException?.Message ?? string.Empty;
            LastSaveError = string.IsNullOrWhiteSpace(inner) ? ex.Message : $"{ex.Message} → {inner}";
            System.Diagnostics.Debug.WriteLine($"[StatsService] SaveActivity: {ex}");
            return null;
        }
    }

    // ── Chart data (grouped by day) ────────────────────────────────────────

    public async Task<int> SyncPendingActivitiesAsync()
    {
        LastSaveWasPending = false;

        var pending = await _workoutPersistence.LoadPendingActivitiesAsync();
        if (pending.Count == 0)
            return 0;

        try
        {
            await _supabase.InitializeAsync();
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] SyncPendingActivities init: {ex.Message}");
            return 0;
        }

        var synced = 0;
        foreach (var pendingActivity in pending)
        {
            var activity = pendingActivity.ToUserActivity();
            if (string.IsNullOrWhiteSpace(activity.UserId))
                activity.UserId = _supabase.CurrentUser!.Id;

            try
            {
                if (await ActivityExistsAsync(activity.Id))
                {
                    await _workoutPersistence.RemovePendingActivityAsync(activity.Id);
                    synced++;
                    continue;
                }

                await _supabase.Client!
                    .From<UserActivity>()
                    .Insert(activity);

                await _workoutPersistence.RemovePendingActivityAsync(activity.Id);
                synced++;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StatsService] SyncPendingActivities {activity.Id}: {ex.Message}");
            }
        }

        return synced;
    }

    private async Task<bool> ActivityExistsAsync(string activityId)
    {
        if (_supabase.Client == null || _supabase.CurrentUser == null || string.IsNullOrWhiteSpace(activityId))
            return false;

        try
        {
            var existing = await GetActivityByIdAsync(activityId);
            return existing != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTransientSaveException(Exception ex)
    {
        var message = ex.ToString();
        return ex is HttpRequestException
            || ex is TaskCanceledException
            || message.Contains("connection reset", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection was reset", StringComparison.OrdinalIgnoreCase)
            || message.Contains("temporarily", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || message.Contains("network", StringComparison.OrdinalIgnoreCase)
            || message.Contains("transport", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildErrorMessage(Exception ex)
    {
        var inner = ex.InnerException?.Message ?? string.Empty;
        return string.IsNullOrWhiteSpace(inner) ? ex.Message : $"{ex.Message} -> {inner}";
    }

    public async Task<List<(string Label, float Value)>> GetChartDataAsync(string period)
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return new List<(string, float)>();

            var from = period switch
            {
                "Today" => DateTime.UtcNow.Date,
                "Week"  => DateTime.UtcNow.Date.AddDays(-6),
                "Month" => DateTime.UtcNow.Date.AddDays(-30),
                _       => DateTime.UtcNow.Date
            };

            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Filter("created_at", Postgrest.Constants.Operator.GreaterThanOrEqual, from.ToString("O"))
                .Order("created_at", Postgrest.Constants.Ordering.Ascending)
                .Get();

            var buckets = BuildChartBuckets(period);
            var totals = result.Models
                .GroupBy(a => NormalizeBucketDate(period, a.CreatedAt))
                .ToDictionary(g => g.Key, g => (float)g.Sum(a => a.DistanceKm));

            return buckets
                .Select(bucket => (
                    Label: bucket.Label,
                    Value: totals.TryGetValue(bucket.Key, out var value) ? value : 0f
                ))
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetChartData: {ex.Message}");
            return GenerateFlatLine(period);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static UserStats BuildStats(List<UserActivity> activities)
    {
        if (activities.Count == 0) return new UserStats();

        double totalKm   = activities.Sum(a => a.DistanceKm);
        long   totalTicks = activities.Sum(a => a.DurationTicks);
        var    totalTime = TimeSpan.FromTicks(totalTicks);

        double avgSpeed = totalTime.TotalHours > 0
            ? totalKm / totalTime.TotalHours
            : 0;

        return new UserStats
        {
            TotalKm         = totalKm,
            TotalActivities = activities.Count,
            ActiveDays      = activities
                .Select(activity => activity.CreatedAt.ToLocalTime().Date)
                .Distinct()
                .Count(),
            TotalTime       = totalTime,
            AvgSpeedKmh     = avgSpeed
        };
    }

    /// Returns a zero-value placeholder so the chart still renders.
    private static List<(string Label, float Value)> GenerateFlatLine(string period) =>
        BuildChartBuckets(period)
            .Select(bucket => (bucket.Label, 0f))
            .ToList();

    private static List<(DateTime Key, string Label)> BuildChartBuckets(string period)
    {
        var now = DateTime.UtcNow;

        return period switch
        {
            "Today" => Enumerable.Range(0, 7)
                .Select(i => now.Date.AddHours(i * 3))
                .Select(dt => (dt, dt.ToLocalTime().ToString("HH:mm")))
                .ToList(),
            "Week" => Enumerable.Range(0, 7)
                .Select(i => now.Date.AddDays(i - 6))
                .Select(dt => (dt, dt.ToLocalTime().ToString("ddd")))
                .ToList(),
            _ => Enumerable.Range(0, 5)
                .Select(i => now.Date.AddDays(i * 6 - 24))
                .Select(dt => (dt, dt.ToLocalTime().ToString("MMM dd")))
                .ToList()
        };
    }

    private static DateTime NormalizeBucketDate(string period, DateTime createdAt)
    {
        var utc = createdAt.Kind == DateTimeKind.Utc ? createdAt : createdAt.ToUniversalTime();
        return period switch
        {
            "Today" => utc.Date.AddHours((utc.Hour / 3) * 3),
            "Week" => utc.Date,
            _ => NormalizeMonthBucket(utc.Date)
        };
    }

    private static DateTime NormalizeMonthBucket(DateTime date)
    {
        var start = DateTime.UtcNow.Date.AddDays(-24);
        var daysFromStart = Math.Max(0, (date - start).Days);
        return start.AddDays((daysFromStart / 6) * 6);
    }
}
