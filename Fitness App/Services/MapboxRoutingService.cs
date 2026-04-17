using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace Fitness_App.Services;

public sealed class MapboxRoutingService : IMapboxRoutingService
{
    private readonly HttpClient _http;
    private static readonly ConcurrentDictionary<string, (MapboxDirectionsResult? Result, DateTime ExpiresUtc)> DirectionsCache = new();
    private static readonly ConcurrentDictionary<string, (IReadOnlyList<MapboxGeocodeFeature> List, DateTime ExpiresUtc)> GeocodeCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public MapboxRoutingService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("Mapbox");
    }

    /// <summary>Mapbox Directions profile: walking, cycling, or driving.</summary>
    public static string ProfileForSport(string sportName)
    {
        return sportName switch
        {
            "Walk" or "Hike" or "Trail Run" or "Snowshoe" or "Walk (Treadmill)" => "walking",
            "Run" or "Virtual Run" => "walking",
            "Cycling" or "Mountain Bike" or "Gravel Ride" or "E-Bike" or "Indoor Cycling" => "cycling",
            "Skateboard" or "Inline Skate" or "Roller Ski" => "cycling",
            _ => "driving",
        };
    }

    private static string DirectionsCacheKey(double originLng, double originLat, double destLng, double destLat, string profile) =>
        $"{profile}:{Round4(originLng)},{Round4(originLat)};{Round4(destLng)},{Round4(destLat)}";

    private static string GeocodeCacheKey(string query, double proximityLng, double proximityLat) =>
        $"{query.Trim().ToLowerInvariant()}|{Round3(proximityLng)},{Round3(proximityLat)}";

    private static string Round4(double v) => v.ToString("F4", CultureInfo.InvariantCulture);
    private static string Round3(double v) => v.ToString("F3", CultureInfo.InvariantCulture);

    private static string NormalizeSearchText(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ')
            .ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static double ScoreFeature(string query, string placeName, double featureLng, double featureLat, double proximityLng, double proximityLat)
    {
        string normalizedQuery = NormalizeSearchText(query);
        string normalizedPlace = NormalizeSearchText(placeName);

        double score = 0;
        if (normalizedPlace == normalizedQuery)
            score += 120;
        else if (normalizedPlace.StartsWith(normalizedQuery, StringComparison.Ordinal))
            score += 80;
        else if (normalizedPlace.Contains(normalizedQuery, StringComparison.Ordinal))
            score += 55;

        foreach (var token in normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (normalizedPlace.Contains(token, StringComparison.Ordinal))
                score += 8;
        }

        double distanceKm = CebuMapRegion.DistanceKm(proximityLng, proximityLat, featureLng, featureLat);
        score -= Math.Min(distanceKm, CebuMapRegion.MaxPopularPoiKm);
        return score;
    }

    private static double ScoreDestinationResolution(string query, string placeName, double featureLng, double featureLat, double approximateLng, double approximateLat)
    {
        double score = ScoreFeature(query, placeName, featureLng, featureLat, approximateLng, approximateLat);
        double offsetKm = CebuMapRegion.DistanceKm(approximateLng, approximateLat, featureLng, featureLat);
        score -= offsetKm * 3.5;
        return score;
    }

    public async Task<MapboxDirectionsResult?> GetDirectionsAsync(
        double originLng,
        double originLat,
        double destLng,
        double destLat,
        string profile,
        CancellationToken cancellationToken = default)
    {
        string p = string.IsNullOrWhiteSpace(profile) ? "walking" : profile;
        var cacheKey = DirectionsCacheKey(originLng, originLat, destLng, destLat, p);
        if (DirectionsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresUtc > DateTime.UtcNow)
            return cached.Result;

        string coords = string.Format(CultureInfo.InvariantCulture,
            "{0},{1};{2},{3}", originLng, originLat, destLng, destLat);
        string url =
            $"https://api.mapbox.com/directions/v5/mapbox/{Uri.EscapeDataString(p)}/{coords}" +
            "?geometries=geojson&overview=full&steps=false" +
            $"&access_token={Uri.EscapeDataString(MapboxConfig.AccessToken)}";

        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(text);

        if (!doc.RootElement.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
            return null;

        var route = routes[0];
        if (!route.TryGetProperty("geometry", out var geometry))
            return null;

        if (!geometry.TryGetProperty("coordinates", out var coordsEl))
            return null;

        var list = new List<(double Lng, double Lat)>();
        foreach (var pt in coordsEl.EnumerateArray())
        {
            if (pt.GetArrayLength() < 2) continue;
            double lng = pt[0].GetDouble();
            double lat = pt[1].GetDouble();
            list.Add((lng, lat));
        }

        if (list.Count == 0)
            return null;

        double distanceM = route.TryGetProperty("distance", out var d) ? d.GetDouble() : 0;
        double durationS = route.TryGetProperty("duration", out var t) ? t.GetDouble() : 0;

        var result = new MapboxDirectionsResult(list, distanceM, durationS);
        DirectionsCache[cacheKey] = (result, DateTime.UtcNow + CacheTtl);
        return result;
    }

    public async Task<IReadOnlyList<MapboxGeocodeFeature>> GeocodeAsync(
        string query,
        double proximityLng,
        double proximityLat,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<MapboxGeocodeFeature>();

        var cacheKey = GeocodeCacheKey(query, proximityLng, proximityLat);
        if (GeocodeCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresUtc > DateTime.UtcNow)
            return cached.List.Select(f => new MapboxGeocodeFeature(f.PlaceName, f.Lng, f.Lat)).ToList();

        string q = Uri.EscapeDataString(query.Trim());
        string prox = string.Format(CultureInfo.InvariantCulture, "{0},{1}", proximityLng, proximityLat);
        string bbox = CebuMapRegion.BboxQueryString;
        string url =
            $"https://api.mapbox.com/geocoding/v5/mapbox.places/{q}.json" +
            $"?proximity={Uri.EscapeDataString(prox)}&limit=8&country=PH&autocomplete=true&types=poi,address,place,locality,neighborhood" +
            $"&bbox={Uri.EscapeDataString(bbox)}" +
            $"&access_token={Uri.EscapeDataString(MapboxConfig.AccessToken)}";

        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<MapboxGeocodeFeature>();

        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(text);

        if (!doc.RootElement.TryGetProperty("features", out var features))
            return Array.Empty<MapboxGeocodeFeature>();

        var result = new List<MapboxGeocodeFeature>();
        foreach (var f in features.EnumerateArray())
        {
            string name = f.TryGetProperty("place_name", out var pn) ? pn.GetString() ?? "" : "";
            if (!f.TryGetProperty("center", out var center) || center.GetArrayLength() < 2)
                continue;
            double lng = center[0].GetDouble();
            double lat = center[1].GetDouble();
            result.Add(new MapboxGeocodeFeature(name, lng, lat));
        }

        var filtered = FilterToCebuProximity(result, proximityLng, proximityLat)
            .OrderByDescending(f => ScoreFeature(query, f.PlaceName, f.Lng, f.Lat, proximityLng, proximityLat))
            .ToList();
        var asReadOnly = (IReadOnlyList<MapboxGeocodeFeature>)filtered;
        GeocodeCache[cacheKey] = (asReadOnly, DateTime.UtcNow + CacheTtl);
        return filtered.Select(f => new MapboxGeocodeFeature(f.PlaceName, f.Lng, f.Lat)).ToList();
    }

    public async Task<MapboxGeocodeFeature?> ResolveDestinationAsync(
        string query,
        double approximateLng,
        double approximateLat,
        double proximityLng,
        double proximityLat,
        CancellationToken cancellationToken = default)
    {
        var candidates = await GeocodeAsync(query, proximityLng, proximityLat, cancellationToken).ConfigureAwait(false);
        return candidates
            .Where(f => CebuMapRegion.DistanceKm(approximateLng, approximateLat, f.Lng, f.Lat) <= 20.0)
            .OrderByDescending(f => ScoreDestinationResolution(query, f.PlaceName, f.Lng, f.Lat, approximateLng, approximateLat))
            .FirstOrDefault()
            ?? candidates.FirstOrDefault();
    }

    private static List<MapboxGeocodeFeature> FilterToCebuProximity(
        List<MapboxGeocodeFeature> features,
        double proximityLng,
        double proximityLat)
    {
        var list = new List<MapboxGeocodeFeature>();
        foreach (var f in features)
        {
            if (!CebuMapRegion.ContainsPoint(f.Lng, f.Lat))
                continue;
            if (CebuMapRegion.DistanceKm(proximityLng, proximityLat, f.Lng, f.Lat) > CebuMapRegion.MaxPopularPoiKm)
                continue;
            list.Add(f);
        }

        return list;
    }

    public async Task<IReadOnlyList<MapboxGeocodeFeature>> GetPopularNearbyAsync(
        double proximityLng,
        double proximityLat,
        CancellationToken cancellationToken = default)
    {
        string[] queries =
        [
            "park", "Ayala Center Cebu", "SM Seaside", "Fuente Osmeña", "Tops lookout",
            "mall", "coffee", "restaurant", "hotel"
        ];

        var seen = new HashSet<string>();
        var merged = new List<MapboxGeocodeFeature>();

        foreach (var q in queries)
        {
            if (merged.Count >= 8) break;
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await GeocodeAsync(q, proximityLng, proximityLat, cancellationToken).ConfigureAwait(false);
            foreach (var f in batch)
            {
                string key = $"{Math.Round(f.Lng, 4)},{Math.Round(f.Lat, 4)}";
                if (!seen.Add(key)) continue;
                if (!CebuMapRegion.ContainsPoint(f.Lng, f.Lat)) continue;
                if (CebuMapRegion.DistanceKm(proximityLng, proximityLat, f.Lng, f.Lat) > CebuMapRegion.MaxPopularPoiKm)
                    continue;
                merged.Add(f);
                if (merged.Count >= 8) break;
            }
        }

        return merged;
    }
}
