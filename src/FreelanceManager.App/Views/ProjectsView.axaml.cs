using Avalonia.Controls;
using Avalonia.Interactivity;
using FreelanceManager.Core.Models;
using FreelanceManager.App.ViewModels;

namespace FreelanceManager.App.Views;

public partial class ProjectsView : UserControl
{
    public ProjectsView()
    {
        InitializeComponent();
    }

    private void OnSetProjectStatus(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: ProjectStatus status } menuItem) return;
        if (DataContext is not ProjectsViewModel vm) return;
        if (menuItem.DataContext is Project row) vm.Selected = row;
        vm.SetStatusCommand.Execute(status);
    }
}
