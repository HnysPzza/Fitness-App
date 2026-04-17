namespace Fitness_App.Services;

public sealed record NotificationNavigationRequest(string TargetPage, string PlannedSport, string PlannedWorkoutId);

public static class NotificationNavigationService
{
    public const string ExtraTargetPage = "fitness_target_page";
    public const string ExtraPlannedSport = "fitness_planned_sport";
    public const string ExtraPlannedWorkoutId = "fitness_planned_workout_id";
    public const string TargetYouPage = "you";
    public const string TargetRecordPage = "record";

    public static NotificationNavigationRequest? PendingRequest { get; private set; }

    public static void SetPending(string? targetPage, string? plannedSport = null, string? plannedWorkoutId = null)
    {
        if (string.IsNullOrWhiteSpace(targetPage))
            return;

        PendingRequest = new NotificationNavigationRequest(
            targetPage.Trim().ToLowerInvariant(),
            plannedSport ?? string.Empty,
            plannedWorkoutId ?? string.Empty);
    }

    public static async Task HandlePendingAsync()
    {
        if (PendingRequest == null || Shell.Current == null)
            return;

        var request = PendingRequest;
        PendingRequest = null;

        if (request.TargetPage == TargetYouPage)
        {
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//main/you"));
            return;
        }

        if (request.TargetPage == TargetRecordPage)
        {
            var route = "//main/record";
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.PlannedSport))
                queryParts.Add($"plannedSport={Uri.EscapeDataString(request.PlannedSport)}");
            if (!string.IsNullOrWhiteSpace(request.PlannedWorkoutId))
                queryParts.Add($"plannedWorkoutId={Uri.EscapeDataString(request.PlannedWorkoutId)}");
            if (queryParts.Count > 0)
                route = $"{route}?{string.Join("&", queryParts)}";

            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));
        }
    }
}
