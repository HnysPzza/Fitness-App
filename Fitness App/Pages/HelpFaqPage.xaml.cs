namespace Fitness_App.Pages;

public partial class HelpFaqPage : ContentPage
{
    private readonly List<(string category, string question, string answer)> _faqs = new()
    {
        ("GETTING STARTED", "How do I record an activity?", "Tap the Record tab at the bottom of the screen. Select your activity type, then press Start. Your GPS and stats will be tracked automatically. Press Stop when you're done."),
        ("GETTING STARTED", "How do I connect a device?", "Go to Settings → General Settings → Connected Services → Wearable Devices. Tap 'Add New Device' and follow the Bluetooth pairing instructions."),
        ("ACCOUNT", "How do I change my password?", "Go to Settings → General Settings → Privacy & Security → Change Password. Enter your current password along with your new password and confirm it."),
        ("ACCOUNT", "How do I delete my account?", "Go to Settings → General Settings → Account → Delete Account. You'll be asked to confirm twice and enter your password. This action is permanent and cannot be undone."),
        ("MAPS & GPS", "Why is my GPS inaccurate?", "GPS accuracy can be affected by tall buildings, dense forests, or indoor locations. Make sure Location Services are enabled for the app. Walking in an open area for a few seconds before starting helps acquire a better signal."),
        ("MAPS & GPS", "How do I view past routes?", "Open the Progress tab and tap on any past activity. A map view of your route will be shown along with your stats for that session."),
        ("NOTIFICATIONS", "How do I set workout reminders?", "Go to Settings → General Settings → Notifications → Workout Reminders. Toggle 'Enable Reminders' and set your preferred time and days."),
        ("NOTIFICATIONS", "Why am I not receiving notifications?", "Make sure Push Notifications are enabled in Settings → General Settings → Notifications. Also check your device's notification settings for this app."),
    };

    private List<(string category, string question, string answer)> _filteredFaqs;
    private readonly HashSet<int> _expandedItems = new();

    public HelpFaqPage()
    {
        InitializeComponent();
        _filteredFaqs = _faqs;
        BuildFaqUI();
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim().ToLowerInvariant() ?? string.Empty;
        _filteredFaqs = string.IsNullOrEmpty(query)
            ? _faqs
            : _faqs.Where(f =>
                f.question.ToLowerInvariant().Contains(query) ||
                f.answer.ToLowerInvariant().Contains(query)).ToList();
        _expandedItems.Clear();
        BuildFaqUI();
    }

    private void BuildFaqUI()
    {
        FaqContainer.Children.Clear();

        string? lastCategory = null;
        int idx = 0;

        foreach (var (category, question, answer) in _filteredFaqs)
        {
            var i = idx; // capture for closure

            if (category != lastCategory)
            {
                FaqContainer.Children.Add(new Label
                {
                    Text = category,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#9E9E9E"),
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(16, 20, 16, 8)
                });
                lastCategory = category;
            }

            // Divider
            FaqContainer.Children.Add(new BoxView
            {
                HeightRequest = 0.5,
                Color = Color.FromArgb("#E0E0E0"),
                Margin = new Thickness(16, 0, 0, 0)
            });

            var answerLabel = new Label
            {
                Text = answer,
                FontSize = 14,
                TextColor = Color.FromArgb("#9E9E9E"),
                Margin = new Thickness(16, 0, 16, 12),
                LineBreakMode = LineBreakMode.WordWrap,
                IsVisible = _expandedItems.Contains(i)
            };

            var chevron = new Label
            {
                Text = _expandedItems.Contains(i) ? "▲" : "▼",
                FontSize = 14,
                TextColor = Color.FromArgb("#BDBDBD"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            var questionGrid = new Grid
            {
                HeightRequest = 56,
                Padding = new Thickness(16, 0),
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
            };
            questionGrid.Add(new Label
            {
                Text = question,
                FontSize = 16,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000"),
                VerticalOptions = LayoutOptions.Center
            }, 0);
            questionGrid.Add(chevron, 1);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                if (_expandedItems.Contains(i))
                {
                    _expandedItems.Remove(i);
                    answerLabel.IsVisible = false;
                    chevron.Text = "▼";
                }
                else
                {
                    _expandedItems.Add(i);
                    answerLabel.IsVisible = true;
                    chevron.Text = "▲";
                }
            };
            questionGrid.GestureRecognizers.Add(tapGesture);

            FaqContainer.Children.Add(questionGrid);
            FaqContainer.Children.Add(answerLabel);

            idx++;
        }

        if (_filteredFaqs.Count == 0)
        {
            FaqContainer.Children.Add(new Label
            {
                Text = "No results found",
                FontSize = 16,
                TextColor = Color.FromArgb("#9E9E9E"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
        }
    }
}
