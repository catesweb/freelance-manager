# Design System & Shell Foundation Implementation Plan

**Goal:** Replace Freelance Manager's default-themed, hardcoded-hex UI with a token-based design system (light + dark), reusable component styles, a redesigned app shell, and a hybrid editor framework — then retrofit all five existing views onto it.

**Architecture:** All work lands in `FreelanceManager.App` plus the test project, with one new persisted setting in Core/Data. Colors/spacing/typography become named resources in `ThemeDictionaries` referenced via `DynamicResource`, so Avalonia swaps light/dark live. `ThemeVariant.Default` follows the OS automatically — "System" mode needs no manual OS listener. Editing moves from a docked side panel to a hybrid: full-page editors for Projects/Invoices, modal dialogs (via an injected `IDialogService`) for Clients and confirmations, with an `INotificationService` replacing bottom red-text status lines.

**Tech Stack:** .NET 10, Avalonia UI 12.0.4 (Fluent base + Inter font), CommunityToolkit.Mvvm, EF Core + SQLite, Microsoft.Extensions.DependencyInjection, xUnit tests.

**Spec:** [docs/specs/2026-06-16-design-system-shell-foundation-design.md](../specs/2026-06-16-design-system-shell-foundation-design.md)

---

## File Structure

**New files:**
- `src/FreelanceManager.App/Themes/Tokens.axaml` — ThemeDictionaries: Light + Dark color/radius/elevation values.
- `src/FreelanceManager.App/Themes/Typography.axaml` — text styles (PageTitle, SectionHeading, Body, Caption).
- `src/FreelanceManager.App/Themes/Controls.axaml` — button/input/card styles.
- `src/FreelanceManager.App/Themes/Icons.axaml` — Lucide `StreamGeometry` path resources.
- `src/FreelanceManager.App/Controls/StatusBadge.axaml(.cs)` — status pill.
- `src/FreelanceManager.App/Controls/EmptyState.axaml(.cs)` — empty-list placeholder.
- `src/FreelanceManager.App/Controls/PageHeader.axaml(.cs)` — title + action region.
- `src/FreelanceManager.App/Services/IThemeService.cs`, `ThemeService.cs`, `ThemeVariantMap.cs`.
- `src/FreelanceManager.App/Services/IDialogService.cs`, `DialogService.cs`.
- `src/FreelanceManager.App/Services/INotificationService.cs`, `NotificationService.cs`.
- `src/FreelanceManager.App/Converters/StatusToBrushConverter.cs`.
- `src/FreelanceManager.App/Views/ProjectEditView.axaml(.cs)` — full-page project editor.
- `src/FreelanceManager.App/Views/InvoiceEditView.axaml(.cs)` — full-page invoice editor.
- `src/FreelanceManager.App/Views/Dialogs/ClientEditDialog.axaml(.cs)` — modal client form.
- `src/FreelanceManager.App/Views/Dialogs/ConfirmDialog.axaml(.cs)` — confirmation modal.
- `src/FreelanceManager.Core/Models/ThemeMode.cs` — `enum { System, Light, Dark }`.
- Migration under `src/FreelanceManager.Data/Migrations/`.
- Test files under `tests/FreelanceManager.Tests/`.

**Modified files:**
- `src/FreelanceManager.App/App.axaml` — merge theme dictionaries.
- `src/FreelanceManager.App/App.axaml.cs` — apply persisted theme on startup.
- `src/FreelanceManager.App/Views/MainWindow.axaml` — redesigned sidebar.
- `src/FreelanceManager.App/ServiceConfiguration.cs` — register new services.
- `src/FreelanceManager.App/ViewModels/*` — use dialog/notification services; editor host changes.
- `src/FreelanceManager.App/Views/*` — retrofit onto tokens/components.
- `src/FreelanceManager.Core/Models/BusinessProfile.cs` — add `Theme` property.
- `src/FreelanceManager.Data/AppDbContext.cs` — (only if explicit config needed; default maps enum to int).

---

## Task 1: Theme tokens (light + dark)

**Files:**
- Create: `src/FreelanceManager.App/Themes/Tokens.axaml`
- Modify: `src/FreelanceManager.App/App.axaml`

- [ ] **Step 1: Create the token dictionary**

Create `src/FreelanceManager.App/Themes/Tokens.axaml`:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ResourceDictionary.ThemeDictionaries>

    <ResourceDictionary x:Key="Light">
      <SolidColorBrush x:Key="BgCanvas"      Color="#FFFFFF"/>
      <SolidColorBrush x:Key="BgSurface"     Color="#F6F8FB"/>
      <SolidColorBrush x:Key="BgSurfaceAlt"  Color="#EEF2F8"/>
      <SolidColorBrush x:Key="Border"        Color="#E2E6EC"/>
      <SolidColorBrush x:Key="TextPrimary"   Color="#1F2733"/>
      <SolidColorBrush x:Key="TextMuted"     Color="#7C8896"/>
      <SolidColorBrush x:Key="AccentPrimary" Color="#4F6BED"/>
      <SolidColorBrush x:Key="AccentPrimaryHover" Color="#3A52C7"/>
      <SolidColorBrush x:Key="AccentSubtle"  Color="#E3E8FB"/>
      <SolidColorBrush x:Key="Success"       Color="#2E9E5B"/>
      <SolidColorBrush x:Key="SuccessSubtle" Color="#E2F4E9"/>
      <SolidColorBrush x:Key="Warning"       Color="#C98A00"/>
      <SolidColorBrush x:Key="WarningSubtle" Color="#FBF0D6"/>
      <SolidColorBrush x:Key="Danger"        Color="#C0392B"/>
      <SolidColorBrush x:Key="DangerSubtle"  Color="#FCE6E3"/>
      <SolidColorBrush x:Key="Info"          Color="#5A6678"/>
      <SolidColorBrush x:Key="InfoSubtle"    Color="#ECEFF3"/>
    </ResourceDictionary>

    <ResourceDictionary x:Key="Dark">
      <SolidColorBrush x:Key="BgCanvas"      Color="#161C25"/>
      <SolidColorBrush x:Key="BgSurface"     Color="#1E2630"/>
      <SolidColorBrush x:Key="BgSurfaceAlt"  Color="#232C38"/>
      <SolidColorBrush x:Key="Border"        Color="#2A333F"/>
      <SolidColorBrush x:Key="TextPrimary"   Color="#E6EBF2"/>
      <SolidColorBrush x:Key="TextMuted"     Color="#9AA5B5"/>
      <SolidColorBrush x:Key="AccentPrimary" Color="#6B83F0"/>
      <SolidColorBrush x:Key="AccentPrimaryHover" Color="#8197F4"/>
      <SolidColorBrush x:Key="AccentSubtle"  Color="#222C44"/>
      <SolidColorBrush x:Key="Success"       Color="#34D399"/>
      <SolidColorBrush x:Key="SuccessSubtle" Color="#0F3D2E"/>
      <SolidColorBrush x:Key="Warning"       Color="#E0B341"/>
      <SolidColorBrush x:Key="WarningSubtle" Color="#3A2F12"/>
      <SolidColorBrush x:Key="Danger"        Color="#F87171"/>
      <SolidColorBrush x:Key="DangerSubtle"  Color="#3A1E1E"/>
      <SolidColorBrush x:Key="Info"          Color="#5A6678"/>
      <SolidColorBrush x:Key="InfoSubtle"    Color="#222B36"/>
    </ResourceDictionary>

  </ResourceDictionary.ThemeDictionaries>

  <!-- Non-theme-varying tokens -->
  <x:Double x:Key="Space1">4</x:Double>
  <x:Double x:Key="Space2">8</x:Double>
  <x:Double x:Key="Space3">12</x:Double>
  <x:Double x:Key="Space4">16</x:Double>
  <x:Double x:Key="Space6">24</x:Double>
  <x:Double x:Key="Space8">32</x:Double>

  <CornerRadius x:Key="RadiusSm">6</CornerRadius>
  <CornerRadius x:Key="RadiusMd">9</CornerRadius>
  <CornerRadius x:Key="RadiusLg">12</CornerRadius>

  <Thickness x:Key="PagePadding">24</Thickness>
</ResourceDictionary>
```

- [ ] **Step 2: Merge it in App.axaml**

In `src/FreelanceManager.App/App.axaml`, add a merged dictionary inside `<Application.Resources>` (add the element — it does not exist yet) above `<Application.Styles>`:

```xml
  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source="avares://FreelanceManager.App/Themes/Tokens.axaml"/>
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Application.Resources>
```

- [ ] **Step 3: Build to verify resources compile**

Run: `dotnet build src/FreelanceManager.App/FreelanceManager.App.csproj`
Expected: Build succeeds (0 errors). XAML compilation validates the dictionary.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Themes/Tokens.axaml src/FreelanceManager.App/App.axaml
git commit -m "feat(app): add light/dark design tokens"
```

---

## Task 2: Typography styles

**Files:**
- Create: `src/FreelanceManager.App/Themes/Typography.axaml`
- Modify: `src/FreelanceManager.App/App.axaml`

- [ ] **Step 1: Create typography styles**

