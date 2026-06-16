using Avalonia;
using Avalonia.Controls;

namespace FreelanceManager.App.Controls;

public partial class StatusBadge : UserControl
{
    public static readonly StyledProperty<object?> StatusProperty =
        AvaloniaProperty.Register<StatusBadge, object?>(nameof(Status));

    public object? Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public StatusBadge() => InitializeComponent();
}
