using Fitness_App.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Fitness_App.Pages;

public partial class SwitchAccountPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly IAccountSessionStore _accountStore;
    private readonly IProfileService _profile;
    private IReadOnlyList<SavedAccountSession> _savedAccounts = Array.Empty<SavedAccountSession>();

    public SwitchAccountPage(
        ISupabaseService supabase,
        IAccountSessionStore accountStore,
        IProfileService profile)
    {
        InitializeComponent();
        _supabase = supabase;
        _accountStore = accountStore;
        _profile = profile;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        await _supabase.InitializeAsync();
        _savedAccounts = await _accountStore.GetAccountsAsync();

        CurrentAccountLayout.Children.Clear();
        SavedAccountsLayout.Children.Clear();

        var currentUser = _supabase.CurrentUser;
        if (currentUser == null)
        {
            CurrentAccountLayout.Children.Add(CreateAccountRow("No active account", "Sign in to continue", null, true));
        }
        else
        {
            var name = string.IsNullOrWhiteSpace(_profile.FullName) ? currentUser.Email ?? "Current account" : _profile.FullName;
            CurrentAccountLayout.Children.Add(CreateAccountRow(name, currentUser.Email ?? "Signed in", currentUser.Id, true));
        }

        var currentUserId = currentUser?.Id;
        var otherAccounts = _savedAccounts
            .Where(account => account.UserId != currentUserId)
            .ToList();

        EmptyAccountsLabel.IsVisible = otherAccounts.Count == 0;

        foreach (var account in otherAccounts)
        {
            SavedAccountsLayout.Children.Add(CreateAccountRow(
                account.DisplayName,
                account.Email,
                account.UserId,
                false));
        }
    }

    private View CreateAccountRow(string title, string subtitle, string? userId, bool isCurrent)
    {
        var textColor = IsLightTheme() ? Color.FromArgb("#0F172A") : Color.FromArgb("#F8FAFC");
        var mutedColor = IsLightTheme() ? Color.FromArgb("#64748B") : Color.FromArgb("#94A3B8");
        var avatarColor = isCurrent ? Color.FromArgb("#FC5200") : Color.FromArgb("#2563EB");

        var row = new Grid
        {
            Padding = new Thickness(18, 14),
            MinimumHeightRequest = 78,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        if (!isCurrent && !string.IsNullOrWhiteSpace(userId))
        {
            var tapGesture = new TapGestureRecognizer
            {
                CommandParameter = userId,
                NumberOfTapsRequired = 1
            };
            tapGesture.Tapped += OnSwitchAccount;
            row.GestureRecognizers.Add(tapGesture);
        }

        var avatar = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            BackgroundColor = avatarColor,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(22) },
            Margin = new Thickness(0, 0, 12, 0),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = GetInitial(title, subtitle),
                TextColor = Colors.White,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        row.Add(avatar, 0, 0);

        var textStack = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontSize = 16,
                    FontAttributes = isCurrent ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = textColor,
                    LineBreakMode = LineBreakMode.TailTruncation
                },
                new Label
                {
                    Text = subtitle,
                    FontSize = 13,
                    TextColor = mutedColor,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        };
        row.Add(textStack, 1, 0);

        row.Add(new Label
        {
            Text = isCurrent ? "Active" : "Switch",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = isCurrent ? Color.FromArgb("#FC5200") : mutedColor,
            VerticalOptions = LayoutOptions.Center
        }, 2, 0);

        return row;
    }

    private async void OnSwitchAccount(object? sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        if (e.Parameter is not string userId)
            return;

        var account = _savedAccounts.FirstOrDefault(saved => saved.UserId == userId);
        if (account == null)
            return;

        var confirm = await DisplayAlert("Switch Account",
            $"Switch to {account.Email}?", "Switch", "Cancel");
        if (!confirm)
            return;

        var activated = await _accountStore.ActivateSessionAsync(userId);
        if (!activated)
        {
            await DisplayAlert("Account unavailable",
                "This saved account could not be restored. Please sign in again.",
                "OK");
            return;
        }

        await _supabase.ReloadPersistedSessionAsync();

        var profile = await _supabase.GetCurrentProfileAsync();
        if (profile != null)
            _profile.SyncFromSupabase(profile);

        await LoadAccountsAsync();

        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//home");
    }

    private async void OnAddAccount(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var proceed = await DisplayAlert("Add Account",
            "Sign in once with another account. After that, it will be saved on this phone for quick switching.",
            "Continue", "Cancel");
        if (!proceed || Shell.Current == null)
            return;

        await Shell.Current.GoToAsync("//login");
    }

    private static string GetInitial(string title, string subtitle)
    {
        var source = string.IsNullOrWhiteSpace(title) ? subtitle : title;
        return string.IsNullOrWhiteSpace(source)
            ? "?"
            : source.Trim()[0].ToString().ToUpperInvariant();
    }

    private static bool IsLightTheme()
    {
        return Application.Current?.RequestedTheme == AppTheme.Light;
    }
}
