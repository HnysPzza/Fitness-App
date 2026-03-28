using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fitness_App.Services;

namespace Fitness_App.ViewModels;

public partial class ProfileSetupViewModel : ObservableObject
{
    private readonly ISupabaseService _auth;
    private readonly IProfileService _profile;

    public ProfileSetupViewModel(ISupabaseService auth, IProfileService profile)
    {
        _auth = auth;
        _profile = profile;
        SelectedDate = DateTime.Today.AddYears(-18);
    }

    // ── Observable Properties ───────────────────────────────────────────────

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private DateTime _selectedDate;

    [ObservableProperty]
    private string _selectedGender = "Prefer not to say";

    [ObservableProperty]
    private string? _firstNameError;

    [ObservableProperty]
    private string? _lastNameError;

    [ObservableProperty]
    private string? _usernameError;

    [ObservableProperty]
    private string? _generalError;

    [ObservableProperty]
    private bool _isLoading;

    public List<string> GenderOptions { get; } = new()
    {
        "Male",
        "Female",
        "Prefer not to say"
    };

    // ── Commands ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectAvatarAsync()
    {
        try
        {
            var action = await Shell.Current.DisplayActionSheet("Set Profile Picture", "Cancel", null, "Take Photo", "Choose from Gallery");

            FileResult? photo = null;

            if (action == "Take Photo")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    photo = await MediaPicker.Default.CapturePhotoAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Not Supported", "Camera isn't available on this device.", "OK");
                    return;
                }
            }
            else if (action == "Choose from Gallery")
            {
                photo = await MediaPicker.Default.PickPhotoAsync();
            }

            if (photo != null)
            {
                IsLoading = true;
                using var stream = await photo.OpenReadAsync();
                
                // Compress/convert or just use raw stream
                // We'll upload directly
                var extension = Path.GetExtension(photo.FileName);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                
                var fileName = $"avatar_{DateTime.UtcNow.Ticks}{extension}";
                
                var url = await _auth.UploadAvatarAsync(stream, fileName);
                
                if (url != null)
                {
                    // Bust the cache so the UI updates
                    AvatarUrl = $"{url}?t={DateTime.UtcNow.Ticks}";
                }
            }
        }
        catch (Exception ex)
        {
            GeneralError = $"Failed to set picture: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        FirstNameError = null;
        LastNameError = null;
        UsernameError = null;
        GeneralError = null;

        bool valid = true;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            FirstNameError = "First name is required";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            LastNameError = "Last name is required";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            UsernameError = "Username is required";
            valid = false;
        }
        else if (Username.Trim().Length < 3)
        {
            UsernameError = "Username must be at least 3 characters";
            valid = false;
        }

        if (!valid) return;

        IsLoading = true;

        try
        {
            var profile = new Models.UserProfile
            {
                UserId = _auth.CurrentUser?.Id,
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Username = Username.Trim(),
                DateOfBirth = SelectedDate.ToString("yyyy-MM-dd"),
                Gender = SelectedGender,
                AvatarUrl = AvatarUrl
            };

            await _auth.SaveProfileAsync(profile);

            // Sync the new Supabase profile into local Preferences
            // so HomePage, SettingsPage etc. show the correct data.
            _profile.SyncFromSupabase(profile);

            // Profile saved → go to Home
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("unique", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("username", StringComparison.OrdinalIgnoreCase))
            {
                UsernameError = "Username already taken";
            }
            else if (ex is HttpRequestException)
            {
                GeneralError = "Check your connection and try again";
            }
            else
            {
                GeneralError = $"Something went wrong: {msg}";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
