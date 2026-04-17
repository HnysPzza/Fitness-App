using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Fitness_App.Services;

namespace Fitness_App.Platforms.Android;

[Service(Enabled = true, Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public sealed class WorkoutTrackingForegroundService : Service
{
    public const string ChannelId = "workout_tracking";
    public const int NotificationId = 3107;
    public const string ActionRefresh = "Fitness_App.action.WORKOUT_REFRESH";
    public const string ActionStopService = "Fitness_App.action.WORKOUT_STOP_SERVICE";

    /// <summary>
    /// Self-updating handler that refreshes the notification every second
    /// even when the app is in the background.
    /// </summary>
    private Handler? _handler;
    private Java.Lang.Runnable? _tickRunnable;
    private const int TickIntervalMs = 1000;

    public override void OnCreate()
    {
        base.OnCreate();
        EnsureChannel();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action ?? ActionRefresh;
        if (action == ActionStopService)
        {
            StopSelfUpdating();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                StopForeground(StopForegroundFlags.Remove);
            else
                StopForeground(true);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var snapshot = WorkoutSessionManager.GetSnapshot();
        if (!snapshot.IsActive)
        {
            StopSelfUpdating();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                StopForeground(StopForegroundFlags.Remove);
            else
                StopForeground(true);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var notification = BuildNotification(snapshot);
        StartForeground(NotificationId, notification);

        // Start the self-updating timer so the notification refreshes
        // every second even when the app is backgrounded
        StartSelfUpdating();

        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        StopSelfUpdating();
        base.OnDestroy();
    }

    /// <summary>
    /// Starts a Handler-based periodic loop that refreshes the notification
    /// content (elapsed time, distance, speed) every second. This runs
    /// independently of the MAUI UI thread so it continues when the app
    /// is minimised or the screen is off.
    /// </summary>
    private void StartSelfUpdating()
    {
        if (_handler != null)
            return; // already running

        _handler = new Handler(Looper.MainLooper!);
        _tickRunnable = new Java.Lang.Runnable(OnSelfUpdateTick);
        _handler.PostDelayed(_tickRunnable, TickIntervalMs);
    }

    private void StopSelfUpdating()
    {
        if (_handler != null && _tickRunnable != null)
        {
            _handler.RemoveCallbacks(_tickRunnable);
        }
        _handler?.Dispose();
        _tickRunnable?.Dispose();
        _handler = null;
        _tickRunnable = null;
    }

    private void OnSelfUpdateTick()
    {
        try
        {
            var snapshot = WorkoutSessionManager.GetSnapshot();
            if (!snapshot.IsActive)
            {
                // Workout ended; tear down.
                StopSelfUpdating();
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                    StopForeground(StopForegroundFlags.Remove);
                else
                    StopForeground(true);
                StopSelf();
                return;
            }

            var notification = BuildNotification(snapshot);
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.Notify(NotificationId, notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkoutFGS] Tick error: {ex.Message}");
        }

        // Schedule the next tick
        _handler?.PostDelayed(_tickRunnable!, TickIntervalMs);
    }

    private Notification BuildNotification(WorkoutSessionSnapshot snapshot)
    {
        var pendingFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            pendingFlags |= PendingIntentFlags.Immutable;

        var launchIntent = new Intent(this, typeof(MainActivity));
        launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);
        launchIntent.PutExtra(NotificationNavigationService.ExtraTargetPage, NotificationNavigationService.TargetRecordPage);
        launchIntent.PutExtra(NotificationNavigationService.ExtraPlannedSport, snapshot.Sport);
        var launchPendingIntent = PendingIntent.GetActivity(
            this,
            0,
            launchIntent,
            pendingFlags);

        var pauseResumeIntent = new Intent(this, typeof(WorkoutNotificationActionReceiver));
        pauseResumeIntent.SetAction(WorkoutNotificationActionReceiver.ActionPauseResume);
        var pauseResumePendingIntent = PendingIntent.GetBroadcast(
            this,
            1,
            pauseResumeIntent,
            pendingFlags);

        var stopIntent = new Intent(this, typeof(WorkoutNotificationActionReceiver));
        stopIntent.SetAction(WorkoutNotificationActionReceiver.ActionStop);
        var stopPendingIntent = PendingIntent.GetBroadcast(
            this,
            2,
            stopIntent,
            pendingFlags);

        var elapsed = snapshot.Elapsed;
        string elapsedText = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        string statusText = snapshot.IsPaused ? " PAUSED" : "";
        string metricText = snapshot.IsGpsDependent
            ? $"{snapshot.DistanceKm:F2} km | max {snapshot.MaxSpeedKmh:F1} km/h"
            : $"{snapshot.DistanceKm:F2} km | {snapshot.Sport}";

        return new NotificationCompat.Builder(this, ChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon_round)
            .SetContentTitle($"{snapshot.Sport} in progress{statusText}")
            .SetContentText($"{elapsedText} | {metricText}")
            .SetStyle(new NotificationCompat.BigTextStyle().BigText($"{elapsedText} | {metricText}"))
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetContentIntent(launchPendingIntent)
            .AddAction(0, snapshot.IsPaused ? "Resume" : "Pause", pauseResumePendingIntent)
            .AddAction(0, "Stop", stopPendingIntent)
            .SetUsesChronometer(false)
            .SetCategory(NotificationCompat.CategoryStatus)
            .SetVisibility((int)NotificationVisibility.Public)
            .SetPriority((int)NotificationPriority.Low)
            .Build();
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager == null)
            return;

        var channel = new NotificationChannel(ChannelId, "Workout tracking", NotificationImportance.Low)
        {
            Description = "Shows active workout tracking controls.",
            LockscreenVisibility = NotificationVisibility.Public
        };
        // Disable sound/vibration for the workout channel; it updates every second.
        channel.SetSound(null, null);
        channel.EnableVibration(false);
        manager.CreateNotificationChannel(channel);
    }
}
