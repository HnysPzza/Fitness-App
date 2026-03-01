namespace Fitness_App.Services
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        void SetTheme(AppTheme theme);
    }

    public sealed class ThemeService : IThemeService
    {
        public AppTheme CurrentTheme => Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

        public void SetTheme(AppTheme theme)
        {
            if (Application.Current is null)
                return;

            Application.Current.UserAppTheme = theme;
        }
    }
}
