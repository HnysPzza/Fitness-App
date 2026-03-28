using System.Globalization;

namespace Fitness_App.Services;

public static class MapboxStaticPreview
{
    /// <summary>Map thumbnail with pin at destination (last point). Reliable across platforms.</summary>
    public static string BuildRoutePreviewUrl(
        IReadOnlyList<(double Lng, double Lat)> coords,
        string accessToken,
        int width = 440,
        int height = 240)
    {
        if (coords.Count == 0) return string.Empty;

        var d = coords[^1];
        string lng = d.Lng.ToString(CultureInfo.InvariantCulture);
        string lat = d.Lat.ToString(CultureInfo.InvariantCulture);

        return string.Format(CultureInfo.InvariantCulture,
            "https://api.mapbox.com/styles/v1/mapbox/outdoors-v12/static/pin-l+fc5200({0},{1})/{0},{1},14,0/{2}x{3}@2x?access_token={4}",
            lng, lat, width, height, Uri.EscapeDataString(accessToken));
    }
}
