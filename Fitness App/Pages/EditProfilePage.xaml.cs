using Fitness_App.Services;

namespace Fitness_App.Pages
{
    public partial class EditProfilePage : ContentPage
    {
        private IProfileService? _profileService;
        private string? _selectedPhotoPath;

        public EditProfilePage()
        {
            InitializeComponent();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();
            LoadProfileData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _profileService ??= Handler?.MauiContext?.Services.GetService<IProfileService>();
            LoadProfileData();
        }

        private void LoadProfileData()
        {
            if (_profileService == null) return;

            // Load existing profile data
            FirstNameEntry.Text = _profileService.FirstName;
            LastNameEntry.Text = _profileService.LastName;
            AddressEntry.Text = _profileService.Address;

            // Load gender
            if (!string.IsNullOrEmpty(_profileService.Gender))
            {
                var genderIndex = GenderPicker.Items.IndexOf(_profileService.Gender);
                if (genderIndex >= 0)
                    GenderPicker.SelectedIndex = genderIndex;
            }

            // Load profile picture
            if (!string.IsNullOrEmpty(_profileService.ProfilePhotoPath) && File.Exists(_profileService.ProfilePhotoPath))
            {
                ProfileImage.Source = ImageSource.FromFile(_profileService.ProfilePhotoPath);
                ProfileImage.IsVisible = true;
                ProfileIcon.IsVisible = false;
                ProfileIconContainer.IsVisible = false;
                _selectedPhotoPath = _profileService.ProfilePhotoPath;
            }
        }

        private async void OnTakePhotoClicked(object? sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }

            try
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        await ProcessSelectedPhoto(photo);
                    }
                }
                else
                {
                    await DisplayAlert("Not Supported", "Camera is not available on this device.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to take photo: {ex.Message}", "OK");
            }
        }

        private async void OnPickPhotoClicked(object? sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }

            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null)
                {
                    await ProcessSelectedPhoto(photo);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to pick photo: {ex.Message}", "OK");
            }
        }

        private async Task ProcessSelectedPhoto(FileResult photo)
        {
            // Save to app data directory
            var appDataDir = FileSystem.AppDataDirectory;
            var profilePicsDir = Path.Combine(appDataDir, "ProfilePictures");
            Directory.CreateDirectory(profilePicsDir);

            var fileName = $"profile_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(profilePicsDir, fileName);

            using (var stream = await photo.OpenReadAsync())
            using (var fileStream = File.Create(filePath))
            {
                await stream.CopyToAsync(fileStream);
            }

            _selectedPhotoPath = filePath;

            // Update UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProfileImage.Source = ImageSource.FromFile(filePath);
                ProfileImage.IsVisible = true;
                ProfileIcon.IsVisible = false;
                ProfileIconContainer.IsVisible = false;
            });
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (_profileService == null)
                return;

            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }

            // Validate inputs
            var firstName = FirstNameEntry.Text?.Trim();
            var lastName = LastNameEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                await DisplayAlert("Validation Error", "Please enter both first and last name.", "OK");
                return;
            }

            // Disable save button during save
            SaveButton.IsEnabled = false;
            SaveButton.Text = "Saving...";

            try
            {
                // Update profile service using Save methods
                _profileService.SaveFirstName(firstName);
                _profileService.SaveLastName(lastName);
                _profileService.SaveAddress(AddressEntry.Text?.Trim() ?? string.Empty);
                
                if (GenderPicker.SelectedIndex >= 0)
                {
                    _profileService.SaveGender(GenderPicker.Items[GenderPicker.SelectedIndex]);
                }

                if (!string.IsNullOrEmpty(_selectedPhotoPath))
                {
                    _profileService.SaveProfilePhoto(_selectedPhotoPath);
                }

                // Show success message
                SaveStatusLabel.Text = "✓ Profile saved successfully!";
                SaveStatusLabel.IsVisible = true;

                // Animate success
                await SaveStatusLabel.FadeToAsync(1, 300);
                await Task.Delay(2000);

                // Navigate back
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Text = "Save Changes";
                SaveStatusLabel.IsVisible = false;
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }

            await Shell.Current.GoToAsync("..");
        }
    }
}
