using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Fitness_App.Models;
using Fitness_App.Services;

namespace Fitness_App.Pages
{
    public partial class HomePageViewModel : ObservableObject
    {
        private readonly ISuggestedLocationsService _locationsService;
        private readonly StatsService _statsService;
        private readonly IWorkoutPlanService _workoutPlanService;
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private string _weekDistance = "0.0";

        [ObservableProperty]
        private string _weekActivities = "0";

        [ObservableProperty]
        private string _weekTime = "0h";

        [ObservableProperty]
        private string _weekDeltaText = "Keep moving this week";

        [ObservableProperty]
        private bool _weekDeltaPositive;

        [ObservableProperty]
        private string _reminderSummary = "Reminders off";

        [ObservableProperty]
        private bool _hasReminder;

        [ObservableProperty]
        private int _currentStreak;

        [ObservableProperty]
        private string _currentPlanTitle = "No workout plan yet";

        [ObservableProperty]
        private string _currentPlanSummary = "Use a template or add a custom workout from Home.";

        [ObservableProperty]
        private string _currentPlanDuration = string.Empty;

        [ObservableProperty]
        private string _currentPlanProgress = "Create plan";

        [ObservableProperty]
        private bool _hasWorkoutPlan;

        public WorkoutPlan? CurrentPlan { get; private set; }

        public ObservableCollection<SuggestedLocation> SuggestedLocations { get; }

        public ObservableCollection<PlannedWorkout> CurrentPlanWorkouts { get; } = new();

        public ObservableCollection<WorkoutPlanTemplate> PlanTemplates { get; } = new();

        public HomePageViewModel(
            ISuggestedLocationsService locationsService,
            StatsService statsService,
            IWorkoutPlanService workoutPlanService,
            ISettingsService settingsService)
        {
            _locationsService = locationsService;
            _statsService = statsService;
            _workoutPlanService = workoutPlanService;
            _settingsService = settingsService;
            SuggestedLocations = new ObservableCollection<SuggestedLocation>(_locationsService.GetAll());
        }

        public void RefreshSuggestedLocations()
        {
            SuggestedLocations.Clear();
            foreach (var location in _locationsService.GetAll())
            {
                SuggestedLocations.Add(location);
            }
        }

        public async Task LoadAsync()
        {
            RefreshSuggestedLocations();
            RefreshReminderSummary();

            var loadTasks = new Task[]
            {
                LoadWeeklySummaryAsync(),
                LoadWorkoutPlanAsync(),
                LoadTemplatesAsync(),
                LoadStreakAsync()
            };

            await Task.WhenAll(loadTasks);
        }

        public void RefreshReminderSummary()
        {
            HasReminder = _settingsService.PushNotificationsEnabled && _settingsService.WorkoutRemindersEnabled;
            ReminderSummary = WorkoutReminderScheduleHelper.BuildSummaryLabel(_settingsService);
        }

        private async Task LoadWeeklySummaryAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                int offset = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
                if (offset < 0) offset += 7;

                var monday = today.AddDays(-offset);
                var previousMonday = monday.AddDays(-7);

                var currentWeekTask = _statsService.GetStatsInRangeAsync(monday, monday.AddDays(7));
                var previousWeekTask = _statsService.GetStatsInRangeAsync(previousMonday, monday);
                await Task.WhenAll(currentWeekTask, previousWeekTask);

                var currentWeek = await currentWeekTask;
                var previousWeek = await previousWeekTask;

                WeekDistance = currentWeek.TotalKm.ToString("F1");
                WeekActivities = currentWeek.TotalActivities.ToString();
                WeekTime = currentWeek.TotalTime.TotalHours >= 1
                    ? $"{(int)currentWeek.TotalTime.TotalHours}h"
                    : currentWeek.TotalTime.TotalMinutes >= 1
                        ? $"{(int)currentWeek.TotalTime.TotalMinutes}m"
                        : "0h";

                var deltaKm = currentWeek.TotalKm - previousWeek.TotalKm;
                WeekDeltaPositive = deltaKm >= 0;
                WeekDeltaText = previousWeek.TotalKm <= 0 && currentWeek.TotalKm <= 0
                    ? "No distance logged yet"
                    : previousWeek.TotalKm <= 0
                        ? $"{currentWeek.TotalKm:F1} km logged this week"
                        : $"{(deltaKm >= 0 ? "+" : string.Empty)}{deltaKm:F1} km vs last week";
            }
            catch
            {
                WeekDistance = "0.0";
                WeekActivities = "0";
                WeekTime = "0h";
                WeekDeltaText = "Keep moving this week";
                WeekDeltaPositive = true;
            }
        }

        private async Task LoadWorkoutPlanAsync()
        {
            CurrentPlan = await _workoutPlanService.GetCurrentPlanAsync();
            CurrentPlanWorkouts.Clear();

            if (CurrentPlan == null || CurrentPlan.Workouts.Count == 0)
            {
                HasWorkoutPlan = false;
                CurrentPlanTitle = "No workout plan yet";
                CurrentPlanSummary = "Use a template or add a custom workout from Home.";
                CurrentPlanDuration = string.Empty;
                CurrentPlanProgress = "Create plan";
                return;
            }

            HasWorkoutPlan = true;
            CurrentPlanTitle = CurrentPlan.Title;
            CurrentPlanSummary = CurrentPlan.IsTemplateBased
                ? "Template-based plan"
                : "Custom plan managed from Home";
            CurrentPlanDuration = $"{CurrentPlan.StartDate:MMM d} - {CurrentPlan.EndDate:MMM d}";
            CurrentPlanProgress = CurrentPlan.ProgressText;

            foreach (var workout in CurrentPlan.Workouts.OrderBy(workout => workout.ScheduledDate).Take(5))
            {
                CurrentPlanWorkouts.Add(workout);
            }
        }

        private async Task LoadTemplatesAsync()
        {
            var templates = await _workoutPlanService.GetTemplatesAsync();
            PlanTemplates.Clear();
            foreach (var template in templates)
            {
                PlanTemplates.Add(template);
            }
        }

        private async Task LoadStreakAsync()
        {
            CurrentStreak = await _statsService.GetCurrentStreakAsync();
        }
    }
}
