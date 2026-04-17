using Fitness_App.Models;

namespace Fitness_App.Services;

public interface IAppNotificationService
{
    Task InitializeAsync();
    Task RequestPermissionAsync();
    Task RefreshWorkoutReminderScheduleAsync();
    Task ShowRecordingCompletedAsync(UserActivity activity);
}

public sealed class AppNotificationService : IAppNotificationService
{
    public Task InitializeAsync()
    {
#if ANDROID
        Platforms.Android.AndroidAppNotificationManager.EnsureChannels(global::Android.App.Application.Context);
#endif
        return Task.CompletedTask;
    }

    public Task RequestPermissionAsync()
    {
#if ANDROID
        Platforms.Android.AndroidAppNotificationManager.RequestNotificationPermission();
#endif
        return Task.CompletedTask;
    }

    public Task RefreshWorkoutReminderScheduleAsync()
    {
#if ANDROID
        var settings = new SettingsService();
        Platforms.Android.AndroidAppNotificationManager.RescheduleWorkoutReminder(global::Android.App.Application.Context, settings);
#endif
        return Task.CompletedTask;
    }

    public Task ShowRecordingCompletedAsync(UserActivity activity)
    {
#if ANDROID
        Platforms.Android.AndroidAppNotificationManager.ShowRecordingCompleted(global::Android.App.Application.Context, activity);
#endif
        return Task.CompletedTask;
    }
}
