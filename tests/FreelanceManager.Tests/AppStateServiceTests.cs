using System.IO;
using FreelanceManager.App.Services;
using Xunit;

namespace FreelanceManager.Tests;

public class AppStateServiceTests
{
    [Fact]
    public void Dismiss_persists_across_instances()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fm-state-{System.Guid.NewGuid():N}.json");
        try
        {
            var a = new AppStateService(path);
            Assert.False(a.OnboardingDismissed);
            a.DismissOnboarding();

            var b = new AppStateService(path);
            Assert.True(b.OnboardingDismissed);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
