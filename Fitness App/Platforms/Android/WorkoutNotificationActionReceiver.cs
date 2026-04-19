using Android.App;
using Android.Content;
using Fitness_App.Services;

namespace Fitness_App.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { ActionPauseResume, ActionStop })]
public sealed class WorkoutNotificationActionReceiver : BroadcastReceiver
{
    public const string ActionPauseResume = "Fitness_App.action.WORKOUT_PAUSE_RESUME";
    public const string ActionStop = "Fitness_App.action.WORKOUT_STOP";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
            return;

        var action = intent?.Action;
        var commandDelivered = false;
        if (action == ActionPauseResume)
            commandDelivered = WorkoutSessionManager.RequestCommand(WorkoutCommand.TogglePauseResume);
        else if (action == ActionStop)
            commandDelivered = WorkoutSessionManager.RequestCommand(WorkoutCommand.Stop);

        if (action == ActionStop)
        {
            WorkoutSessionManager.EnterFinishPending();
            try
            {
                new WorkoutPersistenceService().MarkActiveSessionFinishPendingAsync().GetAwaiter().GetResult();
            }
            catch
            {
            }

            context.StopService(new Intent(context, typeof(WorkoutTrackingForegroundService)));

            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            launchIntent.PutExtra(NotificationNavigationService.ExtraTargetPage, NotificationNavigationService.TargetRecordPage);
            context.StartActivity(launchIntent);
            return;
        }

        if (action != ActionPauseResume)
            return;

        if (!commandDelivered)
        {
            var snapshot = WorkoutSessionManager.GetSnapshot();
            if (snapshot.IsActive)
            {
                if (snapshot.IsPaused)
                    WorkoutSessionManager.Resume();
                else
                    WorkoutSessionManager.Pause();
            }
        }

        var refreshIntent = new Intent(context, typeof(WorkoutTrackingForegroundService));
        refreshIntent.SetAction(WorkoutTrackingForegroundService.ActionRefresh);

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            context.StartForegroundService(refreshIntent);
        else
            context.StartService(refreshIntent);
    }
}
