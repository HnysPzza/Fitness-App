using System.Text.Json;
using Supabase.Gotrue;

namespace Fitness_App.Services;

public sealed record SavedAccountSession(
    string UserId,
    string Email,
    string DisplayName,
    string SessionJson,
    DateTime SavedAtUtc);

public interface IAccountSessionStore
{
    Task<IReadOnlyList<SavedAccountSession>> GetAccountsAsync();
    Task SaveSessionAsync(Session session, string? displayName = null);
    Task<bool> ActivateSessionAsync(string userId);
    Task RemoveSessionAsync(string userId);
}

public sealed class AccountSessionStore : IAccountSessionStore
{
    private const string SessionsKey = "supabase_saved_account_sessions";
    private static readonly JsonSerializerOptions JsonOptions = new();

    public async Task<IReadOnlyList<SavedAccountSession>> GetAccountsAsync()
    {
        var accounts = await LoadAccountsAsync();
        return accounts
            .OrderByDescending(account => account.SavedAtUtc)
            .ToList();
    }

    public async Task SaveSessionAsync(Session session, string? displayName = null)
    {
        var snapshot = CreateSnapshot(session, displayName);
        if (snapshot == null)
            return;

        var accounts = await LoadAccountsAsync();
        accounts.RemoveAll(account => account.UserId == snapshot.UserId);
        accounts.Insert(0, snapshot);
        await SaveAccountsAsync(accounts);
    }

    public async Task<bool> ActivateSessionAsync(string userId)
    {
        var accounts = await LoadAccountsAsync();
        var account = accounts.FirstOrDefault(saved => saved.UserId == userId);
        if (account == null)
            return false;

        await SecureStorage.Default.SetAsync(SupabaseSessionPersistence.StorageKey, account.SessionJson);
        return true;
    }

    public async Task RemoveSessionAsync(string userId)
    {
        var accounts = await LoadAccountsAsync();
        accounts.RemoveAll(account => account.UserId == userId);
        await SaveAccountsAsync(accounts);
    }

    public static Task SaveSessionSnapshotAsync(Session session)
    {
        return new AccountSessionStore().SaveSessionAsync(session);
    }

    private static SavedAccountSession? CreateSnapshot(Session session, string? displayName)
    {
        var user = session.User;
        if (user == null || string.IsNullOrWhiteSpace(user.Id))
            return null;

        var email = user.Email ?? "Saved account";
        var resolvedName = string.IsNullOrWhiteSpace(displayName)
            ? email
            : displayName.Trim();

        return new SavedAccountSession(
            user.Id,
            email,
            resolvedName,
            JsonSerializer.Serialize(session, JsonOptions),
            DateTime.UtcNow);
    }

    private static async Task<List<SavedAccountSession>> LoadAccountsAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(SessionsKey);
            if (string.IsNullOrWhiteSpace(json))
                return new List<SavedAccountSession>();

            return JsonSerializer.Deserialize<List<SavedAccountSession>>(json, JsonOptions)
                   ?? new List<SavedAccountSession>();
        }
        catch
        {
            return new List<SavedAccountSession>();
        }
    }

    private static async Task SaveAccountsAsync(List<SavedAccountSession> accounts)
    {
        try
        {
            var json = JsonSerializer.Serialize(accounts, JsonOptions);
            await SecureStorage.Default.SetAsync(SessionsKey, json);
        }
        catch
        {
            // If secure storage is unavailable, the user can still sign in normally.
        }
    }
}
