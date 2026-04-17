namespace Fitness_App.Pages;

public partial class DownloadMyDataPage : ContentPage
{
    private string _selectedFormat = "JSON";

    public DownloadMyDataPage()
    {
        InitializeComponent();
        UpdateFormatUI();
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
        JsonBorder.Stroke = _selectedFormat == "JSON" ? Color.FromArgb("#FC5200") : Color.FromArgb("#243041");
        CsvBorder.Stroke = _selectedFormat == "CSV" ? Color.FromArgb("#FC5200") : Color.FromArgb("#243041");

        var jsonLabel = (Label)JsonBorder.Content;
        jsonLabel.TextColor = _selectedFormat == "JSON" ? Color.FromArgb("#FC5200") : Color.FromArgb("#94A3B8");

        var csvLabel = (Label)CsvBorder.Content;
        csvLabel.TextColor = _selectedFormat == "CSV" ? Color.FromArgb("#FC5200") : Color.FromArgb("#94A3B8");
    }

    private async void OnRequestExport(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        await DisplayAlert(
            "Export not available",
            $"{_selectedFormat} export is not wired up yet. No request was sent.",
            "OK");
    }
}
