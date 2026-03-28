using Fitness_App.Models;

namespace Fitness_App.Services;

/// <summary>
/// Queries the Supabase "user_activities" table and returns aggregated stats
/// for the currently logged-in user.
/// </summary>
public class StatsService
{
    private readonly ISupabaseService _supabase;

    public StatsService(ISupabaseService supabase)
    {
        _supabase = supabase;
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

    // ── This-week stats ───────────────────────────────────────────────────

    public async Task<UserStats> GetThisWeekStatsAsync()
    {
        try
        {
            if (_supabase.Client == null || _supabase.CurrentUser == null)
                return new UserStats();

            // ISO-8601 monday of current week
            var today  = DateTime.UtcNow.Date;
            int offset = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
            if (offset < 0) offset += 7;
            var monday = today.AddDays(-offset).ToString("O");

            var result = await _supabase.Client
                .From<UserActivity>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, _supabase.CurrentUser.Id)
                .Filter("created_at", Postgrest.Constants.Operator.GreaterThanOrEqual, monday)
                .Get();

            return BuildStats(result.Models);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StatsService] GetThisWeekStats: {ex.Message}");
            return new UserStats();
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

    // ── Chart data (grouped by day) ────────────────────────────────────────

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
