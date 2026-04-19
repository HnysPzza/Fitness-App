using Fitness_App.Services;
using Fitness_App.Models;

namespace Fitness_App.Pages
{
    public partial class HomePage : ContentPage
    {
        private IProfileService? _profileService;
        private IMapboxRoutingService? _routingService;
        private IAppNotificationService? _notificationService;
        private IWorkoutPlanService? _workoutPlanService;
        private CancellationTokenSource? _deferredRefreshCts;

        public HomePage()
        {
            InitializeComponent();

            BindingContext = Application.Current?.Handler?.MauiContext?.Services.GetService<HomePageViewModel>();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            ResolveServices();

            if (_profileService != null)
            {
                _profileService.ProfileChanged -= OnProfileChanged;
                _profileService.ProfileChanged += OnProfileChanged;
                UpdateGreeting();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ResolveServices();

            if (_profileService != null)
            {
                _profileService.ProfileChanged -= OnProfileChanged;
                _profileService.ProfileChanged += OnProfileChanged;
                UpdateGreeting();
            }

            if (BindingContext is HomePageViewModel viewModel)
            {
                QueueDeferredRefresh(viewModel);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_profileService != null)
                _profileService.ProfileChanged -= OnProfileChanged;

            _deferredRefreshCts?.Cancel();
            _deferredRefreshCts?.Dispose();
            _deferredRefreshCts = null;
        }

        private void OnProfileChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(UpdateGreeting);
        }

        private void UpdateGreeting()
        {
            if (_profileService == null) return;

            GreetingLabel.Text = _profileService.GetGreeting();
            HomeUserNameLabel.Text = _profileService.UserName;
        }

        private void ResolveServices()
        {
            var services = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
            if (services == null)
                return;

            _profileService ??= services.GetService<IProfileService>();
            _routingService ??= services.GetService<IMapboxRoutingService>();
            _notificationService ??= services.GetService<IAppNotificationService>();
            _workoutPlanService ??= services.GetService<IWorkoutPlanService>();
        }

        private void QueueDeferredRefresh(HomePageViewModel viewModel)
        {
            _deferredRefreshCts?.Cancel();
            _deferredRefreshCts?.Dispose();

            var cts = new CancellationTokenSource();
            _deferredRefreshCts = cts;
            _ = RunDeferredRefreshAsync(viewModel, cts.Token);
        }

        private async Task RunDeferredRefreshAsync(HomePageViewModel viewModel, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(120, cancellationToken);
                await viewModel.LoadAsync();

                if (cancellationToken.IsCancellationRequested || _notificationService == null)
                    return;

                await _notificationService.RequestPermissionAsync();
                await _notificationService.RefreshWorkoutReminderScheduleAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage] Deferred refresh: {ex.Message}");
            }
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

                await Shell.Current.GoToAsync("generalsettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private async void OnReminderTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }

            if (Shell.Current == null)
                return;

            await Shell.Current.GoToAsync("workoutreminders");
        }

        private async void OnStartWorkoutClicked(object? sender, EventArgs e)
        {
            if (BindingContext is HomePageViewModel viewModel)
            {
                var nextWorkout = viewModel.CurrentPlanWorkouts.FirstOrDefault(workout => !workout.IsCompleted);
                if (nextWorkout != null)
                {
                    await NavigateToRecordAsync(nextWorkout.Sport, nextWorkout.Id);
                    return;
                }
            }

            await NavigateToRecordAsync(string.Empty);
        }

        private async void OnPlanTemplateTapped(object? sender, TappedEventArgs e)
        {
            if (_workoutPlanService == null || _notificationService == null)
                return;

            if (sender is not VisualElement view || view.BindingContext is not WorkoutPlanTemplate template)
                return;

            await _workoutPlanService.CreateTemplatePlanAsync(template.Id, DateTime.Today);

            if (BindingContext is HomePageViewModel viewModel)
            {
                await viewModel.LoadAsync();
            }

            await _notificationService.RefreshWorkoutReminderScheduleAsync();
        }

