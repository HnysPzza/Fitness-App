using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace Fitness_App.Models;

[Table("user_activities")]
public class UserActivity : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("sport")]
    [JsonPropertyName("sport")]
    public string Sport { get; set; } = string.Empty;

    [Column("distance_km")]
    [JsonPropertyName("distance_km")]
    public double DistanceKm { get; set; }

    [Column("duration_ticks")]
    [JsonPropertyName("duration_ticks")]
    public long DurationTicks { get; set; }

    [Column("avg_speed_kmh")]
    [JsonPropertyName("avg_speed_kmh")]
    public double? AvgSpeedKmh { get; set; }

    [Column("max_speed_kmh")]
    [JsonPropertyName("max_speed_kmh")]
    public double? MaxSpeedKmh { get; set; }

    [Column("elevation_gain_m")]
    [JsonPropertyName("elevation_gain_m")]
    public double? ElevationGainM { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("coordinates_json")]
    [JsonPropertyName("coordinates_json")]
    public string CoordinatesJson { get; set; } = string.Empty;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ActivityRouteData RouteData => ActivityRouteCodec.Parse(CoordinatesJson);

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IReadOnlyList<ActivityRoutePoint> RoutePoints => RouteData.Points;
}
