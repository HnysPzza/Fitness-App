namespace Fitness_App.Services;

public interface IMapboxRoutingService
{
    Task<MapboxDirectionsResult?> GetDirectionsAsync(
        double originLng,
        double originLat,
        double destLng,
        double destLat,
        string profile,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MapboxGeocodeFeature>> GeocodeAsync(
        string query,
        double proximityLng,
        double proximityLat,
        CancellationToken cancellationToken = default);

    /// <summary>Merged geocode results for several local keywords (parks, malls, etc.).</summary>
    Task<IReadOnlyList<MapboxGeocodeFeature>> GetPopularNearbyAsync(
        double proximityLng,
        double proximityLat,
        CancellationToken cancellationToken = default);

    Task<MapboxGeocodeFeature?> ResolveDestinationAsync(
        string query,
        double approximateLng,
        double approximateLat,
        double proximityLng,
        double proximityLat,
        CancellationToken cancellationToken = default);
}

public sealed record MapboxDirectionsResult(
    IReadOnlyList<(double Lng, double Lat)> Coordinates,
    double DistanceMeters,
    double DurationSeconds);

public sealed record MapboxGeocodeFeature(string PlaceName, double Lng, double Lat);
