namespace Fitness_App.Pages;

public partial class LoadingPage : ContentPage
{
    private CancellationTokenSource? _animationCts;

    public LoadingPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartLoadingAnimation();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopLoadingAnimation();
    }

    private void StartLoadingAnimation()
    {
        StopLoadingAnimation();

        _animationCts = new CancellationTokenSource();
        _ = AnimateBrandMarkAsync(_animationCts.Token);
        _ = AnimateProgressAsync(_animationCts.Token);
    }

    private void StopLoadingAnimation()
    {
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;
    }

    private async Task AnimateBrandMarkAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.WhenAll(
                    LogoOrbit.RotateToAsync(360, 1600, Easing.Linear),
                    LogoGlow.FadeToAsync(0.34, 800, Easing.CubicInOut),
                    LogoBadge.ScaleToAsync(1.06, 800, Easing.CubicInOut));

                LogoOrbit.Rotation = 0;

                await Task.WhenAll(
                    LogoGlow.FadeToAsync(0.12, 800, Easing.CubicInOut),
                    LogoBadge.ScaleToAsync(1.0, 800, Easing.CubicInOut));
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task AnimateProgressAsync(CancellationToken cancellationToken)
    {
        var statusMessages = new[]
        {
            "Loading your session...",
            "Preparing your routes...",
            "Syncing your progress..."
        };

        try
        {
            var index = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                LoadingStatusLabel.Text = statusMessages[index % statusMessages.Length];
                index++;

                await SessionProgress.ProgressTo(0.82, 850, Easing.CubicOut);
                await SessionProgress.ProgressTo(0.96, 450, Easing.CubicInOut);
                SessionProgress.Progress = 0.12;
                await Task.Delay(120, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}
