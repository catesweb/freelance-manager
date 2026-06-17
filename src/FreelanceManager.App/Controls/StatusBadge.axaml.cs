using System;
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged -= OnThemeVariantChanged;
    }

    // The status->brush converter only re-runs when Status changes, so re-poke it
    // when the theme variant flips at runtime to keep the badge colors correct.
    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        var current = Status;
        Status = null;
        Status = current;
    }
}
