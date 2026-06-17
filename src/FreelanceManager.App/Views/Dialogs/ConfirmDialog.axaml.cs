using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FreelanceManager.App.Views.Dialogs;

public partial class ConfirmDialog : Window
{
    public string TitleText { get; init; } = "";
    public string MessageText { get; init; } = "";
    public string ConfirmText { get; init; } = "Confirm";
    public string CancelText { get; init; } = "Cancel";

    public ConfirmDialog() { InitializeComponent(); DataContext = this; }

    private void OnConfirm(object? s, RoutedEventArgs e) => Close(true);
    private void OnCancel(object? s, RoutedEventArgs e) => Close(false);
}
