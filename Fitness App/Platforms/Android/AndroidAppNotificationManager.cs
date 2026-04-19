using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Fitness_App.Models;
using Fitness_App.Services;
using Microsoft.Maui.ApplicationModel;

namespace Fitness_App.Platforms.Android;

internal static class AndroidAppNotificationManager
{
    public const string ReminderChannelId = "workout_reminders";
    public const string CompletionChannelId = "workout_completion";
    public const int ReminderNotificationId = 3201;
    public const int CompletionNotificationId = 3202;
    public const int ReminderAlarmRequestCode = 4201;
    public const string ReminderAlarmAction = "Fitness_App.action.WORKOUT_REMINDER_ALARM";
    public const int NotificationPermissionRequestCode = 9012;

    public static void EnsureChannels(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager == null)
            return;

        var reminderChannel = new NotificationChannel(ReminderChannelId, "Workout reminders", NotificationImportance.Default)
        {
            Description = "Scheduled reminders for planned workouts.",
            LockscreenVisibility = NotificationVisibility.Public
        };

        var completionChannel = new NotificationChannel(CompletionChannelId, "Workout completion", NotificationImportance.High)
        {
            Description = "Completion alerts for finished recordings.",
            LockscreenVisibility = NotificationVisibility.Public
        };

        manager.CreateNotificationChannel(reminderChannel);
        manager.CreateNotificationChannel(completionChannel);
    }

    public static void RequestNotificationPermission()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            return;

        var activity = Platform.CurrentActivity;
        if (activity == null)
            return;

        if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.PostNotifications) == Permission.Granted)
            return;

        ActivityCompat.RequestPermissions(activity, new[] { Manifest.Permission.PostNotifications }, NotificationPermissionRequestCode);
    }

    public static void RescheduleWorkoutReminder(Context context, ISettingsService settings)
    {
        EnsureChannels(context);
        CancelWorkoutReminder(context);

        var nextReminder = WorkoutReminderScheduleHelper.GetNextReminderDateTimeLocal(settings);
        if (nextReminder == null)
            return;

        var alarmIntent = new Intent(context, typeof(WorkoutReminderAlarmReceiver));
        alarmIntent.SetAction(ReminderAlarmAction);

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            ReminderAlarmRequestCode,
            alarmIntent,
            BuildPendingFlags(PendingIntentFlags.UpdateCurrent));

        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager == null || pendingIntent == null)
            return;

        var triggerAtMillis = new DateTimeOffset(nextReminder.Value).ToUnixTimeMilliseconds();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S && alarmManager.CanScheduleExactAlarms())
        {
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            return;
        }

        alarmManager.Set(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
    }

    public static void CancelWorkoutReminder(Context context)
    {
        var alarmIntent = new Intent(context, typeof(WorkoutReminderAlarmReceiver));
        alarmIntent.SetAction(ReminderAlarmAction);

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            ReminderAlarmRequestCode,
            alarmIntent,
            BuildPendingFlags(PendingIntentFlags.UpdateCurrent));

        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (pendingIntent == null)
            return;

        alarmManager?.Cancel(pendingIntent);
        pendingIntent.Cancel();
    }

    public static void ShowWorkoutReminderNotification(Context context)
    {
        EnsureChannels(context);

        var settings = new SettingsService();
        if (!settings.PushNotificationsEnabled || !settings.WorkoutRemindersEnabled)
            return;

        var workoutPlanService = new WorkoutPlanService();
        var currentPlan = workoutPlanService.GetCurrentPlanAsync().GetAwaiter().GetResult();
        var nextWorkout = workoutPlanService.GetNextPlannedWorkout(currentPlan, DateTime.Now);
        var message = workoutPlanService.BuildReminderMessage(settings.WorkoutReminderMessage, currentPlan, DateTime.Now);

        var launchIntent = new Intent(context, typeof(MainActivity));
        launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);
        launchIntent.PutExtra(NotificationNavigationService.ExtraTargetPage, NotificationNavigationService.TargetRecordPage);
        if (!string.IsNullOrWhiteSpace(nextWorkout?.Sport))
            launchIntent.PutExtra(NotificationNavigationService.ExtraPlannedSport, nextWorkout.Sport);
        if (!string.IsNullOrWhiteSpace(nextWorkout?.Id))
            launchIntent.PutExtra(NotificationNavigationService.ExtraPlannedWorkoutId, nextWorkout.Id);

        var pendingIntent = PendingIntent.GetActivity(
            context,
            ReminderNotificationId,
            launchIntent,
            BuildPendingFlags(PendingIntentFlags.UpdateCurrent));

        const int reminderAccent = unchecked((int)0xFFFC5200);
        var notification = new NotificationCompat.Builder(context, ReminderChannelId)
            // ✅ FIX: drawable required — mipmap causes BadNotificationException crash
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetColor(reminderAccent)
            .SetContentTitle("🏋️ Time to work out!")
            .SetContentText(message)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent)
            .SetCategory(NotificationCompat.CategoryReminder)
            .SetVisibility((int)NotificationVisibility.Public)
            .SetPriority((int)NotificationPriority.Default)
            .Build();

        NotificationManagerCompat.From(context).Notify(ReminderNotificationId, notification);
        RescheduleWorkoutReminder(context, settings);
    }

    public static void ShowRecordingCompleted(Context context, UserActivity activity)
    {
        EnsureChannels(context);

        var duration = TimeSpan.FromTicks(activity.DurationTicks);
        var durationText = duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes:00}:{duration.Seconds:00}";
        var body = activity.DistanceKm > 0
            ? $"{activity.DistanceKm:F2} km in {durationText} | {activity.Sport}"
            : $"{durationText} | {activity.Sport}";

        var launchIntent = new Intent(context, typeof(MainActivity));
        launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);
        launchIntent.PutExtra(NotificationNavigationService.ExtraTargetPage, NotificationNavigationService.TargetYouPage);

        var pendingIntent = PendingIntent.GetActivity(
            context,
            CompletionNotificationId,
            launchIntent,
            BuildPendingFlags(PendingIntentFlags.UpdateCurrent));

        const int completionAccent = unchecked((int)0xFF10B981); // green for completion
        var notification = new NotificationCompat.Builder(context, CompletionChannelId)
            // ✅ FIX: drawable required — mipmap causes BadNotificationException crash
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetColor(completionAccent)
            .SetContentTitle("✅ Workout saved!")
            .SetContentText(body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent)
            .SetCategory(NotificationCompat.CategoryStatus)
            .SetVisibility((int)NotificationVisibility.Public)
            .SetPriority((int)NotificationPriority.High)
            .Build();

        NotificationManagerCompat.From(context).Notify(CompletionNotificationId, notification);
    }

    public static void HandleNotificationIntent(Intent? intent)
    {
        if (intent == null)
            return;

        var targetPage = intent.GetStringExtra(NotificationNavigationService.ExtraTargetPage);
        if (string.IsNullOrWhiteSpace(targetPage))
            return;

        NotificationNavigationService.SetPending(
            targetPage,
            intent.GetStringExtra(NotificationNavigationService.ExtraPlannedSport),
            intent.GetStringExtra(NotificationNavigationService.ExtraPlannedWorkoutId));
    }

    private static PendingIntentFlags BuildPendingFlags(PendingIntentFlags baseFlags)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            return baseFlags | PendingIntentFlags.Immutable;

        return baseFlags;
    }
}