Create `src/FreelanceManager.App/Themes/Typography.axaml`:

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style Selector="TextBlock.PageTitle">
    <Setter Property="FontSize" Value="22"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
  </Style>
  <Style Selector="TextBlock.SectionHeading">
    <Setter Property="FontSize" Value="15"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
  </Style>
  <Style Selector="TextBlock.Body">
    <Setter Property="FontSize" Value="13"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
  </Style>
  <Style Selector="TextBlock.Caption">
    <Setter Property="FontSize" Value="11"/>
    <Setter Property="Foreground" Value="{DynamicResource TextMuted}"/>
  </Style>
  <Style Selector="TextBlock.FieldLabel">
    <Setter Property="FontSize" Value="11"/>
    <Setter Property="Foreground" Value="{DynamicResource TextMuted}"/>
    <Setter Property="Margin" Value="0,0,0,2"/>
  </Style>
</Styles>
```

- [ ] **Step 2: Include it in App.axaml styles**

In `src/FreelanceManager.App/App.axaml`, inside `<Application.Styles>` (after `<FluentTheme />`), add:

```xml
        <StyleInclude Source="avares://FreelanceManager.App/Themes/Typography.axaml"/>
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/FreelanceManager.App/FreelanceManager.App.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Themes/Typography.axaml src/FreelanceManager.App/App.axaml
git commit -m "feat(app): add typography styles"
```

---

## Task 3: Component styles (buttons, inputs, cards)

**Files:**
- Create: `src/FreelanceManager.App/Themes/Controls.axaml`
- Modify: `src/FreelanceManager.App/App.axaml`

- [ ] **Step 1: Create component styles**

Create `src/FreelanceManager.App/Themes/Controls.axaml`:

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Primary button -->
  <Style Selector="Button.primary">
    <Setter Property="Background" Value="{DynamicResource AccentPrimary}"/>
    <Setter Property="Foreground" Value="#FFFFFF"/>
    <Setter Property="Padding" Value="14,7"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSm}"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
  </Style>
  <Style Selector="Button.primary:pointerover /template/ ContentPresenter">
    <Setter Property="Background" Value="{DynamicResource AccentPrimaryHover}"/>
  </Style>

  <!-- Secondary button -->
  <Style Selector="Button.secondary">
    <Setter Property="Background" Value="{DynamicResource BgSurfaceAlt}"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
    <Setter Property="Padding" Value="14,7"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSm}"/>
  </Style>

  <!-- Ghost button -->
  <Style Selector="Button.ghost">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
    <Setter Property="Padding" Value="10,6"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSm}"/>
  </Style>

  <!-- Danger button -->
  <Style Selector="Button.danger">
    <Setter Property="Background" Value="{DynamicResource DangerSubtle}"/>
    <Setter Property="Foreground" Value="{DynamicResource Danger}"/>
    <Setter Property="Padding" Value="14,7"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSm}"/>
  </Style>

  <!-- Card / surface container -->
  <Style Selector="Border.card">
    <Setter Property="Background" Value="{DynamicResource BgSurface}"/>
    <Setter Property="BorderBrush" Value="{DynamicResource Border}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusMd}"/>
    <Setter Property="Padding" Value="16"/>
  </Style>

  <!-- Text inputs -->
  <Style Selector="TextBox">
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSm}"/>
  </Style>
</Styles>
```

- [ ] **Step 2: Include it in App.axaml**

In `src/FreelanceManager.App/App.axaml`, inside `<Application.Styles>` after the Typography include, add:

```xml
        <StyleInclude Source="avares://FreelanceManager.App/Themes/Controls.axaml"/>
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/FreelanceManager.App/FreelanceManager.App.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Themes/Controls.axaml src/FreelanceManager.App/App.axaml
git commit -m "feat(app): add reusable button/input/card styles"
```

---

## Task 4: Lucide icon geometries

**Files:**
- Create: `src/FreelanceManager.App/Themes/Icons.axaml`
- Modify: `src/FreelanceManager.App/App.axaml`

- [ ] **Step 1: Create the icon resource dictionary**

Create `src/FreelanceManager.App/Themes/Icons.axaml` with Lucide path data (24×24 viewbox) as `StreamGeometry` resources:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- Lucide: layout-dashboard -->
  <StreamGeometry x:Key="IconDashboard">M3 3h7v9H3z M14 3h7v5h-7z M14 12h7v9h-7z M3 16h7v5H3z</StreamGeometry>
  <!-- Lucide: users -->
  <StreamGeometry x:Key="IconClients">M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M22 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75</StreamGeometry>
  <!-- Lucide: folder-kanban -->
  <StreamGeometry x:Key="IconProjects">M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.93a2 2 0 0 1-1.66-.9l-.82-1.2A2 2 0 0 0 8.93 3H4a2 2 0 0 0-2 2v13c0 1.1.9 2 2 2z M8 10v6 M12 10v3 M16 10v8</StreamGeometry>
  <!-- Lucide: file-text -->
  <StreamGeometry x:Key="IconInvoices">M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M16 13H8 M16 17H8 M10 9H8</StreamGeometry>
  <!-- Lucide: settings -->
  <StreamGeometry x:Key="IconSettings">M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z</StreamGeometry>
</ResourceDictionary>
```

- [ ] **Step 2: Merge it in App.axaml**

In `src/FreelanceManager.App/App.axaml`, add another `<ResourceInclude>` inside the `MergedDictionaries` created in Task 1:

```xml
        <ResourceInclude Source="avares://FreelanceManager.App/Themes/Icons.axaml"/>
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/FreelanceManager.App/FreelanceManager.App.csproj`
Expected: Build succeeds. If any path string fails to parse, the build errors on that geometry — fix the offending `d` data.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Themes/Icons.axaml src/FreelanceManager.App/App.axaml
git commit -m "feat(app): add Lucide icon geometries"
```

---

## Task 5: ThemeMode enum + BusinessProfile property + migration

**Files:**
- Create: `src/FreelanceManager.Core/Models/ThemeMode.cs`
- Modify: `src/FreelanceManager.Core/Models/BusinessProfile.cs`
- Test: `tests/FreelanceManager.Tests/BusinessProfileRepositoryTests.cs`
- Create: migration via EF CLI

- [ ] **Step 1: Write a failing test for theme persistence**

Add to `tests/FreelanceManager.Tests/BusinessProfileRepositoryTests.cs` (new test method; keep existing tests):

```csharp
[Fact]
public async Task SaveAsync_persists_theme_preference()
{
    using var db = TestDb.NewContext();
    var repo = new BusinessProfileRepository(TestDb.Factory(db));
    var profile = await repo.GetAsync();
    profile.Theme = ThemeMode.Dark;

    await repo.SaveAsync(profile);
    var reloaded = await repo.GetAsync();

    Assert.Equal(ThemeMode.Dark, reloaded.Theme);
}
```

Add `using FreelanceManager.Core.Models;` at the top if not present. If `TestDb` does not expose `Factory`/`NewContext` exactly as above, match this test to the existing helpers used by the other tests in this file (read the file first and mirror its setup pattern).

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/FreelanceManager.Tests/FreelanceManager.Tests.csproj --filter SaveAsync_persists_theme_preference`
Expected: FAIL — `ThemeMode` / `Theme` do not exist (compile error).

- [ ] **Step 3: Add the enum**

Create `src/FreelanceManager.Core/Models/ThemeMode.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}
```

- [ ] **Step 4: Add the property to BusinessProfile**

In `src/FreelanceManager.Core/Models/BusinessProfile.cs`, add after `InvoiceNumberFormat`:

```csharp
    public ThemeMode Theme { get; set; } = ThemeMode.System;
```

- [ ] **Step 5: Create the EF migration**

Run: `dotnet ef migrations add AddThemePreference --project src/FreelanceManager.Data --startup-project src/FreelanceManager.App`
Expected: A new migration appears under `src/FreelanceManager.Data/Migrations/` adding a `Theme` integer column to `BusinessProfiles` with default 0.

(If `dotnet ef` is not installed: `dotnet tool install --global dotnet-ef`.)

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/FreelanceManager.Tests/FreelanceManager.Tests.csproj --filter SaveAsync_persists_theme_preference`
Expected: PASS.

- [ ] **Step 7: Run the full suite to confirm no regressions**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FreelanceManager.Core/Models/ThemeMode.cs src/FreelanceManager.Core/Models/BusinessProfile.cs src/FreelanceManager.Data/Migrations tests/FreelanceManager.Tests/BusinessProfileRepositoryTests.cs
git commit -m "feat: persist theme preference on business profile"
```

---

## Task 6: Theme service + variant mapping

**Files:**
- Create: `src/FreelanceManager.App/Services/ThemeVariantMap.cs`
- Create: `src/FreelanceManager.App/Services/IThemeService.cs`
- Create: `src/FreelanceManager.App/Services/ThemeService.cs`
- Test: `tests/FreelanceManager.Tests/ThemeVariantMapTests.cs`

- [ ] **Step 1: Write a failing test for the mapping**

Create `tests/FreelanceManager.Tests/ThemeVariantMapTests.cs`:

```csharp
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
```

Confirm the test project references `FreelanceManager.App` (the existing `ClientEditViewModelTests` prove it does; if not, add a `ProjectReference`).

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test --filter Resolve_maps_each_mode`
Expected: FAIL — `ThemeVariantMap` does not exist.

- [ ] **Step 3: Implement the mapping**

Create `src/FreelanceManager.App/Services/ThemeVariantMap.cs`:

```csharp
using Avalonia.Styling;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public static class ThemeVariantMap
{
    public static ThemeVariant Resolve(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => ThemeVariant.Light,
        ThemeMode.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test --filter Resolve_maps_each_mode`
Expected: PASS.

