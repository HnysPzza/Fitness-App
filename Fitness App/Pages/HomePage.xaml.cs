using Fitness_App.Services;

namespace Fitness_App.Pages
{
    public partial class HomePage : ContentPage
    {
        private IProfileService? _profileService;

        public HomePage()
        {
            InitializeComponent();

            BindingContext = Application.Current?.Handler?.MauiContext?.Services.GetService<HomePageViewModel>();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();

            if (_profileService != null)
            {
                _profileService.ProfileChanged -= OnProfileChanged;
                _profileService.ProfileChanged += OnProfileChanged;
                UpdateGreeting();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Get profile service and subscribe to changes
            _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();

            if (_profileService != null)
            {
                _profileService.ProfileChanged -= OnProfileChanged;
                _profileService.ProfileChanged += OnProfileChanged;
                UpdateGreeting();
            }

            // Animate hero card entrance
            if (HeroCard != null)
            {
                HeroCard.Scale = 0.9;
                await Task.Delay(100);
                await Task.WhenAll(
                    HeroCard.FadeToAsync(1, 500, Easing.CubicOut),
                    HeroCard.ScaleToAsync(1, 600, Easing.SpringOut)
                );
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_profileService != null)
                _profileService.ProfileChanged -= OnProfileChanged;
        }

        private void OnProfileChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(UpdateGreeting);
        }

        private void UpdateGreeting()
        {
            if (_profileService == null) return;

            GreetingLabel.Text = _profileService.GetGreeting();
            HomeUserNameLabel.Text = $"{_profileService.UserName} 🔥";
        }

        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }

            try
            {
                if (Shell.Current is null)
                    return;

                await Shell.Current.GoToAsync("settings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private async void OnLocationCardTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }

            if (sender is not VisualElement view)
                return;

            await view.ScaleToAsync(0.98, 70, Easing.CubicOut);
            await view.ScaleToAsync(1.0, 120, Easing.CubicOut);
        }
    }
}
