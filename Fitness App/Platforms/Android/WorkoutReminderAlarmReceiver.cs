using Android.App;
using Android.Content;

namespace Fitness_App.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { AndroidAppNotificationManager.ReminderAlarmAction })]
public sealed class WorkoutReminderAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
            return;

        AndroidAppNotificationManager.ShowWorkoutReminderNotification(context);
    }
}
