using Fitness_App.ViewModels;

namespace Fitness_App.Pages
{
    public partial class ProgressPage : ContentPage
    {
        public ProgressPage(ProgressViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
