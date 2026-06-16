using Avalonia;
using Avalonia.Controls;

namespace FreelanceManager.App.Controls;

public partial class EmptyState : UserControl
{
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Message));
    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Hint));

    public string? Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }

    public EmptyState() => InitializeComponent();
}
