namespace FreelanceManager.App.Services;

public enum NotificationKind { Success, Error, Info }

public interface INotificationService
{
    void Show(string message, NotificationKind kind = NotificationKind.Info);
}
