namespace Fitness_App.Models;

/// <summary>Navigate to Maps tab and draw this polyline (see maps.html <c>addSavedRoute</c>).</summary>
public record ShowSuggestedRouteOnMapMessage(
    string RouteId,
    string CoordinatesJson,
    string ColorHex,
    string? Title,
    double? DestLng = null,
    double? DestLat = null);

/// <summary>Set before <c>Shell.GoToAsync("//maps")</c>; consumed when Maps loads.</summary>
public static class MapNavigationState
{
    public static ShowSuggestedRouteOnMapMessage? PendingSuggestedRoute { get; set; }
}
