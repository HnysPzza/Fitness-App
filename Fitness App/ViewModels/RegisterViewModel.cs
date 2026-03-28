using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fitness_App.Services;
using System.Text.RegularExpressions;

namespace Fitness_App.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly ISupabaseService _auth;

    public RegisterViewModel(ISupabaseService auth)
    {
        _auth = auth;
    }

    // ── Observable Properties ───────────────────────────────────────────────

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _emailError;

    [ObservableProperty]
    private string? _passwordError;

    [ObservableProperty]
    private string? _confirmPasswordError;

    [ObservableProperty]
    private string? _generalError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible;

    // ── Commands ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility() => IsConfirmPasswordVisible = !IsConfirmPasswordVisible;

    [RelayCommand]
    private async Task SignUpAsync()
    {
        EmailError = null;
        PasswordError = null;
        ConfirmPasswordError = null;
        GeneralError = null;

        bool valid = true;

        // ── Validation ──────────────────────────────────────────────────
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
        else if (Password.Length < 8)
        {
            PasswordError = "Password must be at least 8 characters";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ConfirmPasswordError = "Please confirm your password";
            valid = false;
        }
        else if (Password != ConfirmPassword)
        {
            ConfirmPasswordError = "Passwords do not match";
            valid = false;
        }

        if (!valid) return;

        // ── API call ────────────────────────────────────────────────────
        IsLoading = true;

        try
        {
            System.Diagnostics.Debug.WriteLine("[REGISTER] Starting SignUp...");
            var session = await _auth.SignUpWithEmailAsync(Email.Trim(), Password);
            System.Diagnostics.Debug.WriteLine($"[REGISTER] SignUp returned. Session null? {session == null}");

            // Save the email for the verification page BEFORE navigating.
            // Use a small delay to let the Supabase client finish its internal
            // callbacks (state change events, session persistence) so they don't
            // collide with Shell's fragment transaction.
            await Task.Delay(300);

            System.Diagnostics.Debug.WriteLine("[REGISTER] Navigating to emailverification...");

            // Navigate on the main thread to avoid Android fragment crashes
            var trimmedEmail = Email.Trim();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var encodedEmail = Uri.EscapeDataString(trimmedEmail);
                await Shell.Current.GoToAsync($"//emailverification?email={encodedEmail}");
            });

            System.Diagnostics.Debug.WriteLine("[REGISTER] Navigation complete.");
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[REGISTER] GotrueException: {ex.Message}");
            var msg = ex.Message;
            if (msg.Contains("already registered", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("already been registered", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("User already registered", StringComparison.OrdinalIgnoreCase))
            {
                GeneralError = "An account with this email already exists";
            }
            else if (msg.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                PasswordError = "Password must be at least 8 characters";
            }
            else
            {
                GeneralError = msg;
            }
        }
        catch (HttpRequestException)
        {
            GeneralError = "Check your connection and try again";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[REGISTER] EXCEPTION: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            GeneralError = $"Something went wrong: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
}
