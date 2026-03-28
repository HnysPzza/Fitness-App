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
        if (action == ActionPauseResume)
            WorkoutSessionManager.RequestCommand(WorkoutCommand.TogglePauseResume);
        else if (action == ActionStop)
            WorkoutSessionManager.RequestCommand(WorkoutCommand.Stop);

        var refreshIntent = new Intent(context, typeof(WorkoutTrackingForegroundService));
        refreshIntent.SetAction(action == ActionStop
            ? WorkoutTrackingForegroundService.ActionStopService
            : WorkoutTrackingForegroundService.ActionRefresh);

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            context.StartForegroundService(refreshIntent);
        else
            context.StartService(refreshIntent);
    }
}
