namespace Fitness_App.Models;

/// <summary>Named destinations used to build suggested routes from the user&apos;s current area.</summary>
/// <param name="DirectionsProfile">Mapbox Directions profile: <c>walking</c> or <c>cycling</c>.</param>
public sealed record SuggestedRouteSeed(
    string Id,
    string Name,
    double DestLng,
    double DestLat,
    string DirectionsProfile = "walking");
