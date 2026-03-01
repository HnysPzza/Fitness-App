using System.Collections.ObjectModel;
using Fitness_App.Models;
using Fitness_App.Services;

namespace Fitness_App.Pages
{
    public class HomePageViewModel
    {
        private readonly ISuggestedLocationsService _locationsService;

        public ObservableCollection<SuggestedLocation> SuggestedLocations { get; }

        public HomePageViewModel(ISuggestedLocationsService locationsService)
        {
            _locationsService = locationsService;
            SuggestedLocations = new ObservableCollection<SuggestedLocation>(_locationsService.GetAll());
        }
    }
}