- [ ] **Step 5: Implement the theme service**

Create `src/FreelanceManager.App/Services/IThemeService.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public interface IThemeService
{
    void Apply(ThemeMode mode);
}
```

Create `src/FreelanceManager.App/Services/ThemeService.cs`:

```csharp
using Avalonia;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public class ThemeService : IThemeService
{
    public void Apply(ThemeMode mode)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = ThemeVariantMap.Resolve(mode);
    }
}
```

- [ ] **Step 6: Register the service**

In `src/FreelanceManager.App/ServiceConfiguration.cs`, add with the other singletons (after the `IPdfExporter` line):

```csharp
        services.AddSingleton<IThemeService, ThemeService>();
```

Add `using FreelanceManager.App.Services;` to the file's usings.

- [ ] **Step 7: Build + run suite**

Run: `dotnet build && dotnet test`
Expected: Build succeeds; all tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FreelanceManager.App/Services/ThemeVariantMap.cs src/FreelanceManager.App/Services/IThemeService.cs src/FreelanceManager.App/Services/ThemeService.cs src/FreelanceManager.App/ServiceConfiguration.cs tests/FreelanceManager.Tests/ThemeVariantMapTests.cs
git commit -m "feat(app): add theme service and variant mapping"
```

---

## Task 7: Apply persisted theme on startup

**Files:**
- Modify: `src/FreelanceManager.App/App.axaml.cs`

- [ ] **Step 1: Apply the saved theme during startup**

In `src/FreelanceManager.App/App.axaml.cs`, inside `OnFrameworkInitializationCompleted`, after `Services = ServiceConfiguration.Build();` and before constructing `MainWindow`, add:

```csharp
        try
        {
            var profiles = Services.GetRequiredService<IBusinessProfileRepository>();
            var theme = Services.GetRequiredService<IThemeService>();
            var profile = profiles.GetAsync().GetAwaiter().GetResult();
            theme.Apply(profile.Theme);
        }
        catch
        {
            // fall back to default variant if the profile can't be read
        }
```

Add usings at the top:

```csharp
using FreelanceManager.App.Services;
using FreelanceManager.Data.Repositories;
```

- [ ] **Step 2: Build + run the app**

Run: `dotnet run --project src/FreelanceManager.App`
Expected: App launches; with a fresh DB the theme is `System` (follows OS). No crash.

- [ ] **Step 3: Commit**

```bash
git add src/FreelanceManager.App/App.axaml.cs
git commit -m "feat(app): apply persisted theme on startup"
```

---

## Task 8: Theme switcher in Settings

**Files:**
- Modify: `src/FreelanceManager.App/ViewModels/SettingsViewModel.cs`
- Modify: `src/FreelanceManager.App/Views/SettingsView.axaml`
- Test: `tests/FreelanceManager.Tests/SettingsViewModelThemeTests.cs`

- [ ] **Step 1: Write a failing test for applying theme on save**

Create `tests/FreelanceManager.Tests/SettingsViewModelThemeTests.cs`:

```csharp
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
        public Task<string> BackupAsync(string source, string destDir) => Task.FromResult("x");
    }

    private sealed class FakeTheme : IThemeService
    {
        public ThemeMode? Applied { get; private set; }
        public void Apply(ThemeMode mode) => Applied = mode;
    }

    [Fact]
    public async Task Save_persists_and_applies_selected_theme()
    {
        var profiles = new FakeProfiles();
        var theme = new FakeTheme();
        var vm = new SettingsViewModel(profiles, new FakeBackup(), theme);
        await Task.Delay(20); // allow LoadAsync to complete

        vm.Theme = ThemeMode.Dark;
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(ThemeMode.Dark, profiles.Saved.Theme);
        Assert.Equal(ThemeMode.Dark, theme.Applied);
    }
}
```

Verify `IBackupService.BackupAsync`'s exact signature against `src/FreelanceManager.Core/Services/IBackupService.cs` and match the fake to it.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test --filter Save_persists_and_applies_selected_theme`
Expected: FAIL — `SettingsViewModel` has no 3-arg constructor / no `Theme` property.

- [ ] **Step 3: Update SettingsViewModel**

In `src/FreelanceManager.App/ViewModels/SettingsViewModel.cs`:

Add field and constructor param:

```csharp
    private readonly IThemeService _theme;
```

Change the constructor to:

```csharp
    public SettingsViewModel(IBusinessProfileRepository profiles, IBackupService backup, IThemeService theme)
    {
        _profiles = profiles;
        _backup = backup;
        _theme = theme;
        _ = LoadAsync();
    }
```

Add an observable property near the others:

```csharp
    [ObservableProperty] private ThemeMode _theme;
```

> Note: the property name `Theme` collides with the field `_theme`. Rename the service field to `_themeService` everywhere in this file to avoid ambiguity, and keep the observable property `[ObservableProperty] private ThemeMode _theme;` which generates the public `Theme` property.

Add `Theme = _model.Theme;` inside `LoadAsync` (with the other assignments).

In `Save`, add before `await _profiles.SaveAsync(_model);`:

```csharp
        _model.Theme = Theme;
```

And after the save, apply it:

```csharp
        _themeService.Apply(Theme);
```

Add `using FreelanceManager.App.Services;` and ensure `using FreelanceManager.Core.Models;` is present.

- [ ] **Step 4: Add a theme picker to SettingsView**

In `src/FreelanceManager.App/Views/SettingsView.axaml`, add a labeled `ComboBox` bound to `Theme`. Use an enum source. Add near the other settings fields:

```xml
    <TextBlock Classes="FieldLabel" Text="Appearance"/>
    <ComboBox SelectedItem="{Binding Theme}"
              HorizontalAlignment="Stretch">
      <ComboBox.Items>
        <x:Static xmlns:m="using:FreelanceManager.Core.Models" Member="m:ThemeMode.System"/>
        <x:Static xmlns:m="using:FreelanceManager.Core.Models" Member="m:ThemeMode.Light"/>
        <x:Static xmlns:m="using:FreelanceManager.Core.Models" Member="m:ThemeMode.Dark"/>
      </ComboBox.Items>
    </ComboBox>
```

