using Microsoft.Maui.Storage;
using System.Collections.Concurrent;
using System.Globalization;

namespace Fitness_App.Services;

/// <summary>Inlines bundled Mapbox GL JS/CSS from Maui assets to avoid CDN cold start on mobile WebViews.</summary>
public static class MapboxWebViewHtml
{
    private const string BundlePlaceholder = "<!--MAPBOX_BUNDLE-->";
    private static readonly ConcurrentDictionary<string, Task<string>> TemplateCache = new();
    private static Task<string>? _bundleMarkupTask;

    public static async Task<string> BuildMapHtmlAsync(string assetFileName, string style, double lng, double lat)
    {
        var template = await GetPreparedTemplateAsync(assetFileName).ConfigureAwait(false);

        return template.Replace("__MAPBOX_TOKEN__", MapboxConfig.AccessToken, StringComparison.Ordinal)
            .Replace("__START_STYLE__", style, StringComparison.Ordinal)
            .Replace("__START_LNG__", lng.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__START_LAT__", lat.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    public static async Task PreloadAsync()
    {
        try
        {
            await Task.WhenAll(
                GetPreparedTemplateAsync("maps.html"),
                GetPreparedTemplateAsync("mapbox_map.html")).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapboxWebViewHtml] Preload skipped: {ex.Message}");
        }
    }

    private static Task<string> GetPreparedTemplateAsync(string assetFileName) =>
        TemplateCache.GetOrAdd(assetFileName, static async fileName =>
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync().ConfigureAwait(false);
            return await InjectBundledMapboxGlAsync(html).ConfigureAwait(false);
        });

    /// <summary>
    /// Replaces <c>&lt;!--MAPBOX_BUNDLE--&gt;</c> with inlined style + script, or CDN fallback if files are missing.
    /// Escapes <c>&lt;/script&gt;</c> sequences inside the JS bundle.
    /// </summary>
    public static async Task<string> InjectBundledMapboxGlAsync(string html)
    {
        if (!html.Contains(BundlePlaceholder, StringComparison.Ordinal))
            return html;

        try
        {
            var bundle = await GetBundleMarkupAsync().ConfigureAwait(false);
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[MapboxWebViewHtml] Bundled mapbox-gl.js/css injected (cold start prefers this over CDN).");
#endif
            return html.Replace(BundlePlaceholder, bundle, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[MapboxWebViewHtml] WARNING: Bundled mapbox-gl assets missing — falling back to CDN (slower). {ex.Message}");
            const string cdn =
                "<link href=\"https://api.mapbox.com/mapbox-gl-js/v3.3.0/mapbox-gl.css\" rel=\"stylesheet\" />\n" +
                "<script src=\"https://api.mapbox.com/mapbox-gl-js/v3.3.0/mapbox-gl.js\"></script>";
            return html.Replace(BundlePlaceholder, cdn, StringComparison.Ordinal);
        }
    }

    private static Task<string> GetBundleMarkupAsync()
    {
        _bundleMarkupTask ??= LoadBundleMarkupAsync();
        return _bundleMarkupTask;
    }

    private static async Task<string> LoadBundleMarkupAsync()
    {
        await using var cssStream = await FileSystem.OpenAppPackageFileAsync("mapbox-gl.css").ConfigureAwait(false);
        await using var jsStream = await FileSystem.OpenAppPackageFileAsync("mapbox-gl.js").ConfigureAwait(false);
        using var cssReader = new StreamReader(cssStream);
        using var jsReader = new StreamReader(jsStream);
        var css = await cssReader.ReadToEndAsync().ConfigureAwait(false);
        var js = await jsReader.ReadToEndAsync().ConfigureAwait(false);
        js = js.Replace("</script>", "<\\/script>", StringComparison.OrdinalIgnoreCase);
        return "<style>\n" + css + "\n</style>\n<script>\n" + js + "\n</script>";
    }
}
