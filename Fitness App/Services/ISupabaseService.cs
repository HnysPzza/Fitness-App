using Supabase.Gotrue;

namespace Fitness_App.Services;

/// <summary>
/// Abstraction for Supabase authentication and profile operations.
/// </summary>
public interface ISupabaseService
{
    /// <summary>Current authenticated user, or null.</summary>
    User? CurrentUser { get; }

    /// <summary>The underlying Supabase client to execute direct REST queries.</summary>
    Supabase.Client? Client { get; }

    /// <summary>True when there is a cached / restored session.</summary>
    bool IsLoggedIn { get; }

    /// <summary>Initializes the Supabase client and attempts to restore a cached session.</summary>
    Task InitializeAsync();

    /// <summary>Reloads the persisted session after another saved account is activated.</summary>
    Task ReloadPersistedSessionAsync();

    /// <summary>Sign in with email + password.</summary>
    Task<Session?> SignInWithEmailAsync(string email, string password);

    /// <summary>Sign up with email + password. Supabase sends OTP to the email.</summary>
    Task<Session?> SignUpWithEmailAsync(string email, string password);

    /// <summary>Verify email OTP code.</summary>
    Task<Session?> VerifyOtpAsync(string email, string token);

    /// <summary>Resend OTP verification email.</summary>
    Task ResendOtpAsync(string email);

    /// <summary>Sign out and clear the persisted session.</summary>
    Task SignOutAsync();

    // ── Profile Operations ──────────────────────────────────────────────────

    /// <summary>Fetch the profile for the current user. Returns null if no profile exists.</summary>
    Task<Models.UserProfile?> GetCurrentProfileAsync();

    /// <summary>Save a new profile to the profiles table.</summary>
    Task SaveProfileAsync(Models.UserProfile profile);

    /// <summary>Uploads an avatar image and returns the public URL.</summary>
    Task<string?> UploadAvatarAsync(Stream fileStream, string fileName);
}
