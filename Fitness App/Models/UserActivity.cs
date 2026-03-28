using Postgrest.Attributes;
using Postgrest.Models;
using System.Text.Json.Serialization;

namespace Fitness_App.Models;

[Table("user_activities")]
public class UserActivity : BaseModel
{
    [PrimaryKey("id", false)]
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

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("coordinates_json")]
    [JsonPropertyName("coordinates_json")]
    public string CoordinatesJson { get; set; } = string.Empty;

    [JsonIgnore]
    public ActivityRouteData RouteData => ActivityRouteCodec.Parse(CoordinatesJson);

    [JsonIgnore]
    public IReadOnlyList<ActivityRoutePoint> RoutePoints => RouteData.Points;

    [JsonIgnore]
    public double? MaxSpeedKmh => RouteData.MaxSpeedKmh;

    [JsonIgnore]
    public double? AvgSpeedKmh => RouteData.AvgSpeedKmh;

    [JsonIgnore]
    public double? ElevationGainM => RouteData.ElevationGainM;
}
