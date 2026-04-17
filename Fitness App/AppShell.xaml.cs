namespace Fitness_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("settings", typeof(Pages.SettingsPage));
            Routing.RegisterRoute("generalsettings", typeof(Pages.GeneralSettingsPage));
            Routing.RegisterRoute("editprofile", typeof(Pages.EditProfilePage));

            Routing.RegisterRoute("activitydetail", typeof(Pages.ActivityDetailPage));

            Routing.RegisterRoute("workoutreminders", typeof(Pages.WorkoutRemindersPage));
            Routing.RegisterRoute("themeselection", typeof(Pages.ThemeSelectionPage));
            Routing.RegisterRoute("accentcolor", typeof(Pages.AccentColorSelectionPage));
            Routing.RegisterRoute("fontsize", typeof(Pages.FontSizeSelectionPage));
            Routing.RegisterRoute("language", typeof(Pages.LanguageSelectionPage));
            Routing.RegisterRoute("units", typeof(Pages.UnitsSelectionPage));
            Routing.RegisterRoute("datetime", typeof(Pages.DateTimeFormatSelectionPage));
            Routing.RegisterRoute("changepassword", typeof(Pages.ChangePasswordPage));
            Routing.RegisterRoute("twofactor", typeof(Pages.TwoFactorAuthPage));
            Routing.RegisterRoute("datasharing", typeof(Pages.DataSharingPage));
            Routing.RegisterRoute("downloaddata", typeof(Pages.DownloadMyDataPage));
            Routing.RegisterRoute("linkedapps", typeof(Pages.LinkedAppsPage));
            Routing.RegisterRoute("wearabledevices", typeof(Pages.WearableDevicesPage));
            Routing.RegisterRoute("helpfaq", typeof(Pages.HelpFaqPage));
            Routing.RegisterRoute("sendfeedback", typeof(Pages.SendFeedbackPage));
            Routing.RegisterRoute("termsofservice", typeof(Pages.TermsOfServicePage));
            Routing.RegisterRoute("privacypolicy", typeof(Pages.PrivacyPolicyPage));
            Routing.RegisterRoute("switchaccount", typeof(Pages.SwitchAccountPage));
        }
    }
}
