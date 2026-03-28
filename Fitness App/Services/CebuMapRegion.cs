using System.Globalization;

namespace Fitness_App.Services;

/// <summary>Metro Cebu / Central Visayas search bounds (Mapbox bbox: minLng,minLat,maxLng,maxLat).</summary>
public static class CebuMapRegion
{
    public const double MinLng = 123.55;
    public const double MinLat = 10.08;
    public const double MaxLng = 124.15;
    public const double MaxLat = 10.65;

    /// <summary>Max distance from user to suggested destination (km).</summary>
    public const double MaxSuggestedRouteKm = 42.0;

    /// <summary>Max distance from proximity point for popular POIs (km).</summary>
    public const double MaxPopularPoiKm = 35.0;

    public static string BboxQueryString =>
        string.Format(CultureInfo.InvariantCulture,
            "{0},{1},{2},{3}", MinLng, MinLat, MaxLng, MaxLat);

    public static bool ContainsPoint(double lng, double lat) =>
        lng >= MinLng && lng <= MaxLng && lat >= MinLat && lat <= MaxLat;

    /// <summary>Haversine distance in km.</summary>
    public static double DistanceKm(double lng1, double lat1, double lng2, double lat2)
    {
        const double R = 6371.0;
        double ToRad(double d) => d * Math.PI / 180.0;
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lng2 - lng1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }
}
