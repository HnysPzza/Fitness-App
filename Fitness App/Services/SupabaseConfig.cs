namespace Fitness_App.Services;

internal static partial class SupabaseConfig
{
    private static readonly Lazy<(string Url, string AnonKey)> Values = new(LoadValues);

    public static string Url => Values.Value.Url;
    public static string AnonKey => Values.Value.AnonKey;

    private static (string Url, string AnonKey) LoadValues()
    {
        var url = Environment.GetEnvironmentVariable("FITNESS_APP_SUPABASE_URL") ?? string.Empty;
        var anonKey = Environment.GetEnvironmentVariable("FITNESS_APP_SUPABASE_ANON_KEY") ?? string.Empty;

        ConfigureLocal(ref url, ref anonKey);

        return (url, anonKey);
    }

    static partial void ConfigureLocal(ref string url, ref string anonKey);
}
