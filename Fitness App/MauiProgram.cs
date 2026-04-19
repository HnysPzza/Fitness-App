using Microsoft.Extensions.Logging;
using Fitness_App.Services;
using Fitness_App.UI.Skia;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Fitness_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialSymbolsOutlined-Regular.ttf", "MaterialSymbols");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler<ShimmerView, SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
                    handlers.AddHandler<MeshGradientView, SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
                })
                .UseSkiaSharp();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddHttpClient("Mapbox", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(25);
            });

            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<ISuggestedLocationsService, SuggestedLocationsService>();
            builder.Services.AddSingleton<IProfileService, ProfileService>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<ISupabaseService, SupabaseService>();
            builder.Services.AddSingleton<IAccountSessionStore, AccountSessionStore>();
            builder.Services.AddSingleton<WorkoutPersistenceService>();
            builder.Services.AddSingleton<StatsService>();
            builder.Services.AddSingleton<IWorkoutPlanService, WorkoutPlanService>();
            builder.Services.AddSingleton<IAppNotificationService, AppNotificationService>();
            builder.Services.AddSingleton<IMapboxRoutingService, MapboxRoutingService>();
            builder.Services.AddSingleton<IActivitySaveNotifier, ActivitySaveNotifier>();

            builder.Services.AddTransient<Pages.HomePageViewModel>();
            builder.Services.AddTransient<ViewModels.ProgressViewModel>();
            builder.Services.AddTransient<ViewModels.YouProgressViewModel>();
            builder.Services.AddTransient<ViewModels.LoginViewModel>();
            builder.Services.AddTransient<ViewModels.RegisterViewModel>();
            builder.Services.AddTransient<ViewModels.EmailVerificationViewModel>();
            builder.Services.AddTransient<ViewModels.ProfileSetupViewModel>();

            builder.Services.AddTransient<Pages.LoadingPage>();
            builder.Services.AddTransient<Pages.LoginPage>();
            builder.Services.AddTransient<Pages.RegisterPage>();
            builder.Services.AddTransient<Pages.EmailVerificationPage>();
            builder.Services.AddTransient<Pages.ProfileSetupPage>();
            builder.Services.AddTransient<Pages.HomePage>();
            builder.Services.AddTransient<Pages.MapsPage>();
            builder.Services.AddTransient<Pages.RecordPage>();
            builder.Services.AddTransient<Pages.ProgressPage>();
            builder.Services.AddTransient<Pages.ActivityDetailPage>();
            builder.Services.AddTransient<Pages.YouPage>();
            builder.Services.AddTransient<Pages.SettingsPage>();
            builder.Services.AddTransient<Pages.GeneralSettingsPage>();
            builder.Services.AddTransient<Pages.EditProfilePage>();


            builder.Services.AddTransient<Pages.WorkoutRemindersPage>();
            builder.Services.AddTransient<Pages.ThemeSelectionPage>();
            builder.Services.AddTransient<Pages.AccentColorSelectionPage>();
            builder.Services.AddTransient<Pages.FontSizeSelectionPage>();
            builder.Services.AddTransient<Pages.LanguageSelectionPage>();
            builder.Services.AddTransient<Pages.UnitsSelectionPage>();
            builder.Services.AddTransient<Pages.DateTimeFormatSelectionPage>();
            builder.Services.AddTransient<Pages.ChangePasswordPage>();
            builder.Services.AddTransient<Pages.TwoFactorAuthPage>();
            builder.Services.AddTransient<Pages.DataSharingPage>();
            builder.Services.AddTransient<Pages.DownloadMyDataPage>();
            builder.Services.AddTransient<Pages.LinkedAppsPage>();
            builder.Services.AddTransient<Pages.WearableDevicesPage>();
            builder.Services.AddTransient<Pages.HelpFaqPage>();
            builder.Services.AddTransient<Pages.SendFeedbackPage>();
            builder.Services.AddTransient<Pages.TermsOfServicePage>();
            builder.Services.AddTransient<Pages.PrivacyPolicyPage>();
            builder.Services.AddTransient<Pages.SwitchAccountPage>();

            return builder.Build();
        }
    }
}
