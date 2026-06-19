using FreelanceManager.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FreelanceManager.Tests;

public class MainWindowViewModelQuickCreateTests
{
    [Fact]
    public void QuickNewProject_navigates_to_Projects_page()
    {
        var services = TestServices.Build();
        var vm = new MainWindowViewModel(services);

        vm.QuickNewProjectCommand.Execute(null);

        Assert.Equal("Projects", vm.ActivePage);
        Assert.IsType<ProjectsViewModel>(vm.CurrentPage);
    }
}
