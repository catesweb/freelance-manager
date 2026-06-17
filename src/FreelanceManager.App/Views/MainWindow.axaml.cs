using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using FreelanceManager.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var mgr = new WindowNotificationManager(this) { Position = NotificationPosition.BottomRight, MaxItems = 3 };
        App.Services.GetRequiredService<NotificationService>().Attach(mgr);
    }
}