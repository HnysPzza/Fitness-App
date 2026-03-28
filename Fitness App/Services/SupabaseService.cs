using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Fitness_App.Services;

/// <summary>
/// Singleton that owns the Supabase <see cref="Client"/> instance and
/// exposes high-level auth + profile helpers.
/// </summary>
public sealed class SupabaseService : ISupabaseService
{
    private Supabase.Client? _client;

    // ── Public API ───────────────────────────────────────────────────────────

    public User? CurrentUser => _client?.Auth?.CurrentUser;
    
    public Supabase.Client? Client => _client;

    public bool IsLoggedIn => CurrentUser != null;

    // ── Initialization ──────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (_client != null) return;

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
        };

        _client = new Supabase.Client(
            SupabaseConfig.Url,
            SupabaseConfig.AnonKey,
            options
        );

        _client.Auth.SetPersistence(new SupabaseSessionPersistence());
        
        // Android's SecureStorage deadlocks/crashes if synchronous access occurs 
        // on the main UI thread. We push LoadSession to the ThreadPool.
        await Task.Run(() => _client.Auth.LoadSession());
        
        _client.Auth.Options.AllowUnconfirmedUserSessions = true;

        try
        {
            _client.Auth.Online = true;
            await _client.InitializeAsync();
        }
        catch
        {
            // Offline is fine – the saved session will still work locally.
        }
    }

    // ── Email + Password Sign In ────────────────────────────────────────────

    public async Task<Session?> SignInWithEmailAsync(string email, string password)
    {
        await InitializeAsync();
        var session = await Task.Run(async () =>
            await _client!.Auth.SignIn(email, password));
        return session;
    }

    // ── Email + Password Sign Up ────────────────────────────────────────────

    public async Task<Session?> SignUpWithEmailAsync(string email, string password)
    {
        // Always initialize — the user may land on register before the session guard finishes
        await InitializeAsync();
        
        // Run on a background thread.  gotrue-csharp internally calls SaveSession
        // synchronously on the calling SynchronizationContext.  On Android that's
        // the UI thread, and our SaveSession writes to SecureStorage (async I/O) —
        // this combination deadlocks & throws JavaProxyThrowable.
        var session = await Task.Run(async () =>
            await _client!.Auth.SignUp(email, password));
            
        return session;
    }

    // ── OTP Verification ────────────────────────────────────────────────────

    public async Task<Session?> VerifyOtpAsync(string email, string token)
    {
        EnsureInitialized();
        // Run on background thread — same SaveSession deadlock risk as SignUp/SignIn
        var session = await Task.Run(async () =>
            await _client!.Auth.VerifyOTP(email, token,
                Supabase.Gotrue.Constants.EmailOtpType.Signup));
        return session;
    }

    // ── Resend OTP ──────────────────────────────────────────────────────────

    public async Task ResendOtpAsync(string email)
    {
        await InitializeAsync();

        // gotrue-csharp 4.2.7 has no ResendEmail helper, so we POST directly
        // to Supabase's /auth/v1/resend endpoint.
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("apikey", SupabaseConfig.AnonKey);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseConfig.AnonKey}");

        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            type  = "signup",
            email = email
        });

        var content = new StringContent(body,
            System.Text.Encoding.UTF8, "application/json");

        await http.PostAsync(
            $"{SupabaseConfig.Url}/auth/v1/resend", content);
    }

    // ── Sign Out ────────────────────────────────────────────────────────────

    public async Task SignOutAsync()
    {
        EnsureInitialized();
        await _client!.Auth.SignOut();
    }

    // ── Profile Operations ──────────────────────────────────────────────────

    public async Task<Models.UserProfile?> GetCurrentProfileAsync()
    {
        EnsureInitialized();

        var user = CurrentUser;
        if (user == null) return null;

        try
        {
            var response = await _client!.From<ProfileRow>()
                .Where(x => x.UserId == user.Id)
                .Get();

            var row = response.Models.FirstOrDefault();
            if (row == null) return null;

            return new Models.UserProfile
            {
                Id = row.Id,
                UserId = row.UserId,
                FirstName = row.FirstName,
                LastName = row.LastName,
                Username = row.Username,
                DateOfBirth = row.DateOfBirth,
                Gender = row.Gender,
                AvatarUrl = row.AvatarUrl,
                CreatedAt = row.CreatedAt
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveProfileAsync(Models.UserProfile profile)
    {
        EnsureInitialized();

        var userId = profile.UserId ?? CurrentUser?.Id;
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException(
                "Not logged in — please sign in again before saving your profile.");

        var row = new ProfileRow
        {
            Id        = userId,  // <-- Added this: PostgreSQL requires the primary key 'id' to not be null.
            UserId    = userId,
            FirstName = profile.FirstName,
            LastName  = profile.LastName,
            Username  = profile.Username,
            DateOfBirth = profile.DateOfBirth,
            Gender    = profile.Gender,
            AvatarUrl = profile.AvatarUrl
        };

        await _client!.From<ProfileRow>().Insert(row);
    }

    public async Task<string?> UploadAvatarAsync(Stream fileStream, string fileName)
    {
        EnsureInitialized();

        var user = CurrentUser;
        if (user == null) return null;

        var storage = _client!.Storage.From("avatars");
        
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        // Path: userId/filename to keep it isolated
        var filePath = $"{user.Id}/{fileName}";

        // Upload/Replace the avatar
        await storage.Upload(bytes, filePath, new Supabase.Storage.FileOptions { Upsert = true });

        // Get public URL
        var publicUrl = storage.GetPublicUrl(filePath);
        return publicUrl;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void EnsureInitialized()
    {
        if (_client is null)
            throw new InvalidOperationException(
                "SupabaseService has not been initialised. Call InitializeAsync first.");
    }
}

// ── Postgrest Model ─────────────────────────────────────────────────────────

/// <summary>
/// Internal row model for the Postgrest ORM. Maps to the "profiles" table.
/// </summary>
[Table("profiles")]
internal class ProfileRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string? Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("date_of_birth")]
    public string? DateOfBirth { get; set; }

    [Column("gender")]
    public string? Gender { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("created_at")]
    public string? CreatedAt { get; set; }
}
