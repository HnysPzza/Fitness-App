using Fitness_App.Services;

namespace Fitness_App.Pages;

public partial class DateTimeFormatSelectionPage : ContentPage
{
    private readonly ISettingsService _settings;

    public DateTimeFormatSelectionPage(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        UpdateDateUI(_settings.DateFormat);
        UpdateTimeUI(_settings.TimeFormat);
        UpdatePreview();
    }

    private void OnDateFormatSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string format)
        {
            _settings.DateFormat = format;
            UpdateDateUI(format);
            UpdatePreview();
        }
    }

    private void OnTimeFormatSelected(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (e.Parameter is string format)
        {
            _settings.TimeFormat = format;
            UpdateTimeUI(format);
            UpdatePreview();
        }
    }

    private void UpdateDateUI(string format)
    {
        DdCheck.IsVisible = format == "DD/MM/YYYY";
        DdUncheck.IsVisible = format != "DD/MM/YYYY";
        MmCheck.IsVisible = format == "MM/DD/YYYY";
        MmUncheck.IsVisible = format != "MM/DD/YYYY";
        IsoCheck.IsVisible = format == "YYYY-MM-DD";
        IsoUncheck.IsVisible = format != "YYYY-MM-DD";
    }

    private void UpdateTimeUI(string format)
    {
        H12Check.IsVisible = format == "12-Hour (AM/PM)";
        H12Uncheck.IsVisible = format != "12-Hour (AM/PM)";
        H24Check.IsVisible = format == "24-Hour";
        H24Uncheck.IsVisible = format != "24-Hour";
    }

    private void UpdatePreview()
    {
        var now = DateTime.Now;
        var dateFmt = _settings.DateFormat;
        var timeFmt = _settings.TimeFormat;

        string dateStr = dateFmt switch
        {
            "DD/MM/YYYY" => now.ToString("dd/MM/yyyy"),
            "MM/DD/YYYY" => now.ToString("MM/dd/yyyy"),
            "YYYY-MM-DD" => now.ToString("yyyy-MM-dd"),
            _ => now.ToString("dd/MM/yyyy")
        };

        string timeStr = timeFmt switch
        {
            "12-Hour (AM/PM)" => now.ToString("h:mm tt"),
            "24-Hour" => now.ToString("HH:mm"),
            _ => now.ToString("h:mm tt")
        };

        PreviewLabel.Text = $"{dateStr}   {timeStr}";
    }
}
