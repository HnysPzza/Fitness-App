using Fitness_App.ViewModels;

namespace Fitness_App.Pages;

public partial class ProfileSetupPage : ContentPage
{
    public ProfileSetupPage(ProfileSetupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
