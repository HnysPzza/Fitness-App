namespace Fitness_App.Services;

/// <summary>
/// Centralized profile service that manages user profile state.
/// Uses Preferences for persistence and events for cross-screen propagation.
/// </summary>
public interface IProfileService
{
    string FirstName { get; }
    string LastName { get; }
    string FullName { get; }
    string Address { get; }
    string Gender { get; }
    string? ProfilePhotoPath { get; }
    event EventHandler? ProfileChanged;

    // Keep backward-compat alias
    string UserName { get; }

    void SaveFirstName(string firstName);
    void SaveLastName(string lastName);
    void SaveName(string name);
    void SaveAddress(string address);
    void SaveGender(string gender);
    void SaveProfilePhoto(string? photoPath);
    string GetGreeting();
}

public sealed class ProfileService : IProfileService
{
    private const string FirstNameKey = "user_first_name";
    private const string LastNameKey = "user_last_name";
    private const string AddressKey = "user_address";
    private const string GenderKey = "user_gender";
    private const string PhotoKey = "user_photo_path";
    private const string LegacyNameKey = "user_name";
    private const string DefaultFirstName = "Athlete";

    public event EventHandler? ProfileChanged;

    public string FirstName
    {
        get
        {
            var first = Preferences.Default.Get(FirstNameKey, string.Empty);
            if (!string.IsNullOrEmpty(first)) return first;

            // Fallback to legacy single-name field
            var legacy = Preferences.Default.Get(LegacyNameKey, DefaultFirstName);
            return legacy;
        }
    }

    public string LastName => Preferences.Default.Get(LastNameKey, string.Empty);

    public string FullName
    {
        get
        {
            var first = FirstName;
            var last = LastName;
            return string.IsNullOrWhiteSpace(last)
                ? first
                : $"{first} {last}";
        }
    }

    // Backward-compat: returns FullName
    public string UserName => FullName;

    public string Address => Preferences.Default.Get(AddressKey, string.Empty);

    public string Gender => Preferences.Default.Get(GenderKey, string.Empty);

    public string? ProfilePhotoPath
    {
        get
        {
            var path = Preferences.Default.Get(PhotoKey, string.Empty);
            return string.IsNullOrEmpty(path) ? null : path;
        }
    }

    public void SaveFirstName(string firstName)
    {
        Preferences.Default.Set(FirstNameKey, firstName?.Trim() ?? string.Empty);
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveLastName(string lastName)
    {
        Preferences.Default.Set(LastNameKey, lastName?.Trim() ?? string.Empty);
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Legacy single-name save. Sets FirstName and clears LastName for backward compat.
    /// </summary>
    public void SaveName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var trimmed = name.Trim();
        Preferences.Default.Set(FirstNameKey, trimmed);
        Preferences.Default.Set(LegacyNameKey, trimmed);
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveAddress(string address)
    {
        Preferences.Default.Set(AddressKey, address?.Trim() ?? string.Empty);
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveGender(string gender)
    {
        Preferences.Default.Set(GenderKey, gender?.Trim() ?? string.Empty);
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveProfilePhoto(string? photoPath)
    {
        Preferences.Default.Set(PhotoKey, photoPath ?? string.Empty);
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetGreeting()
    {
        var hour = DateTime.Now.Hour;
        var timeGreeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };

        return timeGreeting;
    }
}
