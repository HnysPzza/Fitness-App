namespace Fitness_App.Services;

// ════════════════════════════════════════════════════════════════════════════
//  SETUP INSTRUCTIONS — read before running the app
// ════════════════════════════════════════════════════════════════════════════
//
//  1. Copy this file and rename the copy to:
//       MapboxConfig.cs          (same folder)
//
//  2. Replace PASTE_YOUR_TOKEN_HERE with your real Mapbox public token from:
//       https://account.mapbox.com/access-tokens/
//
//  3. MapboxConfig.cs is git-ignored — your real token will NEVER be committed.
//     This template file (MapboxConfig.Template.cs) IS committed so teammates
//     know exactly what to create.
//
//  4. Recommended: restrict your token to your Android package name in the
//     Mapbox dashboard so it can't be misused even if extracted from the APK.
//
// ════════════════════════════════════════════════════════════════════════════
internal static class MapboxConfig
{
    public const string AccessToken = "PASTE_YOUR_TOKEN_HERE";
}