(If the existing `SettingsView.axaml` uses a different container/layout, place these two elements alongside the other field rows following that file's pattern.)

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test --filter Save_persists_and_applies_selected_theme`
Expected: PASS.

- [ ] **Step 6: Manual check**

Run: `dotnet run --project src/FreelanceManager.App`
Go to Settings, switch Appearance to Dark, click Save → the whole app recolors to dark immediately. Switch to System → matches OS. Restart the app → the choice persisted.

- [ ] **Step 7: Run full suite + commit**

Run: `dotnet test`
Expected: All pass.

```bash
git add src/FreelanceManager.App/ViewModels/SettingsViewModel.cs src/FreelanceManager.App/Views/SettingsView.axaml tests/FreelanceManager.Tests/SettingsViewModelThemeTests.cs
git commit -m "feat(app): add System/Light/Dark theme switcher to settings"
```

---

## Task 9: Status badge control + status-to-brush converter

**Files:**
- Create: `src/FreelanceManager.App/Converters/StatusToBrushConverter.cs`
- Create: `src/FreelanceManager.App/Controls/StatusBadge.axaml` + `.axaml.cs`

- [ ] **Step 1: Implement the converter**

Create `src/FreelanceManager.App/Converters/StatusToBrushConverter.cs`. It maps an enum value (invoice or project status, by name) to a `(background, foreground)` brush pair. It takes a `ConverterParameter` of `"bg"` or `"fg"`:

```csharp
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FreelanceManager.App.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value?.ToString() ?? string.Empty;
        var wantFg = string.Equals(parameter as string, "fg", StringComparison.OrdinalIgnoreCase);

        // (subtleKey, solidKey)
        var (bgKey, fgKey) = name switch
        {
            "Paid" or "Complete" or "Active" => ("SuccessSubtle", "Success"),
            "Overdue" => ("DangerSubtle", "Danger"),
            "Sent" => ("AccentSubtle", "AccentPrimary"),
            "Lead" or "Draft" => ("InfoSubtle", "Info"),
            "Archived" => ("InfoSubtle", "Info"),
            _ => ("InfoSubtle", "Info")
        };

        var key = wantFg ? fgKey : bgKey;
        if (Application.Current!.TryFindResource(key, Application.Current.ActualThemeVariant, out var res) && res is IBrush brush)
            return brush;
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: Create the StatusBadge control**

Create `src/FreelanceManager.App/Controls/StatusBadge.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:conv="using:FreelanceManager.App.Converters"
             x:Class="FreelanceManager.App.Controls.StatusBadge"
             x:Name="Root">
  <UserControl.Resources>
    <conv:StatusToBrushConverter x:Key="StatusBrush"/>
  </UserControl.Resources>
  <Border CornerRadius="999" Padding="9,2"
          Background="{Binding Status, ElementName=Root, Converter={StaticResource StatusBrush}, ConverterParameter=bg}">
    <TextBlock Text="{Binding Status, ElementName=Root}" FontSize="11" FontWeight="SemiBold"
               Foreground="{Binding Status, ElementName=Root, Converter={StaticResource StatusBrush}, ConverterParameter=fg}"/>
  </Border>
</UserControl>
```

Create `src/FreelanceManager.App/Controls/StatusBadge.axaml.cs`:

```csharp
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
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/FreelanceManager.App/FreelanceManager.App.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Converters/StatusToBrushConverter.cs src/FreelanceManager.App/Controls/StatusBadge.axaml src/FreelanceManager.App/Controls/StatusBadge.axaml.cs
git commit -m "feat(app): add status badge control"
```

---

## Task 10: EmptyState and PageHeader controls

**Files:**
- Create: `src/FreelanceManager.App/Controls/EmptyState.axaml` + `.axaml.cs`
- Create: `src/FreelanceManager.App/Controls/PageHeader.axaml` + `.axaml.cs`

- [ ] **Step 1: Create EmptyState**

Create `src/FreelanceManager.App/Controls/EmptyState.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="FreelanceManager.App.Controls.EmptyState"
             x:Name="Root">
  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="6">
    <TextBlock Classes="SectionHeading" HorizontalAlignment="Center"
               Text="{Binding Message, ElementName=Root}"/>
    <TextBlock Classes="Caption" HorizontalAlignment="Center"
               Text="{Binding Hint, ElementName=Root}"/>
  </StackPanel>
</UserControl>
```

Create `src/FreelanceManager.App/Controls/EmptyState.axaml.cs`:

```csharp
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
```

- [ ] **Step 2: Create PageHeader**

Create `src/FreelanceManager.App/Controls/PageHeader.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="FreelanceManager.App.Controls.PageHeader"
             x:Name="Root">
  <Grid ColumnDefinitions="*,Auto" Margin="0,0,0,16">
    <TextBlock Grid.Column="0" Classes="PageTitle" VerticalAlignment="Center"
               Text="{Binding Title, ElementName=Root}"/>
    <ContentPresenter Grid.Column="1" Content="{Binding Actions, ElementName=Root}"/>
  </Grid>
</UserControl>
```

Create `src/FreelanceManager.App/Controls/PageHeader.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls;

namespace FreelanceManager.App.Controls;

public partial class PageHeader : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PageHeader, string?>(nameof(Title));
    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<PageHeader, object?>(nameof(Actions));

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    public PageHeader() => InitializeComponent();
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/FreelanceManager.App/FreelanceManager.App.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Controls/EmptyState.axaml src/FreelanceManager.App/Controls/EmptyState.axaml.cs src/FreelanceManager.App/Controls/PageHeader.axaml src/FreelanceManager.App/Controls/PageHeader.axaml.cs
git commit -m "feat(app): add empty-state and page-header controls"
```

---

## Task 11: Redesign the app shell (sidebar)

**Files:**
- Modify: `src/FreelanceManager.App/Views/MainWindow.axaml`

- [ ] **Step 1: Replace the sidebar markup**

Replace the body of `src/FreelanceManager.App/Views/MainWindow.axaml` with a tokenized sidebar that has branding, icon nav, and an active-state highlight. Active state uses Avalonia's `CommandParameter` + a bound selected key. Replace the `<Grid>...</Grid>` with:

```xml
  <Grid ColumnDefinitions="220,*" Background="{DynamicResource BgCanvas}">
    <Border Grid.Column="0" Background="{DynamicResource BgSurface}"
            BorderBrush="{DynamicResource Border}" BorderThickness="0,0,1,0">
      <StackPanel Margin="12" Spacing="2">
        <StackPanel Orientation="Horizontal" Spacing="8" Margin="6,8,6,16">
          <Border Width="20" Height="20" CornerRadius="6" Background="{DynamicResource AccentPrimary}"/>
          <TextBlock Text="Freelance Manager" Classes="SectionHeading" VerticalAlignment="Center"/>
        </StackPanel>

        <Button Classes="nav" Command="{Binding ShowDashboardCommand}" Tag="Dashboard"
                Classes.active="{Binding IsActive('Dashboard')}">
          <StackPanel Orientation="Horizontal" Spacing="10">
            <Path Width="16" Height="16" Stretch="Uniform" Fill="{DynamicResource TextMuted}"
                  Data="{DynamicResource IconDashboard}"/>
            <TextBlock Text="Dashboard" VerticalAlignment="Center"/>
          </StackPanel>
        </Button>

        <Button Classes="nav" Command="{Binding ShowClientsCommand}" Tag="Clients"
                Classes.active="{Binding IsActive('Clients')}">
          <StackPanel Orientation="Horizontal" Spacing="10">
            <Path Width="16" Height="16" Stretch="Uniform" Fill="{DynamicResource TextMuted}"
                  Data="{DynamicResource IconClients}"/>
            <TextBlock Text="Clients" VerticalAlignment="Center"/>
          </StackPanel>
        </Button>

        <Button Classes="nav" Command="{Binding ShowProjectsCommand}" Tag="Projects"
                Classes.active="{Binding IsActive('Projects')}">
          <StackPanel Orientation="Horizontal" Spacing="10">
            <Path Width="16" Height="16" Stretch="Uniform" Fill="{DynamicResource TextMuted}"
                  Data="{DynamicResource IconProjects}"/>
            <TextBlock Text="Projects" VerticalAlignment="Center"/>
          </StackPanel>
        </Button>

        <Button Classes="nav" Command="{Binding ShowInvoicesCommand}" Tag="Invoices"
                Classes.active="{Binding IsActive('Invoices')}">
          <StackPanel Orientation="Horizontal" Spacing="10">
            <Path Width="16" Height="16" Stretch="Uniform" Fill="{DynamicResource TextMuted}"
                  Data="{DynamicResource IconInvoices}"/>
            <TextBlock Text="Invoices" VerticalAlignment="Center"/>
          </StackPanel>
        </Button>

        <Button Classes="nav" Command="{Binding ShowSettingsCommand}" Tag="Settings"
                Classes.active="{Binding IsActive('Settings')}">
          <StackPanel Orientation="Horizontal" Spacing="10">
            <Path Width="16" Height="16" Stretch="Uniform" Fill="{DynamicResource TextMuted}"
                  Data="{DynamicResource IconSettings}"/>
            <TextBlock Text="Settings" VerticalAlignment="Center"/>
          </StackPanel>
        </Button>
      </StackPanel>
    </Border>

    <ContentControl Grid.Column="1" Margin="24" Content="{Binding CurrentPage}"/>
  </Grid>
```

- [ ] **Step 2: Add the nav button style**

In `src/FreelanceManager.App/Themes/Controls.axaml`, add a `nav` button style and its active variant:

```xml
  <Style Selector="Button.nav">
    <Setter Property="HorizontalAlignment" Value="Stretch"/>
    <Setter Property="HorizontalContentAlignment" Value="Left"/>
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="{DynamicResource TextMuted}"/>
    <Setter Property="Padding" Value="10,8"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSm}"/>
  </Style>
  <Style Selector="Button.nav.active">
    <Setter Property="Background" Value="{DynamicResource AccentSubtle}"/>
    <Setter Property="Foreground" Value="{DynamicResource AccentPrimary}"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
  </Style>
```

- [ ] **Step 3: Add active-tab tracking to MainWindowViewModel**

In `src/FreelanceManager.App/ViewModels/MainWindowViewModel.cs`, add an observable `ActivePage` string set by each `Show…` method, and an `IsActive` helper. Replace the command bodies:

```csharp
    [ObservableProperty] private string _activePage = "Dashboard";

    public bool IsActive(string key) => ActivePage == key;

    partial void OnActivePageChanged(string value) => OnPropertyChanged(nameof(IsActive));

    [RelayCommand] private void ShowDashboard() { CurrentPage = _services.GetRequiredService<DashboardViewModel>(); ActivePage = "Dashboard"; }
    [RelayCommand] private void ShowClients()   { CurrentPage = _services.GetRequiredService<ClientsViewModel>();   ActivePage = "Clients"; }
    [RelayCommand] private void ShowProjects()  { CurrentPage = _services.GetRequiredService<ProjectsViewModel>();  ActivePage = "Projects"; }
    [RelayCommand] private void ShowInvoices()  { CurrentPage = _services.GetRequiredService<InvoicesViewModel>();  ActivePage = "Invoices"; }
    [RelayCommand] private void ShowSettings()  { CurrentPage = _services.GetRequiredService<SettingsViewModel>();  ActivePage = "Settings"; }
```

> Note: `Classes.active="{Binding IsActive('Dashboard')}"` uses a method binding; if compiled bindings reject the method-call syntax, fall back to a `MultiBinding`-free approach: bind `Classes.active` to a per-item bool property (`IsDashboardActive`, etc.) computed from `ActivePage`, each raising change notification in `OnActivePageChanged`. Implement whichever compiles cleanly under `AvaloniaUseCompiledBindingsByDefault=true`.

- [ ] **Step 4: Build + run the app**

Run: `dotnet run --project src/FreelanceManager.App`
Expected: Sidebar shows branding + icons; the current page's nav item is highlighted; clicking moves the highlight.

- [ ] **Step 5: Commit**

```bash
git add src/FreelanceManager.App/Views/MainWindow.axaml src/FreelanceManager.App/Themes/Controls.axaml src/FreelanceManager.App/ViewModels/MainWindowViewModel.cs
git commit -m "feat(app): redesign sidebar with icons and active state"
```

---

## Task 12: Notification service

**Files:**
- Create: `src/FreelanceManager.App/Services/INotificationService.cs`
- Create: `src/FreelanceManager.App/Services/NotificationService.cs`
- Modify: `src/FreelanceManager.App/ServiceConfiguration.cs`

- [ ] **Step 1: Define the interface**

Create `src/FreelanceManager.App/Services/INotificationService.cs`:

```csharp
namespace FreelanceManager.App.Services;

public enum NotificationKind { Success, Error, Info }

public interface INotificationService
{
    void Show(string message, NotificationKind kind = NotificationKind.Info);
}
```

- [ ] **Step 2: Implement a window-backed notifier**

Create `src/FreelanceManager.App/Services/NotificationService.cs` using Avalonia's `WindowNotificationManager`:

```csharp
using Avalonia.Controls.Notifications;

namespace FreelanceManager.App.Services;

public class NotificationService : INotificationService
{
    private WindowNotificationManager? _manager;

    public void Attach(WindowNotificationManager manager) => _manager = manager;

    public void Show(string message, NotificationKind kind = NotificationKind.Info)
    {
        var type = kind switch
        {
            NotificationKind.Success => NotificationType.Success,
            NotificationKind.Error => NotificationType.Error,
            _ => NotificationType.Information
        };
        _manager?.Show(new Notification(null, message, type));
    }
}
```

- [ ] **Step 3: Register + attach**

In `src/FreelanceManager.App/ServiceConfiguration.cs`, register the concrete type behind both itself and the interface (so startup can call `Attach`):

```csharp
        services.AddSingleton<NotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
```

In `src/FreelanceManager.App/Views/MainWindow.axaml`, wrap the root `Grid` content with notification host support: add `xmlns` for notifications is not required, but add a `WindowNotificationManager` host. Simplest: in `MainWindow.axaml.cs` constructor, create and attach the manager. Modify `src/FreelanceManager.App/Views/MainWindow.axaml.cs`:

```csharp
using Avalonia.Controls.Notifications;
using FreelanceManager.App.Services;
using Microsoft.Extensions.DependencyInjection;

// inside the constructor, after InitializeComponent():
var mgr = new WindowNotificationManager(this) { Position = NotificationPosition.BottomRight, MaxItems = 3 };
App.Services.GetRequiredService<NotificationService>().Attach(mgr);
```

(Read the existing `MainWindow.axaml.cs` first and insert after the existing `InitializeComponent()` call, preserving any existing code.)

- [ ] **Step 4: Build + run**

Run: `dotnet build && dotnet run --project src/FreelanceManager.App`
Expected: Builds and launches without error (notifications appear once wired into commands in later tasks).

- [ ] **Step 5: Commit**

```bash
git add src/FreelanceManager.App/Services/INotificationService.cs src/FreelanceManager.App/Services/NotificationService.cs src/FreelanceManager.App/ServiceConfiguration.cs src/FreelanceManager.App/Views/MainWindow.axaml.cs
git commit -m "feat(app): add toast notification service"
```

---

## Task 13: Dialog service (modal forms + confirmations)

**Files:**
- Create: `src/FreelanceManager.App/Services/IDialogService.cs`
- Create: `src/FreelanceManager.App/Services/DialogService.cs`
- Create: `src/FreelanceManager.App/Views/Dialogs/ConfirmDialog.axaml` + `.axaml.cs`
- Modify: `src/FreelanceManager.App/ServiceConfiguration.cs`

- [ ] **Step 1: Define the interface**

Create `src/FreelanceManager.App/Services/IDialogService.cs`:

```csharp
using System.Threading.Tasks;

namespace FreelanceManager.App.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel");
    Task<bool> ShowDialogAsync(object viewModel);   // resolves true if the dialog saved/accepted
}
```

- [ ] **Step 2: Build the ConfirmDialog window**

Create `src/FreelanceManager.App/Views/Dialogs/ConfirmDialog.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="FreelanceManager.App.Views.Dialogs.ConfirmDialog"
        Width="380" SizeToContent="Height" CanResize="False"
        WindowStartupLocation="CenterOwner"
        Background="{DynamicResource BgCanvas}"
        Title="{Binding TitleText}">
  <StackPanel Margin="20" Spacing="16">
    <TextBlock Classes="SectionHeading" Text="{Binding TitleText}"/>
    <TextBlock Classes="Body" TextWrapping="Wrap" Text="{Binding MessageText}"/>
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
      <Button Classes="secondary" Content="{Binding CancelText}" Click="OnCancel"/>
      <Button Classes="danger" Content="{Binding ConfirmText}" Click="OnConfirm"/>
    </StackPanel>
  </StackPanel>
</Window>
```

Create `src/FreelanceManager.App/Views/Dialogs/ConfirmDialog.axaml.cs`:

```csharp
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
```

- [ ] **Step 3: Implement the dialog service**

Create `src/FreelanceManager.App/Services/DialogService.cs`. It resolves the owner window and shows dialogs modally. `ShowDialogAsync` maps a ViewModel to its dialog view via the existing `ViewLocator` naming convention (`…ViewModel` → `…View`), but for dialogs we use an explicit map to keep it simple:

```csharp
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FreelanceManager.App.ViewModels;
using FreelanceManager.App.Views.Dialogs;

namespace FreelanceManager.App.Services;

public class DialogService : IDialogService
{
    private Window? Owner =>
        (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        if (Owner is null) return false;
        var dlg = new ConfirmDialog
        {
            TitleText = title, MessageText = message,
            ConfirmText = confirmText, CancelText = cancelText
        };
        return await dlg.ShowDialog<bool>(Owner);
    }

    public async Task<bool> ShowDialogAsync(object viewModel)
    {
        if (Owner is null) return false;
        Window dlg = viewModel switch
        {
            ClientEditViewModel => new ClientEditDialog(),
            _ => throw new NotSupportedException($"No dialog for {viewModel.GetType().Name}")
        };
        dlg.DataContext = viewModel;
        return await dlg.ShowDialog<bool>(Owner);
    }
}
```

> `ClientEditDialog` is created in Task 15. Until then this file will not compile if built; sequence Task 15 before building/running this service, or stub the `ShowDialogAsync` switch with only the `ConfirmAsync` path and add the `ClientEditViewModel` case in Task 15. Recommended: implement `ConfirmAsync` fully now and add the `ShowDialogAsync` body in Task 15.

For this task, implement `ShowDialogAsync` as `throw new NotSupportedException();` and complete it in Task 15.

- [ ] **Step 4: Register the service**

In `src/FreelanceManager.App/ServiceConfiguration.cs`:

```csharp
        services.AddSingleton<IDialogService, DialogService>();
```

- [ ] **Step 5: Build to verify**

Run: `dotnet build src/FreelanceManager.App/FreelanceManager.App.csproj`
Expected: Build succeeds (with `ShowDialogAsync` throwing for now).

- [ ] **Step 6: Commit**

```bash
git add src/FreelanceManager.App/Services/IDialogService.cs src/FreelanceManager.App/Services/DialogService.cs src/FreelanceManager.App/Views/Dialogs/ConfirmDialog.axaml src/FreelanceManager.App/Views/Dialogs/ConfirmDialog.axaml.cs src/FreelanceManager.App/ServiceConfiguration.cs
git commit -m "feat(app): add dialog service with confirmation dialog"
```

---

## Task 14: Wire confirmation + notifications into Clients delete

**Files:**
- Modify: `src/FreelanceManager.App/ViewModels/ClientsViewModel.cs`
- Test: `tests/FreelanceManager.Tests/ClientsViewModelTests.cs`

- [ ] **Step 1: Write failing tests for confirm + notify behavior**

Create `tests/FreelanceManager.Tests/ClientsViewModelTests.cs`:

```csharp
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ClientsViewModelTests
{
    private sealed class FakeRepo : IClientRepository
    {
        public bool ThrowInUse;
        public bool Deleted;
        public Task<System.Collections.Generic.IReadOnlyList<Client>> GetAllAsync()
            => Task.FromResult((System.Collections.Generic.IReadOnlyList<Client>)new System.Collections.Generic.List<Client>());
        public Task<Client?> GetAsync(int id) => Task.FromResult<Client?>(new Client { Id = id });
        public Task AddAsync(Client c) => Task.CompletedTask;
        public Task UpdateAsync(Client c) => Task.CompletedTask;
        public Task DeleteAsync(int id)
        {
            if (ThrowInUse) throw new ClientInUseException("Client has projects or invoices.");
            Deleted = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDialogs : IDialogService
    {
        public bool ConfirmResult = true;
        public Task<bool> ConfirmAsync(string t, string m, string c = "Confirm", string x = "Cancel") => Task.FromResult(ConfirmResult);
        public Task<bool> ShowDialogAsync(object vm) => Task.FromResult(false);
    }

    private sealed class FakeNotes : INotificationService
    {
        public string? LastMessage; public NotificationKind LastKind;
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { LastMessage = message; LastKind = kind; }
    }

    [Fact]
    public async Task Delete_does_nothing_when_not_confirmed()
    {
        var repo = new FakeRepo();
        var dialogs = new FakeDialogs { ConfirmResult = false };
        var vm = new ClientsViewModel(repo, dialogs, new FakeNotes()) { Selected = new Client { Id = 1 } };

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.False(repo.Deleted);
    }

    [Fact]
    public async Task Delete_in_use_reports_error_notification()
    {
        var repo = new FakeRepo { ThrowInUse = true };
        var notes = new FakeNotes();
        var vm = new ClientsViewModel(repo, new FakeDialogs { ConfirmResult = true }, notes) { Selected = new Client { Id = 1 } };

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(NotificationKind.Error, notes.LastKind);
        Assert.Contains("projects or invoices", notes.LastMessage);
    }
}
```

Confirm `IClientRepository`'s exact member signatures against `src/FreelanceManager.Data/Repositories/IClientRepository.cs` and adjust `FakeRepo` to match exactly.

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter ClientsViewModelTests`
Expected: FAIL — `ClientsViewModel` has no 3-arg constructor.

- [ ] **Step 3: Update ClientsViewModel**

In `src/FreelanceManager.App/ViewModels/ClientsViewModel.cs`:

- Add fields and constructor params for `IDialogService` and `INotificationService`:

```csharp
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notes;

    public ClientsViewModel(IClientRepository repo, IDialogService dialogs, INotificationService notes)
    {
        _repo = repo;
        _dialogs = dialogs;
        _notes = notes;
        _ = LoadAsync();
    }
```

- Replace the `Delete` command body:

```csharp
    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        if (!await _dialogs.ConfirmAsync("Delete client",
                $"Delete \"{Selected.Name}\"? This cannot be undone.", "Delete"))
            return;
        try
        {
            await _repo.DeleteAsync(Selected.Id);
            await LoadAsync();
            _notes.Show("Client deleted.", NotificationKind.Success);
        }
        catch (ClientInUseException ex)
        {
            _notes.Show(ex.Message, NotificationKind.Error);
        }
    }
