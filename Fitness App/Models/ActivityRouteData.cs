using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fitness_App.Models;

public sealed class ActivityRouteData
{
    [JsonPropertyName("points")]
    public List<ActivityRoutePoint> Points { get; set; } = new();

    [JsonPropertyName("max_speed_kmh")]
    public double? MaxSpeedKmh { get; set; }

    [JsonPropertyName("avg_speed_kmh")]
    public double? AvgSpeedKmh { get; set; }

    [JsonPropertyName("elevation_gain_m")]
    public double? ElevationGainM { get; set; }
}

public sealed class ActivityRoutePoint
{
    [JsonPropertyName("lng")]
    public double Lng { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }
}

public static class ActivityRouteCodec
{
    public static string Serialize(IEnumerable<ActivityRoutePoint> points, double? maxSpeedKmh, double? avgSpeedKmh, double? elevationGainM)
    {
        var payload = new ActivityRouteData
        {
            Points = points.ToList(),
            MaxSpeedKmh = maxSpeedKmh,
            AvgSpeedKmh = avgSpeedKmh,
            ElevationGainM = elevationGainM
        };

        return JsonSerializer.Serialize(payload);
    }

    public static ActivityRouteData Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ActivityRouteData();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var points = JsonSerializer.Deserialize<double[][]>(json) ?? Array.Empty<double[]>();
                return new ActivityRouteData
                {
                    Points = points
                        .Where(p => p.Length >= 2)
                        .Select(p => new ActivityRoutePoint { Lng = p[0], Lat = p[1] })
                        .ToList()
                };
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var parsed = JsonSerializer.Deserialize<ActivityRouteData>(json) ?? new ActivityRouteData();
                parsed.Points ??= new List<ActivityRoutePoint>();
                return parsed;
            }
        }
        catch
        {
        }

        return new ActivityRouteData();
    }

    public static double[][] ExtractCoordinates(string? json) =>
        Parse(json).Points.Select(point => new[] { point.Lng, point.Lat }).ToArray();
}
