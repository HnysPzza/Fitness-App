namespace Fitness_App.Pages;

public partial class DownloadMyDataPage : ContentPage
{
    private string _selectedFormat = "JSON";

    public DownloadMyDataPage()
    {
        InitializeComponent();
    }

    private void OnFormatSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string fmt)
        {
            _selectedFormat = fmt;
            UpdateFormatUI();
        }
    }

    private void UpdateFormatUI()
    {
        JsonBorder.Stroke = _selectedFormat == "JSON" ? Color.FromArgb("#FC5200") : Color.FromArgb("#E0E0E0");
        CsvBorder.Stroke = _selectedFormat == "CSV" ? Color.FromArgb("#FC5200") : Color.FromArgb("#E0E0E0");

        var jsonLabel = (Label)JsonBorder.Content;
        jsonLabel.TextColor = _selectedFormat == "JSON" ? Color.FromArgb("#FC5200") : Color.FromArgb("#9E9E9E");

        var csvLabel = (Label)CsvBorder.Content;
        csvLabel.TextColor = _selectedFormat == "CSV" ? Color.FromArgb("#FC5200") : Color.FromArgb("#9E9E9E");
    }

    private async void OnRequestExport(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var confirm = await DisplayAlert("Request Export",
            $"You are about to request a {_selectedFormat} export of your data. You'll receive an email when it's ready. Continue?",
            "Request", "Cancel");

        if (confirm)
        {
            // In production, call Supabase export API here
            await DisplayAlert("Export Requested", "Export requested. You'll be notified when ready.", "OK");
        }
    }
}
