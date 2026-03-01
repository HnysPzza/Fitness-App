using System.Text.Json;
using Fitness_App.Models;

namespace Fitness_App.Services
{
    public interface ISuggestedLocationsService
    {
        IReadOnlyList<SuggestedLocation> GetAll();
    }

    public sealed class SuggestedLocationsService : ISuggestedLocationsService
    {
        private readonly IReadOnlyList<SuggestedLocation> _locations;

        public SuggestedLocationsService()
        {
            _locations = LoadFromJson();
        }

        public IReadOnlyList<SuggestedLocation> GetAll() => _locations;

        private static IReadOnlyList<SuggestedLocation> LoadFromJson()
        {
            var json = @"[
  { ""name"": ""Naga Boardwalk"", ""distanceKm"": 2.1, ""estimatedMinutes"": 18, ""image"": ""dotnet_bot.png"" },
  { ""name"": ""Freedom Park Loop"", ""distanceKm"": 3.6, ""estimatedMinutes"": 30, ""image"": ""dotnet_bot.png"" },
  { ""name"": ""Riverside Trail"", ""distanceKm"": 5.2, ""estimatedMinutes"": 45, ""image"": ""dotnet_bot.png"" }
]";

            return JsonSerializer.Deserialize<List<SuggestedLocation>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SuggestedLocation>();
        }
    }
}
