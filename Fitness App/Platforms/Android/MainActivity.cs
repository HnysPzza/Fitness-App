using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Fitness_App.Platforms.Android;

namespace Fitness_App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AndroidAppNotificationManager.HandleNotificationIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null)
            {
                Intent = intent;
                AndroidAppNotificationManager.HandleNotificationIntent(intent);
                _ = Services.NotificationNavigationService.HandlePendingAsync();
            }
        }
    }
}
