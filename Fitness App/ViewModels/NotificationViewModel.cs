using CommunityToolkit.Mvvm.ComponentModel;
using Fitness_App.Models;
using System.Collections.ObjectModel;

namespace Fitness_App.ViewModels;

public partial class NotificationViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<AppNotification> _notifications = new();

    public NotificationViewModel()
    {
        LoadMockNotifications();
    }

    private void LoadMockNotifications()
    {
        var now = DateTime.Now;

        Notifications.Add(new AppNotification
        {
            Id = "1",
            Title = "Welcome to StrideX! 🎉",
            Message = "Start your fitness journey today",
            Timestamp = now.AddHours(-2),
            IsRead = false
        });

        Notifications.Add(new AppNotification
        {
            Id = "2",
            Title = "Daily Goal Achieved! 🏆",
            Message = "You've completed 10km today",
            Timestamp = now.AddDays(-1),
            IsRead = true
        });

        Notifications.Add(new AppNotification
        {
            Id = "3",
            Title = "New Feature Available",
            Message = "Check out our new progress tracking",
            Timestamp = now.AddDays(-2),
            IsRead = true
        });

        Notifications.Add(new AppNotification
        {
            Id = "4",
            Title = "Weekly Summary 📊",
            Message = "You ran 45km this week",
            Timestamp = now.AddDays(-5),
            IsRead = true
        });

        Notifications.Add(new AppNotification
        {
            Id = "5",
            Title = "Challenge Invitation",
            Message = "Join the 30-day running challenge",
            Timestamp = now.AddDays(-7),
            IsRead = false
        });
    }
}
