namespace Fitness_App.Pages;

public partial class SendFeedbackPage : ContentPage
{
    private string _selectedType = "Bug";

    public SendFeedbackPage()
    {
        InitializeComponent();
        UpdateTypeUI();
    }

    private void OnTypeSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string type)
        {
            _selectedType = type;
            UpdateTypeUI();
        }
    }

    private void UpdateTypeUI()
    {
        var activeStroke = Color.FromArgb("#FC5200");
        var inactiveStroke = Color.FromArgb("#243041");

        var activeTextColor = Color.FromArgb("#FC5200");
        var inactiveTextColor = Color.FromArgb("#94A3B8");

        BugBorder.Stroke = _selectedType == "Bug" ? activeStroke : inactiveStroke;
        SuggestionBorder.Stroke = _selectedType == "Suggestion" ? activeStroke : inactiveStroke;
        OtherBorder.Stroke = _selectedType == "Other" ? activeStroke : inactiveStroke;

        ((Label)BugBorder.Content).TextColor = _selectedType == "Bug" ? activeTextColor : inactiveTextColor;
        ((Label)SuggestionBorder.Content).TextColor = _selectedType == "Suggestion" ? activeTextColor : inactiveTextColor;
        ((Label)OtherBorder.Content).TextColor = _selectedType == "Other" ? activeTextColor : inactiveTextColor;
    }

    private void OnFeedbackTextChanged(object? sender, TextChangedEventArgs e)
    {
        int count = e.NewTextValue?.Length ?? 0;
        CharCount.Text = $"{count}/500";
    }

    private async void OnAddImage(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo != null)
            {
                await DisplayAlert("Image Added", $"Screenshot '{photo.FileName}' attached.", "OK");
            }
        }
        catch
        {
            await DisplayAlert("Error", "Could not pick an image. Please check permissions.", "OK");
        }
    }

    private async void OnSendFeedback(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var feedback = FeedbackEditor.Text?.Trim();
        if (string.IsNullOrEmpty(feedback))
        {
            await DisplayAlert("Feedback Required", "Please enter your feedback before submitting.", "OK");
            return;
        }

        // In production: post feedback to backend/Supabase
        await DisplayAlert("Thanks!", "Thanks for your feedback!", "OK");
        await Navigation.PopAsync();
    }
}