```

- Replace `StatusMessage` usages in `Save`/`LoadAsync` with `_notes.Show(...)` calls (success/error) and remove the `[ObservableProperty] private string? _statusMessage;` field.

Add `using FreelanceManager.App.Services;`.

- [ ] **Step 4: Update DI registration**

The DI container already constructs `ClientsViewModel` via `AddTransient`; since `IDialogService` and `INotificationService` are registered, no change is needed. Verify by building.

- [ ] **Step 5: Run tests to verify pass**

Run: `dotnet test --filter ClientsViewModelTests`
Expected: PASS (both tests).

- [ ] **Step 6: Full suite + commit**

Run: `dotnet test`
Expected: All pass (note: this removes `StatusMessage`; Task 16 retrofits `ClientsView.axaml` to drop the bound red text — until then the view may reference a missing binding, which Avalonia tolerates at runtime but fix it in Task 16).

```bash
git add src/FreelanceManager.App/ViewModels/ClientsViewModel.cs tests/FreelanceManager.Tests/ClientsViewModelTests.cs
git commit -m "feat(app): confirm + notify on client delete"
```

---

## Task 15: Modal client editor

**Files:**
- Create: `src/FreelanceManager.App/Views/Dialogs/ClientEditDialog.axaml` + `.axaml.cs`
- Modify: `src/FreelanceManager.App/Services/DialogService.cs`
- Modify: `src/FreelanceManager.App/ViewModels/ClientsViewModel.cs`
- Modify: `src/FreelanceManager.App/ViewModels/ClientEditViewModel.cs`

- [ ] **Step 1: Read the current ClientEditViewModel**

Read `src/FreelanceManager.App/ViewModels/ClientEditViewModel.cs` to confirm its properties (`Name`, `Company`, `Email`, `Phone`, `Address`, `Notes`, `Id`, `IsValid`, `ApplyTo`). The dialog binds to these.

- [ ] **Step 2: Create the modal dialog**

Create `src/FreelanceManager.App/Views/Dialogs/ClientEditDialog.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:FreelanceManager.App.ViewModels"
        x:Class="FreelanceManager.App.Views.Dialogs.ClientEditDialog"
        x:DataType="vm:ClientEditViewModel"
        Width="380" SizeToContent="Height" CanResize="False"
        WindowStartupLocation="CenterOwner"
        Background="{DynamicResource BgCanvas}"
        Title="Client">
  <StackPanel Margin="20" Spacing="10">
    <TextBlock Classes="SectionHeading" Text="Client"/>
    <StackPanel Spacing="4">
      <TextBlock Classes="FieldLabel" Text="Name *"/>
      <TextBox Text="{Binding Name}"/>
    </StackPanel>
    <StackPanel Spacing="4">
      <TextBlock Classes="FieldLabel" Text="Company"/>
      <TextBox Text="{Binding Company}"/>
    </StackPanel>
    <StackPanel Spacing="4">
      <TextBlock Classes="FieldLabel" Text="Email"/>
      <TextBox Text="{Binding Email}"/>
    </StackPanel>
    <StackPanel Spacing="4">
      <TextBlock Classes="FieldLabel" Text="Phone"/>
      <TextBox Text="{Binding Phone}"/>
    </StackPanel>
    <StackPanel Spacing="4">
      <TextBlock Classes="FieldLabel" Text="Address"/>
      <TextBox Text="{Binding Address}" AcceptsReturn="True" Height="60"/>
    </StackPanel>
    <StackPanel Spacing="4">
      <TextBlock Classes="FieldLabel" Text="Notes"/>
      <TextBox Text="{Binding Notes}" AcceptsReturn="True" Height="60"/>
    </StackPanel>
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
      <Button Classes="secondary" Content="Cancel" Click="OnCancel"/>
      <Button Classes="primary" Content="Save" Click="OnSave"/>
    </StackPanel>
  </StackPanel>
