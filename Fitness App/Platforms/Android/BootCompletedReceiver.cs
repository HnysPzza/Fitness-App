using Android.App;
using Android.Content;
using Fitness_App.Services;

namespace Fitness_App.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
public sealed class BootCompletedReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
            return;

        var settings = new SettingsService();
        AndroidAppNotificationManager.RescheduleWorkoutReminder(context, settings);
    }
}
