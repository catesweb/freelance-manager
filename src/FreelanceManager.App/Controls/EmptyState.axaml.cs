using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FreelanceManager.App.Controls;

public partial class EmptyState : UserControl
{
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Message));
    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Hint));

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<EmptyState, Geometry?>(nameof(Icon));
    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(ActionText));
    public static readonly StyledProperty<System.Windows.Input.ICommand?> CommandProperty =
        AvaloniaProperty.Register<EmptyState, System.Windows.Input.ICommand?>(nameof(Command));

    public string? Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? ActionText { get => GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public System.Windows.Input.ICommand? Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    public EmptyState() => InitializeComponent();
}
