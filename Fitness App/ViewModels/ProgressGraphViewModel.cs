using CommunityToolkit.Mvvm.ComponentModel;
using Fitness_App.Models;

namespace Fitness_App.ViewModels;

public partial class ProgressGraphViewModel : ObservableObject
{
    [ObservableProperty]
    private ProgressPeriod _selectedPeriod = ProgressPeriod.Today;

    public ProgressGraphViewModel()
    {
    }
}