        private async void OnCreateCustomPlanClicked(object? sender, EventArgs e)
        {
            if (_workoutPlanService == null || _notificationService == null || BindingContext is not HomePageViewModel viewModel)
                return;

            var title = await DisplayPromptAsync("Plan title", "Name your plan:", initialValue: "Custom Plan");
            if (title == null)
                return;

            var durationText = await DisplayPromptAsync("Duration", "How many days should this plan run?", initialValue: "7", keyboard: Keyboard.Numeric);
            if (!int.TryParse(durationText, out var durationDays) || durationDays <= 0)
                durationDays = 7;
            durationDays = Math.Min(durationDays, 30);

            var sport = await DisplayActionSheet("Choose a sport", "Cancel", null, "Run", "Walk", "Cycling", "Gym Workout", "Yoga", "Hike");
            if (sport == null || sport == "Cancel")
                return;

            var sessionsText = await DisplayPromptAsync("Sessions", "How many workouts in this plan?", initialValue: "3", keyboard: Keyboard.Numeric);
            if (!int.TryParse(sessionsText, out var sessionCount) || sessionCount <= 0)
                sessionCount = 3;
            sessionCount = Math.Min(sessionCount, durationDays);

            var goalType = await DisplayActionSheet("Workout target", "Cancel", null, "Distance", "Duration");
            if (goalType == null || goalType == "Cancel")
                return;

            var targetText = await DisplayPromptAsync(
                goalType,
                goalType == "Distance" ? "Target km per workout" : "Target minutes per workout",
                initialValue: goalType == "Distance" ? "5" : "30",
                keyboard: Keyboard.Numeric);

            var startDate = DateTime.Today;
            var workouts = new List<PlannedWorkout>();
            for (var index = 0; index < sessionCount; index++)
            {
                var dayOffset = sessionCount == 1
                    ? 0
                    : (int)Math.Round(index * (durationDays - 1d) / Math.Max(1, sessionCount - 1d));

                var workout = new PlannedWorkout
                {
                    Title = $"{sport} Session {index + 1}",
                    Sport = sport,
                    ScheduledDate = startDate.AddDays(dayOffset)
                };

                if (goalType == "Distance" && double.TryParse(targetText, out var distanceKm))
                    workout.PlannedDistanceKm = distanceKm;
                else if (goalType == "Duration" && int.TryParse(targetText, out var durationMinutes))
                    workout.PlannedDurationMinutes = durationMinutes;

                workouts.Add(workout);
            }

            await _workoutPlanService.SaveCustomPlanAsync(title, startDate, durationDays, workouts);
            await viewModel.LoadAsync();
            await _notificationService.RefreshWorkoutReminderScheduleAsync();
        }

        private async void OnPlanWorkoutTapped(object? sender, TappedEventArgs e)
        {
            if (_workoutPlanService == null || _notificationService == null || BindingContext is not HomePageViewModel viewModel)
                return;

            if (sender is not VisualElement view || view.BindingContext is not PlannedWorkout workout)
                return;

            var action = await DisplayActionSheet(
                workout.Title,
                "Cancel",
                null,
                "Start workout",
                workout.IsCompleted ? "Mark incomplete" : "Mark complete");

            if (action == "Start workout")
            {
                await NavigateToRecordAsync(workout.Sport, workout.Id);
                return;
            }

            if (action == "Mark complete" || action == "Mark incomplete")
            {
                await _workoutPlanService.ToggleWorkoutCompletionAsync(workout.Id);
                await viewModel.LoadAsync();
                await _notificationService.RefreshWorkoutReminderScheduleAsync();
            }
        }

        private async Task NavigateToRecordAsync(string sport, string? plannedWorkoutId = null)
        {
            if (Shell.Current == null)
                return;

            var route = "//main/record";
            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(sport))
                queryParts.Add($"plannedSport={Uri.EscapeDataString(sport)}");
            if (!string.IsNullOrWhiteSpace(plannedWorkoutId))
                queryParts.Add($"plannedWorkoutId={Uri.EscapeDataString(plannedWorkoutId)}");

            if (queryParts.Count > 0)
                route = $"{route}?{string.Join("&", queryParts)}";

            await Shell.Current.GoToAsync(route);
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

            if (view.BindingContext is not SuggestedLocation location)
                return;

            await NavigateToSuggestedRouteAsync(location);
        }

        private async Task NavigateToSuggestedRouteAsync(SuggestedLocation location)
        {
            if (_routingService == null || Shell.Current == null)
                return;

            try
            {
                var origin = await ResolveStartLocationAsync();
                var resolvedDestination = await _routingService.ResolveDestinationAsync(
                    location.Name,
                    location.DestLng,
                    location.DestLat,
                    origin.Longitude,
                    origin.Latitude);

                var destinationLng = resolvedDestination?.Lng ?? location.DestLng;
                var destinationLat = resolvedDestination?.Lat ?? location.DestLat;
                var route = await _routingService.GetDirectionsAsync(
                    origin.Longitude,
                    origin.Latitude,
                    destinationLng,
                    destinationLat,
                    location.DirectionsProfile);

                string coordinatesJson = route != null
                    ? JsonSerializer.Serialize(route.Coordinates.Select(point => new[] { point.Lng, point.Lat }))
                    : JsonSerializer.Serialize(new[]
                    {
                        new[] { origin.Longitude, origin.Latitude },
                        new[] { destinationLng, destinationLat }
                    });

                MapNavigationState.PendingSuggestedRoute = new ShowSuggestedRouteOnMapMessage(
                    string.IsNullOrWhiteSpace(location.RouteId) ? Guid.NewGuid().ToString("N") : location.RouteId,
                    coordinatesJson,
                    "#FC5200",
                    location.Name,
                    destinationLng,
                    destinationLat);

                await Shell.Current.GoToAsync("//main/maps");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage.NavigateToSuggestedRouteAsync] {ex.Message}");
            }
        }

        private async Task<Location> ResolveStartLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status == PermissionStatus.Granted)
                {
                    var current = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(8)
                    });

                    if (current != null)
                    {
                        Preferences.Default.Set("last_lng", current.Longitude);
                        Preferences.Default.Set("last_lat", current.Latitude);
                        return current;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage.ResolveStartLocationAsync] {ex.Message}");
            }

            return new Location(
                Preferences.Default.Get("last_lat", 10.315),
                Preferences.Default.Get("last_lng", 123.885));
        }
    }
}
