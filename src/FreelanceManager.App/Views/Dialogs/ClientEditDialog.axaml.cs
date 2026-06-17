using Avalonia.Controls;
using Avalonia.Interactivity;
using FreelanceManager.App.ViewModels;

namespace FreelanceManager.App.Views.Dialogs;

public partial class ClientEditDialog : Window
{
    public ClientEditDialog() => InitializeComponent();

    private void OnSave(object? s, RoutedEventArgs e)
    {
        if (DataContext is ClientEditViewModel vm && vm.IsValid)
            Close(true);
    }

    private void OnCancel(object? s, RoutedEventArgs e) => Close(false);
}
