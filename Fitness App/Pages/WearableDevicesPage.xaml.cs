namespace Fitness_App.Pages;

public partial class WearableDevicesPage : ContentPage
{
    public WearableDevicesPage()
    {
        InitializeComponent();
    }

    private async void OnRemoveDevice(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        string deviceName = (sender as Button)?.CommandParameter as string ?? "this device";
        var confirm = await DisplayAlert("Remove Device",
            $"Are you sure you want to unpair {deviceName}?",
            "Remove", "Cancel");
        if (confirm)
        {
            await DisplayAlert("Device Removed", $"{deviceName} has been unpaired.", "OK");
        }
    }

    private async void OnAddDevice(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await DisplayAlert("Add New Device",
            "In a production app, this would open the Bluetooth pairing flow.",
            "OK");
    }
}