</Window>
```

Create `src/FreelanceManager.App/Views/Dialogs/ClientEditDialog.axaml.cs`:

```csharp
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
```

- [ ] **Step 3: Complete DialogService.ShowDialogAsync**

In `src/FreelanceManager.App/Services/DialogService.cs`, replace the stubbed `ShowDialogAsync` with the full body shown in Task 13 Step 3 (the `switch` mapping `ClientEditViewModel => new ClientEditDialog()`).

- [ ] **Step 4: Use the dialog from ClientsViewModel**

In `src/FreelanceManager.App/ViewModels/ClientsViewModel.cs`, change `New` and `Edit` to open the modal and persist on accept:

```csharp
    [RelayCommand]
    private async Task New()
    {
        var editor = new ClientEditViewModel(new Client());
        if (await _dialogs.ShowDialogAsync(editor))
        {
            var model = new Client();
            editor.ApplyTo(model);
            await _repo.AddAsync(model);
            _notes.Show("Client added.", NotificationKind.Success);
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task Edit()
    {
        if (Selected is null) return;
        var editor = new ClientEditViewModel(Selected);
        if (await _dialogs.ShowDialogAsync(editor))
        {
            var model = await _repo.GetAsync(editor.Id);
            if (model is not null) { editor.ApplyTo(model); await _repo.UpdateAsync(model); }
            _notes.Show("Client saved.", NotificationKind.Success);
            await LoadAsync();
        }
    }
```

Remove the now-unused `Save`/`Cancel` commands and the `Editor` observable property from `ClientsViewModel` (they are replaced by the modal flow).

- [ ] **Step 5: Build + run**

Run: `dotnet build && dotnet run --project src/FreelanceManager.App`
Expected: On Clients, "New"/"Edit" opens a centered modal; Save persists and a success toast appears; "Delete" asks to confirm.

- [ ] **Step 6: Run suite + commit**

Run: `dotnet test`
Expected: All pass. (The `ClientsViewModelTests` still construct the VM the same way; only `New`/`Edit` changed.)

```bash
git add src/FreelanceManager.App/Views/Dialogs/ClientEditDialog.axaml src/FreelanceManager.App/Views/Dialogs/ClientEditDialog.axaml.cs src/FreelanceManager.App/Services/DialogService.cs src/FreelanceManager.App/ViewModels/ClientsViewModel.cs
git commit -m "feat(app): modal client editor"
```

---

## Task 16: Retrofit ClientsView onto the design system

**Files:**
- Modify: `src/FreelanceManager.App/Views/ClientsView.axaml`

- [ ] **Step 1: Replace ClientsView markup**

Rewrite `src/FreelanceManager.App/Views/ClientsView.axaml` to use `PageHeader`, styled buttons, an `EmptyState` shown when the list is empty, and `StatusBadge` is not needed here. Remove the docked editor panel and bottom red-text line (editing is now modal):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             xmlns:c="using:FreelanceManager.App.Controls"
             x:Class="FreelanceManager.App.Views.ClientsView"
             x:DataType="vm:ClientsViewModel">
  <Grid RowDefinitions="Auto,*">
    <c:PageHeader Grid.Row="0" Title="Clients">
      <c:PageHeader.Actions>
        <StackPanel Orientation="Horizontal" Spacing="8">
          <Button Classes="primary" Content="New" Command="{Binding NewCommand}"/>
          <Button Classes="secondary" Content="Edit" Command="{Binding EditCommand}"/>
          <Button Classes="danger" Content="Delete" Command="{Binding DeleteCommand}"/>
        </StackPanel>
      </c:PageHeader.Actions>
    </c:PageHeader>

    <Panel Grid.Row="1">
      <DataGrid ItemsSource="{Binding Clients}" SelectedItem="{Binding Selected}"
                IsReadOnly="True" AutoGenerateColumns="False">
        <DataGrid.Columns>
          <DataGridTextColumn Header="Name" Binding="{Binding Name}"/>
          <DataGridTextColumn Header="Company" Binding="{Binding Company}"/>
          <DataGridTextColumn Header="Email" Binding="{Binding Email}"/>
        </DataGrid.Columns>
      </DataGrid>
      <c:EmptyState Message="No clients yet" Hint="Click “New” to add your first client."
                    IsVisible="{Binding !Clients.Count}"/>
    </Panel>
  </Grid>
</UserControl>
```

> `IsVisible="{Binding !Clients.Count}"` relies on Avalonia's bool conversion of an int (0 → false, so `!0` → true). If that conversion is rejected under compiled bindings, add an `int`-to-bool `IsZero` converter in `Converters/` and use it; implement whichever compiles.

- [ ] **Step 2: Run the app**

Run: `dotnet run --project src/FreelanceManager.App`
Expected: Clients page uses the page header + styled buttons; empty DB shows the empty state; adding a client hides it.

- [ ] **Step 3: Commit**

```bash
git add src/FreelanceManager.App/Views/ClientsView.axaml
git commit -m "refactor(app): retrofit Clients view onto design system"
```

---

## Task 17: Full-page Project editor

**Files:**
- Modify: `src/FreelanceManager.App/ViewModels/ProjectsViewModel.cs`
- Create: `src/FreelanceManager.App/Views/ProjectEditView.axaml` + `.axaml.cs`
- Modify: `src/FreelanceManager.App/Views/ProjectsView.axaml`

- [ ] **Step 1: Read current Projects VMs**

Read `src/FreelanceManager.App/ViewModels/ProjectsViewModel.cs` and `ProjectEditViewModel.cs` to confirm property names and the existing `Editor`, `ClientOptions`, `EditorClient`, `Save`, `Cancel`, `New`, `Edit` members.

- [ ] **Step 2: Add navigation between list and editor**

In `ProjectsViewModel`, keep the existing `Editor` property but expose a bool `IsEditing => Editor is not null` for the host to switch content. Add `partial void OnEditorChanged(ProjectEditViewModel? value) => OnPropertyChanged(nameof(IsEditing));`. Ensure `Save`/`Cancel` set `Editor = null` (returning to the list) and that `Save` raises a success notification via an injected `INotificationService` (add it to the constructor like Clients in Task 14; update the `Fake... ` pattern is not needed unless a Projects VM test exists — none does, so just wire the constructor and DI resolves it).

- [ ] **Step 3: Create the full-page editor view**

Create `src/FreelanceManager.App/Views/ProjectEditView.axaml` — a roomy two-column form. Bind to the existing `ProjectEditViewModel` shape. Move the field markup currently inside the docked panel of `ProjectsView.axaml` here, restructured into two columns with `FieldLabel`s:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             xmlns:c="using:FreelanceManager.App.Controls"
             x:Class="FreelanceManager.App.Views.ProjectEditView"
             x:DataType="vm:ProjectsViewModel">
  <ScrollViewer>
    <StackPanel Spacing="16" MaxWidth="760" HorizontalAlignment="Left">
      <c:PageHeader Title="Edit project">
        <c:PageHeader.Actions>
          <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Classes="primary" Content="Save project" Command="{Binding SaveCommand}"/>
            <Button Classes="secondary" Content="Cancel" Command="{Binding CancelCommand}"/>
          </StackPanel>
        </c:PageHeader.Actions>
      </c:PageHeader>

      <Border Classes="card">
        <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto,Auto,Auto" >
          <StackPanel Grid.Row="0" Grid.Column="0" Margin="0,0,12,12" Spacing="4">
            <TextBlock Classes="FieldLabel" Text="Client *"/>
            <ComboBox HorizontalAlignment="Stretch" ItemsSource="{Binding ClientOptions}" SelectedItem="{Binding EditorClient}">
              <ComboBox.ItemTemplate><DataTemplate><TextBlock Text="{Binding Name}"/></DataTemplate></ComboBox.ItemTemplate>
            </ComboBox>
          </StackPanel>
          <StackPanel Grid.Row="0" Grid.Column="1" Margin="0,0,0,12" Spacing="4" DataContext="{Binding Editor}">
            <TextBlock Classes="FieldLabel" Text="Title *"/>
            <TextBox Text="{Binding Title}"/>
          </StackPanel>

          <StackPanel Grid.Row="1" Grid.ColumnSpan="2" Spacing="4" DataContext="{Binding Editor}">
            <TextBlock Classes="FieldLabel" Text="Status"/>
            <ComboBox HorizontalAlignment="Stretch" ItemsSource="{Binding StatusOptions}" SelectedItem="{Binding Status}"/>
            <TextBlock Classes="FieldLabel" Text="Repo URL" Margin="0,8,0,0"/>
            <TextBox Text="{Binding RepoUrl}"/>
            <TextBlock Classes="FieldLabel" Text="Live site URL" Margin="0,8,0,0"/>
            <TextBox Text="{Binding LiveSiteUrl}"/>
            <TextBlock Classes="FieldLabel" Text="Build stack notes" Margin="0,8,0,0"/>
            <TextBox Text="{Binding BuildStackNotes}" AcceptsReturn="True" Height="50"/>
            <TextBlock Classes="FieldLabel" Text="Hosting notes" Margin="0,8,0,0"/>
            <TextBox Text="{Binding HostingNotes}" AcceptsReturn="True" Height="50"/>
            <TextBlock Classes="FieldLabel" Text="Where credentials live" Margin="0,8,0,0"/>
            <TextBox Text="{Binding CredentialsLocation}"/>
            <TextBlock Classes="FieldLabel" Text="General notes / future instructions" Margin="0,8,0,0"/>
            <TextBox Text="{Binding GeneralNotes}" AcceptsReturn="True" Height="60"/>
            <Grid ColumnDefinitions="*,*" Margin="0,8,0,0">
              <StackPanel Grid.Column="0" Margin="0,0,12,0" Spacing="4">
                <TextBlock Classes="FieldLabel" Text="Start date"/>
                <DatePicker SelectedDate="{Binding StartDate}"/>
              </StackPanel>
              <StackPanel Grid.Column="1" Spacing="4">
                <TextBlock Classes="FieldLabel" Text="Due date"/>
                <DatePicker SelectedDate="{Binding DueDate}"/>
              </StackPanel>
            </Grid>
            <Button Classes="secondary" Content="Generate summary (coming soon)" IsEnabled="False" Margin="0,12,0,0"/>
          </StackPanel>
        </Grid>
      </Border>
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

Create `src/FreelanceManager.App/Views/ProjectEditView.axaml.cs`:

```csharp
using Avalonia.Controls;

namespace FreelanceManager.App.Views;

public partial class ProjectEditView : UserControl
{
    public ProjectEditView() => InitializeComponent();
}
```

- [ ] **Step 4: Switch the Projects page between list and editor**

Rewrite `src/FreelanceManager.App/Views/ProjectsView.axaml` so the content swaps on `IsEditing`: list (with `PageHeader` + styled buttons + `StatusBadge` column + `EmptyState`) when not editing, the `ProjectEditView` when editing:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             xmlns:c="using:FreelanceManager.App.Controls"
             xmlns:views="using:FreelanceManager.App.Views"
             x:Class="FreelanceManager.App.Views.ProjectsView"
             x:DataType="vm:ProjectsViewModel">
  <Panel>
    <!-- List -->
    <Grid RowDefinitions="Auto,*" IsVisible="{Binding !IsEditing}">
      <c:PageHeader Grid.Row="0" Title="Projects">
        <c:PageHeader.Actions>
          <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Classes="primary" Content="New" Command="{Binding NewCommand}"/>
            <Button Classes="secondary" Content="Edit" Command="{Binding EditCommand}"/>
            <Button Classes="danger" Content="Delete" Command="{Binding DeleteCommand}"/>
          </StackPanel>
        </c:PageHeader.Actions>
      </c:PageHeader>
      <Panel Grid.Row="1">
        <DataGrid ItemsSource="{Binding Projects}" SelectedItem="{Binding Selected}"
                  IsReadOnly="True" AutoGenerateColumns="False">
          <DataGrid.Columns>
            <DataGridTextColumn Header="Title" Binding="{Binding Title}"/>
            <DataGridTextColumn Header="Client" Binding="{Binding Client.Name}"/>
            <DataGridTemplateColumn Header="Status">
              <DataGridTemplateColumn.CellTemplate>
                <DataTemplate><c:StatusBadge Status="{Binding Status}" HorizontalAlignment="Left"/></DataTemplate>
              </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>
            <DataGridTextColumn Header="Due" Binding="{Binding DueDate, StringFormat={}{0:yyyy-MM-dd}}"/>
          </DataGrid.Columns>
        </DataGrid>
        <c:EmptyState Message="No projects yet" Hint="Click “New” to create one."
                      IsVisible="{Binding !Projects.Count}"/>
      </Panel>
    </Grid>

    <!-- Editor -->
    <views:ProjectEditView IsVisible="{Binding IsEditing}"/>
  </Panel>
</UserControl>
```

- [ ] **Step 5: Build + run**

Run: `dotnet build && dotnet run --project src/FreelanceManager.App`
Expected: Projects list shows status badges; "New"/"Edit" replaces the whole page with the roomy editor; Save/Cancel returns to the list.

- [ ] **Step 6: Run suite + commit**

Run: `dotnet test`
Expected: All pass.

```bash
git add src/FreelanceManager.App/ViewModels/ProjectsViewModel.cs src/FreelanceManager.App/Views/ProjectEditView.axaml src/FreelanceManager.App/Views/ProjectEditView.axaml.cs src/FreelanceManager.App/Views/ProjectsView.axaml
git commit -m "feat(app): full-page project editor with status badges"
```

---

## Task 18: Full-page Invoice editor

**Files:**
- Modify: `src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs`
- Create: `src/FreelanceManager.App/Views/InvoiceEditView.axaml` + `.axaml.cs`
- Modify: `src/FreelanceManager.App/Views/InvoicesView.axaml`

- [ ] **Step 1: Read current Invoices VMs**

Read `src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs` and `InvoiceEditViewModel.cs` to confirm members (`Editor`, `ClientOptions`, `EditorClient`, `Lines`, `AddLineCommand`, `Subtotal`, `Tax`, `Total`, `Save`, `Cancel`, statuses).

- [ ] **Step 2: Add IsEditing to InvoicesViewModel**

Mirror Task 17 Step 2: add `public bool IsEditing => Editor is not null;` and `partial void OnEditorChanged(InvoiceEditViewModel? value) => OnPropertyChanged(nameof(IsEditing));`. Inject `INotificationService` and raise a success toast on save and on PDF export (replacing the `StatusMessage` red text).

- [ ] **Step 3: Create the full-page invoice editor**

Create `src/FreelanceManager.App/Views/InvoiceEditView.axaml`. Concretely: copy the existing editor block from `InvoicesView.axaml` — the `<ScrollViewer Grid.Column="1"> … </ScrollViewer>` containing the Client `ComboBox`, the `DataContext="{Binding Editor}"` inner panel (Number, Status, IssueDate/DueDate `DatePicker`s, Tax rate, the line-items `DataGrid`, "Add line" button, Subtotal/Tax/Total `TextBlock`s, Notes) — into this new view. Then restyle it: wrap the form in `MaxWidth="760"`, put each input under a `<TextBlock Classes="FieldLabel"/>` label instead of watermark-only, wrap the line-items + totals in a `Border.card`, and add a `PageHeader Title="Edit invoice"` whose Actions are `primary` Save and `secondary` Cancel bound to `SaveCommand`/`CancelCommand`. Keep the exact binding paths verified in Step 1 (`Editor.*`, `EditorClient`, `ClientOptions`, `Lines`, `AddLineCommand`, `Subtotal`, `Tax`, `Total`).

Create `src/FreelanceManager.App/Views/InvoiceEditView.axaml.cs`:

```csharp
using Avalonia.Controls;

namespace FreelanceManager.App.Views;

public partial class InvoiceEditView : UserControl
{
    public InvoiceEditView() => InitializeComponent();
}
```

- [ ] **Step 4: Switch the Invoices page between list and editor**

Rewrite `src/FreelanceManager.App/Views/InvoicesView.axaml` like Task 17 Step 4: a list grid (PageHeader + New/Edit/Delete/Export buttons + `StatusBadge` column + `EmptyState`) shown when `!IsEditing`, and `<views:InvoiceEditView IsVisible="{Binding IsEditing}"/>` when editing. The Status column becomes:

```xml
            <DataGridTemplateColumn Header="Status">
              <DataGridTemplateColumn.CellTemplate>
                <DataTemplate><c:StatusBadge Status="{Binding Status}" HorizontalAlignment="Left"/></DataTemplate>
              </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>
```

Keep the existing `Number`, `Client`, `Issued`, `Due`, `Total` columns.

- [ ] **Step 5: Build + run**

Run: `dotnet build && dotnet run --project src/FreelanceManager.App`
Expected: Invoices list shows status badges; New/Edit opens the full-page editor with live subtotal/tax/total; Export shows a success toast; Save/Cancel returns to the list.

- [ ] **Step 6: Run suite + commit**

Run: `dotnet test`
Expected: All pass.

```bash
git add src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs src/FreelanceManager.App/Views/InvoiceEditView.axaml src/FreelanceManager.App/Views/InvoiceEditView.axaml.cs src/FreelanceManager.App/Views/InvoicesView.axaml
git commit -m "feat(app): full-page invoice editor with status badges"
```

---

## Task 19: Retrofit Dashboard onto tokens

**Files:**
- Modify: `src/FreelanceManager.App/Views/DashboardView.axaml`

- [ ] **Step 1: Replace hardcoded hex with tokens**

Rewrite `src/FreelanceManager.App/Views/DashboardView.axaml` so the three stat cards use `Border.card` and token brushes instead of `#eef`/`#efe`/`#fee`, and the title uses `PageHeader`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             xmlns:c="using:FreelanceManager.App.Controls"
             x:Class="FreelanceManager.App.Views.DashboardView"
             x:DataType="vm:DashboardViewModel">
  <StackPanel Spacing="16">
    <c:PageHeader Title="Dashboard"/>
    <WrapPanel>
      <Border Classes="card" Margin="0,0,12,12" MinWidth="180">
        <StackPanel Spacing="4">
          <TextBlock Classes="Caption" Text="Active projects"/>
          <TextBlock Text="{Binding ActiveProjects}" FontSize="28" FontWeight="Bold"
                     Foreground="{DynamicResource AccentPrimary}"/>
        </StackPanel>
      </Border>
      <Border Classes="card" Margin="0,0,12,12" MinWidth="180">
        <StackPanel Spacing="4">
          <TextBlock Classes="Caption" Text="Outstanding"/>
          <TextBlock Text="{Binding OutstandingTotal, StringFormat={}{0:0.00}}" FontSize="28" FontWeight="Bold"
                     Foreground="{DynamicResource TextPrimary}"/>
        </StackPanel>
      </Border>
      <Border Classes="card" Margin="0,0,12,12" MinWidth="180">
        <StackPanel Spacing="4">
          <TextBlock Classes="Caption" Text="Overdue"/>
          <TextBlock Text="{Binding OverdueCount}" FontSize="28" FontWeight="Bold"
                     Foreground="{DynamicResource Danger}"/>
        </StackPanel>
      </Border>
    </WrapPanel>
  </StackPanel>
</UserControl>
```

- [ ] **Step 2: Run the app in both themes**

Run: `dotnet run --project src/FreelanceManager.App`
Expected: Dashboard cards look correct in light; switch to Dark in Settings → cards recolor cleanly (no white-on-white or broken pastels).

- [ ] **Step 3: Commit**

```bash
git add src/FreelanceManager.App/Views/DashboardView.axaml
git commit -m "refactor(app): tokenize dashboard, fix dark mode"
```

---

## Task 20: Retrofit SettingsView + final hex sweep

**Files:**
- Modify: `src/FreelanceManager.App/Views/SettingsView.axaml`
- Audit: all `src/FreelanceManager.App/Views/*.axaml`

- [ ] **Step 1: Tokenize SettingsView**

Update `src/FreelanceManager.App/Views/SettingsView.axaml` to use `PageHeader`, `FieldLabel`s above each input, `Border.card` grouping, styled `primary`/`secondary` buttons, and the theme picker from Task 8. Remove any hardcoded `Background`/`Foreground` hex and the bottom red `StatusMessage` text (replace Save/Backup feedback with `INotificationService` toasts — inject it into `SettingsViewModel` and call `_notes.Show(...)` where it currently sets `StatusMessage`).

> **Constructor change:** adding `INotificationService` makes `SettingsViewModel`'s constructor 4-arg: `(IBusinessProfileRepository profiles, IBackupService backup, IThemeService theme, INotificationService notes)`. This breaks the test from Task 8. Update `tests/FreelanceManager.Tests/SettingsViewModelThemeTests.cs`: add a `FakeNotes : INotificationService` (copy the one from `ClientsViewModelTests`) and pass it as the 4th constructor arg in `Save_persists_and_applies_selected_theme`. Re-run that test to confirm it still passes after the change.

- [ ] **Step 2: Sweep for remaining hardcoded colors**

Run a search for leftover hex literals in views:

Run: `git grep -nE "#[0-9a-fA-F]{3,8}" -- "src/FreelanceManager.App/Views/*.axaml"`
Expected: No matches (every color now comes from a token). Fix any stragglers by replacing with the appropriate `{DynamicResource …}` token.

- [ ] **Step 3: Confirm StatusMessage is fully removed**

Run: `git grep -n "StatusMessage" -- src/FreelanceManager.App`
Expected: No matches in ViewModels or Views (all replaced by notifications). Remove any remnants.

- [ ] **Step 4: Run the app, full light/dark pass**

Run: `dotnet run --project src/FreelanceManager.App`
Manually verify every page (Dashboard, Clients, Projects, Invoices, Settings) in both Light and Dark: text is legible, surfaces/borders use tokens, badges colored correctly, modals and the full-page editors render correctly, toasts appear bottom-right.

- [ ] **Step 5: Run full suite + commit**

Run: `dotnet test`
Expected: All pass.

```bash
git add src/FreelanceManager.App/Views/SettingsView.axaml src/FreelanceManager.App/ViewModels/SettingsViewModel.cs
git commit -m "refactor(app): tokenize settings and complete hex sweep"
```

---

## Task 21: Final verification

**Files:** none (verification only)

- [ ] **Step 1: Clean build**

Run: `dotnet build`
Expected: 0 errors, 0 warnings related to this work.

- [ ] **Step 2: Full test suite**

Run: `dotnet test`
Expected: All tests pass (existing + the new theme, settings, and clients tests).

- [ ] **Step 3: Smoke test the running app**

Run: `dotnet run --project src/FreelanceManager.App`
Walk through: create a client (modal), create a project (full-page editor) and confirm its status badge, create an invoice and export the PDF (toast), delete a client in use (error toast) and an unused one (confirm → success toast), toggle System/Light/Dark and restart to confirm persistence.

- [ ] **Step 4: Final commit (if any cleanup remains)**

```bash
git add -A
git commit -m "chore(app): design-system foundation polish"
```
