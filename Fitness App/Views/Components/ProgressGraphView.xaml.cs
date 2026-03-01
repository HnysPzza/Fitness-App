using Fitness_App.Models;
using Fitness_App.ViewModels;

namespace Fitness_App.Views.Components;

public partial class ProgressGraphView : ContentView
{
    private ProgressGraphViewModel? ViewModel => BindingContext as ProgressGraphViewModel;

    public ProgressGraphView()
    {
        InitializeComponent();
    }

    private async void OnTodayTapped(object? sender, EventArgs e)
    {
        if (ViewModel == null) return;
        
        ViewModel.SelectedPeriod = ProgressPeriod.Today;
        await AnimateCard();
        
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private async void OnWeekTapped(object? sender, EventArgs e)
    {
        if (ViewModel == null) return;
        
        ViewModel.SelectedPeriod = ProgressPeriod.Week;
        await AnimateCard();
        
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private async void OnMonthTapped(object? sender, EventArgs e)
    {
        if (ViewModel == null) return;
        
        ViewModel.SelectedPeriod = ProgressPeriod.Month;
        await AnimateCard();
        
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private async Task AnimateCard()
    {
        // Subtle scale animation - non-blocking
        await ProgressCard.ScaleTo(0.98, 80, Easing.CubicIn);
        await ProgressCard.ScaleTo(1.0, 120, Easing.CubicOut);
    }
}
