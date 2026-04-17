namespace Fitness_App.Models
{
    public sealed class SuggestedLocation
    {
        public string Name { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public int EstimatedMinutes { get; set; }
        public string Image { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public double DestLng { get; set; }
        public double DestLat { get; set; }
        public string DirectionsProfile { get; set; } = "walking";
    }
}
