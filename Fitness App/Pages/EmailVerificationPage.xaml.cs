using Fitness_App.ViewModels;

namespace Fitness_App.Pages;

/// <summary>
/// Shell passes query parameters to the Page class, not directly to the ViewModel.
/// We capture "email" here and push it to the ViewModel.
/// </summary>
[QueryProperty(nameof(Email), "email")]
public partial class EmailVerificationPage : ContentPage
{
    private readonly EmailVerificationViewModel _vm;
    private readonly Entry[] _boxes;

    // Shell sets this property before OnAppearing via the [QueryProperty] attribute.
    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            if (!string.IsNullOrEmpty(value))
                _vm.EmailAddress = Uri.UnescapeDataString(value);
        }
    }

    public EmailVerificationPage(EmailVerificationViewModel vm)
    {
        _vm = vm;
        BindingContext = vm;
        InitializeComponent();
        // _boxes must be set AFTER InitializeComponent so the x:Name fields exist.
        _boxes = new[] { Box1, Box2, Box3, Box4, Box5, Box6 };
    }

    /// <summary>
    /// Auto-advance to the next box when a digit is entered,
    /// or go back when a digit is deleted.
    /// </summary>
    private void OnDigitTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Guard: MAUI can fire TextChanged during InitializeComponent
        // before the constructor finishes — _boxes is still null at that point.
        if (_boxes is null) return;
        if (sender is not Entry entry) return;

        var idx = Array.IndexOf(_boxes, entry);
        if (idx < 0) return;

        if (!string.IsNullOrEmpty(e.NewTextValue) && idx < _boxes.Length - 1)
        {
            // Advance to next box
            _boxes[idx + 1].Focus();
        }
        else if (string.IsNullOrEmpty(e.NewTextValue) && idx > 0)
        {
            // Go back to previous box
            _boxes[idx - 1].Focus();
        }
    }
}
