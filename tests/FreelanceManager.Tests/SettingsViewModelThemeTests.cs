using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class SettingsViewModelThemeTests
{
    private sealed class FakeProfiles : IBusinessProfileRepository
    {
        public BusinessProfile Saved { get; private set; } = new();
        public Task<BusinessProfile> GetAsync() => Task.FromResult(Saved);
        public Task SaveAsync(BusinessProfile profile) { Saved = profile; return Task.CompletedTask; }
    }

    private sealed class FakeBackup : IBackupService
    {
        public Task<string> BackupAsync(string databasePath, string targetDir) => Task.FromResult("x");
    }

    private sealed class FakeTheme : IThemeService
    {
        public ThemeMode? Applied { get; private set; }
        public void Apply(ThemeMode mode) => Applied = mode;
    }

    private sealed class FakeNotes : INotificationService
    {
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { }
    }

    [Fact]
    public async Task Save_persists_and_applies_selected_theme()
    {
        var profiles = new FakeProfiles();
        var theme = new FakeTheme();
        var vm = new SettingsViewModel(profiles, new FakeBackup(), theme, new FakeNotes());
        await Task.Delay(50); // allow LoadAsync to complete

        vm.Theme = ThemeMode.Dark;
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(ThemeMode.Dark, profiles.Saved.Theme);
        Assert.Equal(ThemeMode.Dark, theme.Applied);
    }
}
