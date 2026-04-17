using Fitness_App.Models;
using Microsoft.Maui.Storage;

namespace Fitness_App.Services
{
    public interface ISuggestedLocationsService
    {
        IReadOnlyList<SuggestedLocation> GetAll();
    }

    public sealed class SuggestedLocationsService : ISuggestedLocationsService
    {
        private readonly Random _random = new();

        public SuggestedLocationsService()
        {
        }

        public IReadOnlyList<SuggestedLocation> GetAll()
        {
            double originLng = Preferences.Default.Get("last_lng", 123.900);
            double originLat = Preferences.Default.Get("last_lat", 10.315);
            var allLocations = LoadFromCatalog(originLng, originLat)
                .Where(location => location.DistanceKm <= CebuMapRegion.MaxSuggestedRouteKm)
                .ToList();

            var count = Math.Min(allLocations.Count, _random.Next(8, 13));
            return allLocations
                .OrderBy(_ => _random.Next())
                .Take(count)
                .ToList();
        }

        private static IReadOnlyList<SuggestedLocation> LoadFromCatalog(double approximateOriginLng, double approximateOriginLat)
        {
            return SuggestedRoutesCatalog.Seeds
                .Select(seed =>
                {
                    double distanceKm = CebuMapRegion.DistanceKm(approximateOriginLng, approximateOriginLat, seed.DestLng, seed.DestLat);
                    double minutesPerKm = string.Equals(seed.DirectionsProfile, "cycling", StringComparison.OrdinalIgnoreCase)
                        ? 3.8
                        : string.Equals(seed.DirectionsProfile, "driving", StringComparison.OrdinalIgnoreCase)
                            ? 1.4
                            : 12.0;

                    return new SuggestedLocation
                    {
                        RouteId = seed.Id,
                        Name = seed.Name,
                        DistanceKm = Math.Max(1.0, Math.Round(distanceKm, 1)),
                        EstimatedMinutes = Math.Max(8, (int)Math.Round(distanceKm * minutesPerKm)),
                        Image = "dotnet_bot.png",
                        DestLng = seed.DestLng,
                        DestLat = seed.DestLat,
                        DirectionsProfile = seed.DirectionsProfile
                    };
                })
                .ToList();
        }
    }
}
