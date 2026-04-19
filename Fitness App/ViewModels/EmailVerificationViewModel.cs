using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fitness_App.Services;

namespace Fitness_App.ViewModels;

public partial class EmailVerificationViewModel : ObservableObject
{
    private readonly ISupabaseService _auth;

    public EmailVerificationViewModel(ISupabaseService auth)
    {
        _auth = auth;
    }

    // ── Observable Properties ───────────────────────────────────────────────

    [ObservableProperty]
    private string _emailAddress = string.Empty;

    [ObservableProperty]
    private string _digit1 = string.Empty;

    [ObservableProperty]
    private string _digit2 = string.Empty;

    [ObservableProperty]
    private string _digit3 = string.Empty;

    [ObservableProperty]
    private string _digit4 = string.Empty;

    [ObservableProperty]
    private string _digit5 = string.Empty;

    [ObservableProperty]
    private string _digit6 = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canResend = true;

    [ObservableProperty]
    private int _resendCooldown;

    [ObservableProperty]
    private string _resendText = "Resend Code";

    /// <summary>Masked email for display: k***@example.com</summary>
    public string MaskedEmail
    {
        get
        {
            if (string.IsNullOrEmpty(EmailAddress)) return "";
            var parts = EmailAddress.Split('@');
            if (parts.Length != 2) return EmailAddress;
            var name = parts[0];
            var masked = name.Length <= 2
                ? name + "***"
                : name[..2] + new string('*', Math.Min(name.Length - 2, 4));
            return $"{masked}@{parts[1]}";
        }
    }

    partial void OnEmailAddressChanged(string value)
    {
        OnPropertyChanged(nameof(MaskedEmail));
    }

    // ── Commands ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task VerifyAsync()
    {
        ErrorMessage = null;

        var code = $"{Digit1}{Digit2}{Digit3}{Digit4}{Digit5}{Digit6}";

        if (code.Length != 6 || !code.All(char.IsDigit))
        {
            ErrorMessage = "Please enter all 6 digits";
            return;
        }

        IsLoading = true;

        try
        {
            var session = await _auth.VerifyOtpAsync(EmailAddress, code);

            if (session != null)
            {
                // Wait for gotrue to fully persist the session and update CurrentUser.
                // Without this delay, ProfileSetupViewModel._auth.CurrentUser is null
                // and the INSERT fails with PostgreSQL error 23502 (not_null_violation).
                await Task.Delay(400);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Shell.Current.GoToAsync("//profilesetup"));
            }
            else
            {
                ErrorMessage = "Invalid or expired code. Please try again.";
            }
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            var msg = FormatAuthError(ex);
            System.Diagnostics.Debug.WriteLine($"[EMAIL VERIFY] GotrueException: {msg}\n{ex}");
            if (msg.Contains("expired", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Invalid or expired code. Please try again.";
            }
            else
            {
                ErrorMessage = msg;
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Check your connection and try again.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Something went wrong: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResendCodeAsync()
    {
        if (!CanResend) return;

        try
        {
            await _auth.ResendOtpAsync(EmailAddress);
            StartCooldownTimer();
        }
        catch
        {
            ErrorMessage = "Failed to resend code. Try again later.";
        }
    }

    // ── Cooldown Timer ──────────────────────────────────────────────────────

    private async void StartCooldownTimer()
    {
        CanResend = false;
        ResendCooldown = 60;

        while (ResendCooldown > 0)
        {
            ResendText = $"Resend in {ResendCooldown}s";
            await Task.Delay(1000);
            ResendCooldown--;
        }

        ResendText = "Resend Code";
        CanResend = true;
    }

    private static string FormatAuthError(Supabase.Gotrue.Exceptions.GotrueException ex)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(ex.Message))
            parts.Add(ex.Message);

        if (ex.StatusCode != 0)
            parts.Add($"Status: {(int)ex.StatusCode} {ex.StatusCode}");

        parts.Add($"Reason: {ex.Reason}");

        if (!string.IsNullOrWhiteSpace(ex.Content))
            parts.Add($"Response: {ex.Content}");

        if (!string.IsNullOrWhiteSpace(ex.InnerException?.Message))
            parts.Add($"Inner: {ex.InnerException.Message}");

        return string.Join(Environment.NewLine, parts.Distinct());
    }
}
