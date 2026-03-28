using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fitness_App.Services;
using System.Text.RegularExpressions;

namespace Fitness_App.ViewModels;

/// <summary>
/// ViewModel backing the Login page. Uses CommunityToolkit.Mvvm for
/// observable properties and relay commands.
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly ISupabaseService _auth;
    private readonly IProfileService _profile;

    public LoginViewModel(ISupabaseService auth, IProfileService profile)
    {
        _auth = auth;
        _profile = profile;
    }

    // ── Observable Properties ───────────────────────────────────────────────

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _emailError;

    [ObservableProperty]
    private string? _passwordError;

    [ObservableProperty]
    private string? _generalError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPasswordVisible;

    // ── Commands ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        // Clear previous errors
        EmailError = null;
        PasswordError = null;
        GeneralError = null;

        // ── Client-side validation ──────────────────────────────────────
        bool valid = true;

        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "Email is required";
            valid = false;
        }
        else if (!IsValidEmail(Email.Trim()))
        {
            EmailError = "Enter a valid email address";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordError = "Password is required";
            valid = false;
        }

        if (!valid) return;

        // ── API call ────────────────────────────────────────────────────
        IsLoading = true;

        try
        {
            var session = await _auth.SignInWithEmailAsync(Email.Trim(), Password);

            if (session != null)
            {
                // Sync Supabase profile → local Preferences before navigating
                var supaProfile = await _auth.GetCurrentProfileAsync();
                if (supaProfile != null)
                    _profile.SyncFromSupabase(supaProfile);

                // Navigate to Home via Shell routing.
                // The // prefix resets the navigation stack so Back won’t return to Login.
                await Shell.Current.GoToAsync("//home");
            }
            else
            {
                GeneralError = "Invalid email or password";
            }
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            GeneralError = ex.Message.Contains("Invalid login", StringComparison.OrdinalIgnoreCase)
                ? "Invalid email or password"
                : ex.Message;
        }
        catch (HttpRequestException)
        {
            GeneralError = "No internet connection. Please check your network and try again.";
        }
        catch (Exception ex)
        {
            GeneralError = $"Something went wrong: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);
}
