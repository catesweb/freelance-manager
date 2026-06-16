using Avalonia.Controls.Notifications;

namespace FreelanceManager.App.Services;

public class NotificationService : INotificationService
{
    private WindowNotificationManager? _manager;

    public void Attach(WindowNotificationManager manager) => _manager = manager;

    public void Show(string message, NotificationKind kind = NotificationKind.Info)
    {
        var type = kind switch
        {
            NotificationKind.Success => NotificationType.Success,
            NotificationKind.Error => NotificationType.Error,
            _ => NotificationType.Information
        };
        _manager?.Show(new Notification(null, message, type));
    }
}
