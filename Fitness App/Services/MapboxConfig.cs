namespace Fitness_App.Services;

internal static partial class MapboxConfig
{
    private static readonly Lazy<string> Token = new(LoadAccessToken);

    public static string AccessToken => Token.Value;

    private static string LoadAccessToken()
    {
        var accessToken = Environment.GetEnvironmentVariable("FITNESS_APP_MAPBOX_ACCESS_TOKEN") ?? string.Empty;

        ConfigureLocal(ref accessToken);

        return accessToken;
    }

    static partial void ConfigureLocal(ref string accessToken);
}
