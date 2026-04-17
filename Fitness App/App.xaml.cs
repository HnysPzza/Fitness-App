using Microsoft.Extensions.DependencyInjection;
using Fitness_App.Services;

namespace Fitness_App
{
    public partial class App : Application
    {
        private readonly ISupabaseService _auth;
        private readonly IProfileService _profile;
        private readonly IAppNotificationService _notifications;

        public App(ISupabaseService auth, IProfileService profile, IAppNotificationService notifications)
        {
            InitializeComponent();
            _auth = auth;
            _profile = profile;
            _notifications = notifications;

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var msg = $"[UNHANDLED] {e.ExceptionObject}";
                System.Diagnostics.Debug.WriteLine(msg);
                PersistCrashLog(msg);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                var msg = $"[UNOBSERVED TASK] {e.Exception}";
                System.Diagnostics.Debug.WriteLine(msg);
                PersistCrashLog(msg);
                e.SetObserved();
            };
        }

        private static void PersistCrashLog(string message)
        {
            try
            {
                var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                Preferences.Default.Set("last_crash_log", log);
            }
            catch { }
        }

        public static async Task ShowCrashLogIfAnyAsync()
        {
            try
            {
                var crashLog = Preferences.Default.Get("last_crash_log", "");
                if (string.IsNullOrEmpty(crashLog))
                    return;

                Preferences.Default.Remove("last_crash_log");

                string androidCrash = "";
#if ANDROID
                var prefs = Android.App.Application.Context.GetSharedPreferences("crash_log", Android.Content.FileCreationMode.Private);
                androidCrash = prefs?.GetString("last_crash", "") ?? "";
                if (!string.IsNullOrEmpty(androidCrash))
                    prefs?.Edit()?.Remove("last_crash")?.Apply();
#endif

                var fullLog = string.IsNullOrEmpty(androidCrash)
                    ? crashLog
                    : $"{crashLog}\n\n--- Android ---\n{androidCrash}";

                if (Shell.Current?.CurrentPage != null)
                {
                    await Shell.Current.CurrentPage.DisplayAlert(
                        "Previous Crash Detected",
                        fullLog.Length > 500 ? fullLog[..500] + "…" : fullLog,
                        "OK");
                }
            }
            catch { }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

            // Use Dispatcher instead of fire-and-forget Task so we wait until
            // the Window is actually attached and Shell.Current is non-null.
            // In Release mode the JIT is faster and the race window is wider.
            window.Created += (_, _) => _ = RunSessionGuardAsync();

            return window;
        }

        private async Task RunSessionGuardAsync()
        {
            // Wait until Shell is attached and navigation is possible.
            // In Release/AOT Shell.Current may still be null right after window.Created.
            var retries = 0;
            while (Shell.Current == null && retries++ < 20)
                await Task.Delay(50);

            if (Shell.Current == null)
            {
                System.Diagnostics.Debug.WriteLine("[SessionGuard] Shell.Current never became available — aborting.");
                return;
            }

            try
            {
                await _notifications.InitializeAsync();
                await _auth.InitializeAsync();

                if (!_auth.IsLoggedIn)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Shell.Current.GoToAsync("//login"));
                    return;
                }

                var supaProfile = await _auth.GetCurrentProfileAsync();

                if (supaProfile == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Shell.Current.GoToAsync("//profilesetup"));
                }
                else
                {
                    _profile.SyncFromSupabase(supaProfile);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Shell.Current.GoToAsync("//home"));

                    await Task.Delay(150);
                    await NotificationNavigationService.HandlePendingAsync();
                    await Task.Delay(1000);
                    await ShowCrashLogIfAnyAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionGuard] {ex.Message}");
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Shell.Current?.GoToAsync("//login"));
                }
                catch { }
            }
        }
    }
}