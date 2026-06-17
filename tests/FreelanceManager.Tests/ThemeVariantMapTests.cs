using Avalonia.Styling;
using FreelanceManager.App.Services;
using FreelanceManager.Core.Models;
using Xunit;

namespace FreelanceManager.Tests;

public class ThemeVariantMapTests
{
    [Theory]
    [InlineData(ThemeMode.Light)]
    [InlineData(ThemeMode.Dark)]
    [InlineData(ThemeMode.System)]
    public void Resolve_maps_each_mode(ThemeMode mode)
    {
        var variant = ThemeVariantMap.Resolve(mode);

        var expected = mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        Assert.Equal(expected, variant);
    }
}
