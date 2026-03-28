using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System.Text.Json;

namespace Fitness_App.Services;

/// <summary>
/// Persists the Supabase GoTrue session to SecureStorage so the user
/// stays logged in between app launches.
/// </summary>
public sealed class SupabaseSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string StorageKey = "supabase_session";

    public void SaveSession(Session session)
    {
        // SaveSession is called synchronously by gotrue, but SecureStorage is async.
        // We MUST wait for the write to actually complete, otherwise if the app is
        // killed before this background task finishes the session is lost and the user
        // appears logged out on next launch.
        try
        {
            var json = JsonSerializer.Serialize(session);
            Task.Run(async () => await SecureStorage.Default.SetAsync(StorageKey, json))
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            // Worst case the user has to log in again.
        }
    }

    public void DestroySession()
    {
        try
        {
            SecureStorage.Default.Remove(StorageKey);
        }
        catch { /* best-effort */ }
    }

    public Session? LoadSession()
    {
        try
        {
            // SecureStorage.GetAsync uses Android's EncryptedSharedPreferences.
            // Using .GetAwaiter().GetResult() on the Main Thread causes a fatal deadlock/crash.
            // We wrap it in Task.Run to force it onto the threadpool, preventing the deadlock.
            var json = Task.Run(async () => await SecureStorage.Default.GetAsync(StorageKey))
                           .GetAwaiter()
                           .GetResult();
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<Session>(json);
        }
        catch
        {
            return null;
        }
    }
}
