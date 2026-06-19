# UI/UX Refresh Implementation Plan

> Steps use checkbox (`- [ ]`) syntax for task-by-task tracking. Implement in order; each task ends with a build/test gate and a commit.

**Goal:** Refresh the Avalonia app from generic SaaS-blue to a calm, professional, sellable product: re-tuned tokens, an agenda home screen, consistent master-detail editing, and first-impression polish.

**Architecture:** Pure UI-layer work in `FreelanceManager.App` plus two small pure-logic helpers in `FreelanceManager.Core` (agenda + onboarding-state) so the new logic is unit-testable without the UI. MVVM, DI, and the light/dark token system are preserved. No EF migration, no data-model change (open questions resolved: pinned projects are *derived*; onboarding "dismissed" flag lives in a small JSON app-state file).

**Tech Stack:** .NET 10, Avalonia UI 12, CommunityToolkit.Mvvm, EF Core 10 (untouched here), xUnit.

## Global Constraints

- **dotnet invocation:** the PATH `dotnet` is a broken x86 stub. Use `"C:\Program Files\dotnet\dotnet.exe"` for every build/test/run command.
- **Accent color:** indigo. Light `#5E6AD2`, hover `#4F5BC4`, subtle `#ECECF7`. Dark `#7C8AE8`, hover `#94A0EE`, subtle `#262B45`. Replaces `#4F6BED`.
- **Both theme variants** (Light and Dark dictionaries) must be updated together in any token change.
- **No hardcoded colors** in views — always `{DynamicResource ...}`.
- **Status semantics unchanged:** grey=Draft/Lead, amber=Sent, green=Paid/Active/Complete, red=Overdue, neutral=Archived.
- **Scope:** existing features only. Deferred features (email, payments, handover) untouched.
- Run all tests with: `"C:\Program Files\dotnet\dotnet.exe" test`
- Commit after each task. No `Co-Authored-By`/AI-attribution trailers.

---

## Phase 1 — Visual system

### Task 1: Re-tune design tokens

**Files:**
- Modify: `src/FreelanceManager.App/Themes/Tokens.axaml`

**Interfaces:**
- Produces: token keys (unchanged names) `AccentPrimary`, `AccentPrimaryHover`, `AccentSubtle`, `BgCanvas`, `BgSurface`, `BgSurfaceAlt`, `Border`, `TextPrimary`, `TextMuted`, status brushes. Every later task binds these via `DynamicResource`.

This is a pure-XAML task (no unit test); verify by build + visual run.

- [ ] **Step 1: Replace the Light dictionary brush values** in `Tokens.axaml` with the graphite-warmed palette:

```xml
<ResourceDictionary x:Key="Light">
  <SolidColorBrush x:Key="BgCanvas"      Color="#FFFFFF"/>
  <SolidColorBrush x:Key="BgSurface"     Color="#FAFAF9"/>
  <SolidColorBrush x:Key="BgSurfaceAlt"  Color="#F2F1EE"/>
  <SolidColorBrush x:Key="Border"        Color="#E7E5E1"/>
  <SolidColorBrush x:Key="TextPrimary"   Color="#20222A"/>
  <SolidColorBrush x:Key="TextMuted"     Color="#80838C"/>
  <SolidColorBrush x:Key="AccentPrimary" Color="#5E6AD2"/>
  <SolidColorBrush x:Key="AccentPrimaryHover" Color="#4F5BC4"/>
  <SolidColorBrush x:Key="AccentSubtle"  Color="#ECECF7"/>
  <SolidColorBrush x:Key="Success"       Color="#2E9E5B"/>
  <SolidColorBrush x:Key="SuccessSubtle" Color="#E4F3EA"/>
  <SolidColorBrush x:Key="Warning"       Color="#B07D14"/>
  <SolidColorBrush x:Key="WarningSubtle" Color="#FBF3DE"/>
  <SolidColorBrush x:Key="Danger"        Color="#C0392B"/>
  <SolidColorBrush x:Key="DangerSubtle"  Color="#FBE9E6"/>
  <SolidColorBrush x:Key="Info"          Color="#80838C"/>
  <SolidColorBrush x:Key="InfoSubtle"    Color="#EFEEEC"/>
</ResourceDictionary>
```

- [ ] **Step 2: Replace the Dark dictionary brush values:**

```xml
<ResourceDictionary x:Key="Dark">
  <SolidColorBrush x:Key="BgCanvas"      Color="#15171C"/>
  <SolidColorBrush x:Key="BgSurface"     Color="#1C1F26"/>
  <SolidColorBrush x:Key="BgSurfaceAlt"  Color="#232730"/>
  <SolidColorBrush x:Key="Border"        Color="#2C313B"/>
  <SolidColorBrush x:Key="TextPrimary"   Color="#E7E9EE"/>
  <SolidColorBrush x:Key="TextMuted"     Color="#969BA6"/>
  <SolidColorBrush x:Key="AccentPrimary" Color="#7C8AE8"/>
  <SolidColorBrush x:Key="AccentPrimaryHover" Color="#94A0EE"/>
  <SolidColorBrush x:Key="AccentSubtle"  Color="#262B45"/>
  <SolidColorBrush x:Key="Success"       Color="#34D399"/>
  <SolidColorBrush x:Key="SuccessSubtle" Color="#11362A"/>
  <SolidColorBrush x:Key="Warning"       Color="#E0B341"/>
  <SolidColorBrush x:Key="WarningSubtle" Color="#332B12"/>
  <SolidColorBrush x:Key="Danger"        Color="#F87171"/>
  <SolidColorBrush x:Key="DangerSubtle"  Color="#3A1E1E"/>
  <SolidColorBrush x:Key="Info"          Color="#969BA6"/>
  <SolidColorBrush x:Key="InfoSubtle"    Color="#222630"/>
</ResourceDictionary>
```

- [ ] **Step 3: Make `RadiusMd` the calmer default** — change `RadiusMd` from `9` to `8` and leave `RadiusSm` at `6`, `RadiusLg` at `12`.

- [ ] **Step 4: Build and run, verify both themes**

Run: `"C:\Program Files\dotnet\dotnet.exe" build`
Then: `"C:\Program Files\dotnet\dotnet.exe" run --project src/FreelanceManager.App`
Expected: app launches; accent is indigo; surfaces read warm-neutral; toggle theme in Settings → dark variant is graphite, no leftover blue.

- [ ] **Step 5: Commit**

```bash
git add src/FreelanceManager.App/Themes/Tokens.axaml
git commit -m "style: re-tune design tokens to graphite + indigo"
```

---

### Task 2: Status pills → tint + dot

**Files:**
- Modify: `src/FreelanceManager.App/Controls/StatusBadge.axaml`

**Interfaces:**
- Consumes: `StatusToBrushConverter` (unchanged) with `ConverterParameter=bg` / `fg`, bound off `Status` (string).
- Produces: `StatusBadge` control with a `Status` property — used by Projects/Invoices grids unchanged.

Pure-XAML; verify by build + visual run.

- [ ] **Step 1: Replace the `StatusBadge.axaml` `<Border>` body** so the pill is a soft tint with a leading colored dot:

```xml
<Border CornerRadius="999" Padding="8,3"
        Background="{Binding Status, ElementName=Root, Converter={StaticResource StatusBrush}, ConverterParameter=bg}">
  <StackPanel Orientation="Horizontal" Spacing="6" VerticalAlignment="Center">
    <Ellipse Width="6" Height="6" VerticalAlignment="Center"
             Fill="{Binding Status, ElementName=Root, Converter={StaticResource StatusBrush}, ConverterParameter=fg}"/>
    <TextBlock Text="{Binding Status, ElementName=Root}" FontSize="11" FontWeight="SemiBold"
               VerticalAlignment="Center"
               Foreground="{Binding Status, ElementName=Root, Converter={StaticResource StatusBrush}, ConverterParameter=fg}"/>
  </StackPanel>
</Border>
```

- [ ] **Step 2: Update the `Sent` mapping** so amber (not accent) is used for Sent. In `src/FreelanceManager.App/Converters/StatusToBrushConverter.cs`, change the `"Sent"` arm:

```csharp
"Sent" => ("WarningSubtle", "Warning"),
```

- [ ] **Step 3: Build and run, verify pills**

Run: `"C:\Program Files\dotnet\dotnet.exe" build`
Then launch the app, open Invoices/Projects.
Expected: pills are soft-tinted with a colored dot; Sent=amber, Paid=green, Overdue=red, Draft/Lead=grey; readable in both themes.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Controls/StatusBadge.axaml src/FreelanceManager.App/Converters/StatusToBrushConverter.cs
git commit -m "style: calmer tint+dot status pills"
```

---

### Task 3: Tabular numerals for figures

**Files:**
- Modify: `src/FreelanceManager.App/Themes/Typography.axaml`

**Interfaces:**
- Produces: a `TextBlock.Metric` style class used by the dashboard figures and invoice totals.

- [ ] **Step 1: Append a `Metric` style** to `Typography.axaml` (before `</Styles>`):

```xml
<Style Selector="TextBlock.Metric">
  <Setter Property="FontWeight" Value="Bold"/>
  <Setter Property="FontFeatures" Value="+tnum"/>
  <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
</Style>
```

- [ ] **Step 2: Build to verify the style parses**

Run: `"C:\Program Files\dotnet\dotnet.exe" build`
Expected: build succeeds (style is applied by later tasks).

- [ ] **Step 3: Commit**

```bash
git add src/FreelanceManager.App/Themes/Typography.axaml
git commit -m "style: add tabular-numeral Metric text class"
```

---

### Task 4: Inviting empty states (icon + action)

**Files:**
- Modify: `src/FreelanceManager.App/Controls/EmptyState.axaml`
- Modify: `src/FreelanceManager.App/Controls/EmptyState.axaml.cs`
- Modify: `src/FreelanceManager.App/Views/ClientsView.axaml`, `ProjectsView.axaml`, `InvoicesView.axaml`

**Interfaces:**
- Produces: `EmptyState` control with existing `Message`/`Hint` plus new `Icon` (`Geometry`), `ActionText` (`string?`), `Command` (`ICommand?`) properties.

- [ ] **Step 1: Add the new StyledProperties** in `EmptyState.axaml.cs`. Open the file and add, alongside the existing `Message`/`Hint` properties:

```csharp
public static readonly StyledProperty<Geometry?> IconProperty =
    AvaloniaProperty.Register<EmptyState, Geometry?>(nameof(Icon));
public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

public static readonly StyledProperty<string?> ActionTextProperty =
    AvaloniaProperty.Register<EmptyState, string?>(nameof(ActionText));
public string? ActionText { get => GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }

public static readonly StyledProperty<System.Windows.Input.ICommand?> CommandProperty =
    AvaloniaProperty.Register<EmptyState, System.Windows.Input.ICommand?>(nameof(Command));
public System.Windows.Input.ICommand? Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
```

Ensure `using Avalonia;` and `using Avalonia.Media;` are present.

- [ ] **Step 2: Replace `EmptyState.axaml` body** with the richer layout:

```xml
<StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="10" MaxWidth="300">
  <Border Width="48" Height="48" CornerRadius="14" Background="{DynamicResource AccentSubtle}"
          HorizontalAlignment="Center">
    <Path Width="22" Height="22" Stretch="Uniform" HorizontalAlignment="Center" VerticalAlignment="Center"
          Fill="{DynamicResource AccentPrimary}" Data="{Binding Icon, ElementName=Root}"/>
  </Border>
  <TextBlock Classes="SectionHeading" HorizontalAlignment="Center" TextAlignment="Center"
             Text="{Binding Message, ElementName=Root}"/>
  <TextBlock Classes="Caption" HorizontalAlignment="Center" TextAlignment="Center" TextWrapping="Wrap"
             Text="{Binding Hint, ElementName=Root}"/>
  <Button Classes="primary" HorizontalAlignment="Center"
          Content="{Binding ActionText, ElementName=Root}"
          Command="{Binding Command, ElementName=Root}"
          IsVisible="{Binding ActionText, ElementName=Root, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"/>
</StackPanel>
```

- [ ] **Step 3: Wire the action into each list view.** In `ClientsView.axaml` replace the `<c:EmptyState .../>` with:

```xml
<c:EmptyState Message="No clients yet"
              Hint="Add your first client to start creating projects and invoices."
              Icon="{DynamicResource IconClients}"
              ActionText="Add a client" Command="{Binding NewCommand}"
              IsVisible="{Binding IsEmpty}"/>
```

In `ProjectsView.axaml`:

```xml
<c:EmptyState Message="No projects yet"
              Hint="Create a project to track handover details and deadlines."
              Icon="{DynamicResource IconProjects}"
              ActionText="New project" Command="{Binding NewCommand}"
              IsVisible="{Binding IsEmpty}"/>
```

In `InvoicesView.axaml`:

```xml
<c:EmptyState Message="No invoices yet"
              Hint="Create your first invoice and export it as a branded PDF."
              Icon="{DynamicResource IconInvoices}"
              ActionText="New invoice" Command="{Binding NewCommand}"
              IsVisible="{Binding IsEmpty}"/>
```

- [ ] **Step 4: Build and run, verify**

Run: `"C:\Program Files\dotnet\dotnet.exe" build`
Then launch the app. With an empty DB (or rename `%AppData%\FreelanceManager\freelance-manager.db` aside first), each list shows the icon, message, hint, and a working action button.

- [ ] **Step 5: Commit**

```bash
git add src/FreelanceManager.App/Controls/EmptyState.axaml src/FreelanceManager.App/Controls/EmptyState.axaml.cs src/FreelanceManager.App/Views/ClientsView.axaml src/FreelanceManager.App/Views/ProjectsView.axaml src/FreelanceManager.App/Views/InvoicesView.axaml
git commit -m "feat: inviting empty states with icon and primary action"
```

---

## Phase 2 — Shell quick-create

### Task 5: Global quick-create menu

**Files:**
- Modify: `src/FreelanceManager.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/FreelanceManager.App/Views/MainWindow.axaml`
- Test: `tests/FreelanceManager.Tests/MainWindowViewModelQuickCreateTests.cs`

**Interfaces:**
- Consumes: `DashboardViewModel`, `ClientsViewModel` (`NewCommand`), `ProjectsViewModel` (`NewCommand`), `InvoicesViewModel` (`NewCommand`) from DI.
- Produces: `MainWindowViewModel.QuickNewClientCommand`, `QuickNewProjectCommand`, `QuickNewInvoiceCommand` — each navigates to the target page and invokes its `NewCommand`.

- [ ] **Step 1: Write the failing test** at `tests/FreelanceManager.Tests/MainWindowViewModelQuickCreateTests.cs`:

```csharp
using FreelanceManager.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class MainWindowViewModelQuickCreateTests
{
    [Fact]
    public void QuickNewProject_navigates_to_Projects_page()
    {
        var services = TestServices.Build();   // see Step 3
        var vm = new MainWindowViewModel(services);

        vm.QuickNewProjectCommand.Execute(null);

        Assert.Equal("Projects", vm.ActivePage);
        Assert.IsType<ProjectsViewModel>(vm.CurrentPage);
    }
}
```

- [ ] **Step 2: Run it to confirm it fails to compile/run**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter MainWindowViewModelQuickCreateTests`
Expected: FAIL — `QuickNewProjectCommand` / `TestServices` don't exist.

- [ ] **Step 3: Add a minimal DI builder for tests** at `tests/FreelanceManager.Tests/TestServices.cs` mirroring the real registration the app uses (reuse `ServiceConfiguration` if it exposes a method; otherwise register the VMs and an in-memory/SQLite-backed repo set the existing tests already use). Minimal form:

```csharp
using FreelanceManager.App;
using Microsoft.Extensions.DependencyInjection;

public static class TestServices
{
    public static System.IServiceProvider Build()
    {
        var sc = new ServiceCollection();
        ServiceConfiguration.Register(sc);   // use the app's existing registration entry point
        return sc.BuildServiceProvider();
    }
}
```

If `ServiceConfiguration` has no public `Register(IServiceCollection)`, add one by extracting the existing registration body into a `public static void Register(IServiceCollection services)` method and calling it from the app's current composition root. Keep the app behavior identical.

- [ ] **Step 4: Implement the quick-create commands** in `MainWindowViewModel.cs`:

```csharp
[RelayCommand] private void QuickNewClient()
{
    ShowClients();
    (CurrentPage as ClientsViewModel)?.NewCommand.Execute(null);
}

[RelayCommand] private void QuickNewProject()
{
    ShowProjects();
    (CurrentPage as ProjectsViewModel)?.NewCommand.Execute(null);
}

[RelayCommand] private void QuickNewInvoice()
{
    ShowInvoices();
    (CurrentPage as InvoicesViewModel)?.NewCommand.Execute(null);
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter MainWindowViewModelQuickCreateTests`
Expected: PASS.

- [ ] **Step 6: Add the quick-create button to the shell.** In `MainWindow.axaml`, inside the sidebar `StackPanel`, directly under the logo row, add:

```xml
<Button Classes="primary" HorizontalAlignment="Stretch" Margin="6,0,6,10">
  <StackPanel Orientation="Horizontal" Spacing="8" HorizontalAlignment="Center">
    <TextBlock Text="+" FontWeight="Bold"/>
    <TextBlock Text="New"/>
  </StackPanel>
  <Button.Flyout>
    <MenuFlyout>
      <MenuItem Header="New invoice" Command="{Binding QuickNewInvoiceCommand}"/>
      <MenuItem Header="New project" Command="{Binding QuickNewProjectCommand}"/>
      <MenuItem Header="New client"  Command="{Binding QuickNewClientCommand}"/>
    </MenuFlyout>
  </Button.Flyout>
</Button>
```

- [ ] **Step 7: Build, run, verify** the New menu creates each record type from any page.

Run: `"C:\Program Files\dotnet\dotnet.exe" build` then run the app.

- [ ] **Step 8: Commit**

```bash
git add src/FreelanceManager.App/ViewModels/MainWindowViewModel.cs src/FreelanceManager.App/Views/MainWindow.axaml tests/FreelanceManager.Tests/MainWindowViewModelQuickCreateTests.cs tests/FreelanceManager.Tests/TestServices.cs src/FreelanceManager.App/ServiceConfiguration.cs
git commit -m "feat: global quick-create menu in shell"
```

---

## Phase 3 — Agenda home screen

### Task 6: Core agenda builder

**Files:**
- Create: `src/FreelanceManager.Core/Services/AgendaBuilder.cs`
- Test: `tests/FreelanceManager.Tests/AgendaBuilderTests.cs`

**Interfaces:**
- Produces: `record AgendaItem(DateTime Date, string Kind, string Title, string? Trailing)` and `static IReadOnlyList<AgendaItem> AgendaBuilder.BuildWeek(IEnumerable<Project> projects, IEnumerable<Invoice> invoices, DateTime today)`. `Kind` is `"Project"` or `"Invoice"`. Returns items whose due date falls in the Mon–Sun week containing `today`, ordered ascending by date.

- [ ] **Step 1: Write the failing test** at `tests/FreelanceManager.Tests/AgendaBuilderTests.cs`:

```csharp
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

public class AgendaBuilderTests
{
    [Fact]
    public void BuildWeek_includes_only_due_dates_in_current_week_sorted()
    {
        var today = new DateTime(2026, 6, 17); // a Wednesday
        var projects = new[]
        {
            new Project { Title = "In week",  DueDate = new DateTime(2026, 6, 19) },
            new Project { Title = "Next week", DueDate = new DateTime(2026, 6, 25) },
            new Project { Title = "No date",   DueDate = null },
        };
        var invoices = new[]
        {
            new Invoice { Number = "INV-1", DueDate = new DateTime(2026, 6, 16) },
        };

        var items = AgendaBuilder.BuildWeek(projects, invoices, today);

        Assert.Equal(2, items.Count);
        Assert.Equal(new DateTime(2026, 6, 16), items[0].Date); // invoice first (earlier)
        Assert.Equal("Invoice", items[0].Kind);
        Assert.Equal("In week", items[1].Title);
    }
}
```

- [ ] **Step 2: Run to confirm failure**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter AgendaBuilderTests`
Expected: FAIL — `AgendaBuilder` does not exist.

- [ ] **Step 3: Implement** `src/FreelanceManager.Core/Services/AgendaBuilder.cs`:

```csharp
namespace FreelanceManager.Core.Services;

using FreelanceManager.Core.Models;

public record AgendaItem(DateTime Date, string Kind, string Title, string? Trailing);

public static class AgendaBuilder
{
    public static IReadOnlyList<AgendaItem> BuildWeek(
        IEnumerable<Project> projects, IEnumerable<Invoice> invoices, DateTime today)
    {
        var monday = today.Date.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var sunday = monday.AddDays(6);
        bool InWeek(DateTime d) => d.Date >= monday && d.Date <= sunday;

        var items = new List<AgendaItem>();
        foreach (var p in projects)
            if (p.DueDate is { } d && InWeek(d))
                items.Add(new AgendaItem(d.Date, "Project", p.Title, "Deadline"));
        foreach (var i in invoices)
            if (InWeek(i.DueDate))
                items.Add(new AgendaItem(i.DueDate.Date, "Invoice", i.Number, "Due"));

        return items.OrderBy(x => x.Date).ThenBy(x => x.Kind).ToList();
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter AgendaBuilderTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FreelanceManager.Core/Services/AgendaBuilder.cs tests/FreelanceManager.Tests/AgendaBuilderTests.cs
git commit -m "feat: core agenda builder for current-week deadlines"
```

---

### Task 7: Dashboard view-model — agenda, pinned, metrics

**Files:**
- Modify: `src/FreelanceManager.App/ViewModels/DashboardViewModel.cs`
- Test: `tests/FreelanceManager.Tests/DashboardViewModelAgendaTests.cs`

**Interfaces:**
- Consumes: `IProjectRepository`, `IInvoiceRepository`, `IClock` (existing constructor).
- Produces: `ObservableCollection<AgendaItem> Agenda`, `ObservableCollection<Project> PinnedProjects` (derived: status==Active, newest `CreatedAt` first, max 5), plus existing `ActiveProjects`/`OverdueCount`/`OutstandingTotal`. Exposes `Task RefreshAsync()`.

- [ ] **Step 1: Write the failing test** at `tests/FreelanceManager.Tests/DashboardViewModelAgendaTests.cs`. Use the existing repository test doubles/fakes the suite already provides (mirror an existing ViewModel test's setup). Assert that after construction, `PinnedProjects` contains only Active projects, newest first, capped at 5, and `Agenda` reflects `AgendaBuilder.BuildWeek`. Example skeleton:

```csharp
using System.Linq;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using Xunit;

public class DashboardViewModelAgendaTests
{
    [Fact]
    public async System.Threading.Tasks.Task Pinned_shows_active_newest_first_capped_at_five()
    {
        var vm = DashboardTestFixture.WithProjects(   // build via existing fake repos
            Enumerable.Range(1, 7).Select(n => new Project
            {
                Id = n, Title = $"P{n}", Status = ProjectStatus.Active,
                CreatedAt = new System.DateTime(2026, 1, n)
            }).ToArray());

        await vm.RefreshAsync();

        Assert.Equal(5, vm.PinnedProjects.Count);
        Assert.Equal("P7", vm.PinnedProjects[0].Title); // newest first
    }
}
```

(If the suite has no existing fake repos, add a small in-memory `IProjectRepository`/`IInvoiceRepository` fake in the test project; keep it minimal.)

- [ ] **Step 2: Run to confirm failure**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter DashboardViewModelAgendaTests`
Expected: FAIL — members don't exist.

- [ ] **Step 3: Implement.** In `DashboardViewModel.cs`, add collections and refactor `LoadAsync` into a public `RefreshAsync`:

```csharp
using System.Collections.ObjectModel;
// ...
public ObservableCollection<AgendaItem> Agenda { get; } = new();
public ObservableCollection<Project> PinnedProjects { get; } = new();

public async Task RefreshAsync()
{
    try
    {
        var projects = (await _projects.GetAllAsync()).ToList();
        var invoices = (await _invoices.GetAllAsync()).ToList();

        ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);

        decimal outstanding = 0m; int overdue = 0;
        foreach (var i in invoices)
        {
            var eff = OverduePolicy.EffectiveStatus(i, _clock.Today);
            if (eff == InvoiceStatus.Overdue) overdue++;
            if (eff is InvoiceStatus.Sent or InvoiceStatus.Overdue)
                outstanding += InvoiceCalculator.Total(i);
        }
        OverdueCount = overdue;
        OutstandingTotal = outstanding;

        Agenda.Clear();
        foreach (var item in AgendaBuilder.BuildWeek(projects, invoices, _clock.Today))
            Agenda.Add(item);

        PinnedProjects.Clear();
        foreach (var p in projects.Where(p => p.Status == ProjectStatus.Active)
                                  .OrderByDescending(p => p.CreatedAt).Take(5))
            PinnedProjects.Add(p);
    }
    catch (System.Exception ex)
    {
        _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
    }
}
```

Replace the constructor's `_ = LoadAsync();` with `_ = RefreshAsync();` and delete the old `LoadAsync`. Add `using FreelanceManager.Core.Services;` if not present (for `AgendaBuilder`/`AgendaItem`).

- [ ] **Step 4: Run to verify pass**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter DashboardViewModelAgendaTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FreelanceManager.App/ViewModels/DashboardViewModel.cs tests/FreelanceManager.Tests/DashboardViewModelAgendaTests.cs
git commit -m "feat: dashboard agenda and pinned projects"
```

---

### Task 8: Dashboard agenda layout

**Files:**
- Modify: `src/FreelanceManager.App/Views/DashboardView.axaml`

Pure-XAML; verify by build + run.

- [ ] **Step 1: Replace `DashboardView.axaml` body** with the two-column agenda + compact metrics strip:

```xml
<StackPanel Spacing="16">
  <c:PageHeader Title="This week"/>

  <!-- compact metrics strip -->
  <StackPanel Orientation="Horizontal" Spacing="24">
    <StackPanel><TextBlock Classes="Caption" Text="Active projects"/>
      <TextBlock Classes="Metric" FontSize="20" Text="{Binding ActiveProjects}"/></StackPanel>
    <StackPanel><TextBlock Classes="Caption" Text="Outstanding"/>
      <TextBlock Classes="Metric" FontSize="20" Text="{Binding OutstandingTotal, StringFormat={}{0:0.00}}"/></StackPanel>
    <StackPanel><TextBlock Classes="Caption" Text="Overdue"/>
      <TextBlock Classes="Metric" FontSize="20" Foreground="{DynamicResource Danger}" Text="{Binding OverdueCount}"/></StackPanel>
  </StackPanel>

  <Grid ColumnDefinitions="*,260" >
    <!-- agenda -->
    <Border Grid.Column="0" Classes="card" Margin="0,0,16,0">
      <Panel>
        <ItemsControl ItemsSource="{Binding Agenda}">
          <ItemsControl.ItemTemplate>
            <DataTemplate>
              <Grid ColumnDefinitions="90,*,Auto" Margin="0,0,0,8">
                <TextBlock Grid.Column="0" Classes="Caption" Text="{Binding Date, StringFormat={}{0:ddd dd MMM}}"/>
                <TextBlock Grid.Column="1" Classes="Body" Text="{Binding Title}"/>
                <TextBlock Grid.Column="2" Classes="Caption" Text="{Binding Trailing}"/>
              </Grid>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
        <TextBlock Classes="Caption" Text="Nothing due this week."
                   HorizontalAlignment="Center" VerticalAlignment="Center"
                   IsVisible="{Binding !Agenda.Count}"/>
      </Panel>
    </Border>

    <!-- pinned projects -->
    <Border Grid.Column="1" Classes="card">
      <StackPanel Spacing="8">
        <TextBlock Classes="SectionHeading" Text="Active projects"/>
        <ItemsControl ItemsSource="{Binding PinnedProjects}">
          <ItemsControl.ItemTemplate>
            <DataTemplate>
              <TextBlock Classes="Body" Margin="0,0,0,4" Text="{Binding Title}"/>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </StackPanel>
    </Border>
  </Grid>
</StackPanel>
```

Confirm the `xmlns:c="using:FreelanceManager.App.Controls"` namespace is present on the root `UserControl` (it is in the current file).

- [ ] **Step 2: Build and run, verify**

Run: `"C:\Program Files\dotnet\dotnet.exe" build` then run the app.
Expected: dashboard shows the metrics strip, a week agenda of project/invoice due dates, and an active-projects list; empty week shows "Nothing due this week."

- [ ] **Step 3: Commit**

```bash
git add src/FreelanceManager.App/Views/DashboardView.axaml
git commit -m "feat: agenda dashboard layout"
```

---

## Phase 4 — Master-detail records

### Task 9: Invoices master-detail layout

**Files:**
- Modify: `src/FreelanceManager.App/Views/InvoicesView.axaml`
- Modify: `src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs`

The VM already has `Selected`, `Editor`, `IsEditing`. Change: selecting a row opens the editor in a right pane; list stays visible.

- [ ] **Step 1: Open the editor on selection.** In `InvoicesViewModel.cs`, add a partial method so selecting a row loads it into the editor:

```csharp
partial void OnSelectedChanged(InvoiceRow? value)
{
    if (value is not null) _ = Edit();
}
```

(`Edit()` already loads the full invoice into `Editor`.)

- [ ] **Step 2: Replace `InvoicesView.axaml` body** with a split layout (list left, detail right):

```xml
<Grid RowDefinitions="Auto,*">
  <c:PageHeader Grid.Row="0" Title="Invoices">
    <c:PageHeader.Actions>
      <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Classes="primary" Content="New" Command="{Binding NewCommand}"/>
      </StackPanel>
    </c:PageHeader.Actions>
  </c:PageHeader>

  <Grid Grid.Row="1" ColumnDefinitions="360,*">
    <!-- list -->
    <Border Grid.Column="0" BorderBrush="{DynamicResource Border}" BorderThickness="0,0,1,0" Margin="0,0,16,0">
      <Panel>
        <DataGrid ItemsSource="{Binding Invoices}" SelectedItem="{Binding Selected}"
                  IsReadOnly="True" AutoGenerateColumns="False">
          <DataGrid.Columns>
            <DataGridTextColumn Header="Number" Binding="{Binding Number}"/>
            <DataGridTextColumn Header="Client" Binding="{Binding ClientName}"/>
            <DataGridTemplateColumn Header="Status">
              <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                  <Button HorizontalAlignment="Center" Padding="0" Background="Transparent"
                          BorderThickness="0" Cursor="Hand">
                    <c:StatusBadge Status="{Binding Status}"/>
                    <Button.Flyout>
                      <MenuFlyout>
                        <MenuItem Header="Draft" Tag="{x:Static models:InvoiceStatus.Draft}" Click="OnSetInvoiceStatus"/>
                        <MenuItem Header="Sent" Tag="{x:Static models:InvoiceStatus.Sent}" Click="OnSetInvoiceStatus"/>
                        <MenuItem Header="Paid" Tag="{x:Static models:InvoiceStatus.Paid}" Click="OnSetInvoiceStatus"/>
                      </MenuFlyout>
                    </Button.Flyout>
                  </Button>
                </DataTemplate>
              </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>
          </DataGrid.Columns>
        </DataGrid>
        <c:EmptyState Message="No invoices yet"
                      Hint="Create your first invoice and export it as a branded PDF."
                      Icon="{DynamicResource IconInvoices}"
                      ActionText="New invoice" Command="{Binding NewCommand}"
                      IsVisible="{Binding IsEmpty}"/>
      </Panel>
    </Border>

    <!-- detail -->
    <Panel Grid.Column="1">
      <views:InvoiceEditView IsVisible="{Binding IsEditing}"/>
      <TextBlock Classes="Caption" Text="Select an invoice, or click New."
                 HorizontalAlignment="Center" VerticalAlignment="Center"
                 IsVisible="{Binding IsNotEditing}"/>
    </Panel>
  </Grid>
</Grid>
```

Keep the existing `OnSetInvoiceStatus` code-behind handler (unchanged) and the `Delete`/`Export PDF` actions — move them into the `InvoiceEditView` header (already has Save/Cancel) so they act on the open record. Add to `InvoiceEditView.axaml` header actions:

```xml
<Button Classes="secondary" Content="Export PDF" Command="{Binding ExportPdfCommand}"/>
<Button Classes="danger" Content="Delete" Command="{Binding DeleteCommand}"/>
```

- [ ] **Step 3: Build and run, verify** clicking a row opens its editor on the right with the list still visible; New opens a blank editor; Save/Delete/Export work; placeholder shows when nothing is selected.

Run: `"C:\Program Files\dotnet\dotnet.exe" build` then run.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Views/InvoicesView.axaml src/FreelanceManager.App/Views/InvoiceEditView.axaml src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs
git commit -m "feat: invoices master-detail layout"
```

---

### Task 10: Projects master-detail layout

**Files:**
- Modify: `src/FreelanceManager.App/Views/ProjectsView.axaml`
- Modify: `src/FreelanceManager.App/ViewModels/ProjectsViewModel.cs`

- [ ] **Step 1: Open editor on selection.** In `ProjectsViewModel.cs` add:

```csharp
partial void OnSelectedChanged(Project? value)
{
    if (value is not null) Edit();
}
```

- [ ] **Step 2: Replace `ProjectsView.axaml` body** with the same split structure as Task 9, adapted to projects:

```xml
<Grid RowDefinitions="Auto,*">
  <c:PageHeader Grid.Row="0" Title="Projects">
    <c:PageHeader.Actions>
      <Button Classes="primary" Content="New" Command="{Binding NewCommand}"/>
    </c:PageHeader.Actions>
  </c:PageHeader>

  <Grid Grid.Row="1" ColumnDefinitions="360,*">
    <Border Grid.Column="0" BorderBrush="{DynamicResource Border}" BorderThickness="0,0,1,0" Margin="0,0,16,0">
      <Panel>
        <DataGrid ItemsSource="{Binding Projects}" SelectedItem="{Binding Selected}"
                  IsReadOnly="True" AutoGenerateColumns="False">
          <DataGrid.Columns>
            <DataGridTextColumn Header="Title" Binding="{Binding Title}"/>
            <DataGridTemplateColumn Header="Status">
              <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                  <Button HorizontalAlignment="Center" Padding="0" Background="Transparent"
                          BorderThickness="0" Cursor="Hand">
                    <c:StatusBadge Status="{Binding Status}"/>
                    <Button.Flyout>
                      <MenuFlyout>
                        <MenuItem Header="Lead" Tag="{x:Static models:ProjectStatus.Lead}" Click="OnSetProjectStatus"/>
                        <MenuItem Header="Active" Tag="{x:Static models:ProjectStatus.Active}" Click="OnSetProjectStatus"/>
                        <MenuItem Header="Complete" Tag="{x:Static models:ProjectStatus.Complete}" Click="OnSetProjectStatus"/>
                        <MenuItem Header="Archived" Tag="{x:Static models:ProjectStatus.Archived}" Click="OnSetProjectStatus"/>
                      </MenuFlyout>
                    </Button.Flyout>
                  </Button>
                </DataTemplate>
              </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>
          </DataGrid.Columns>
        </DataGrid>
        <c:EmptyState Message="No projects yet"
                      Hint="Create a project to track handover details and deadlines."
                      Icon="{DynamicResource IconProjects}"
                      ActionText="New project" Command="{Binding NewCommand}"
                      IsVisible="{Binding IsEmpty}"/>
      </Panel>
    </Border>

    <Panel Grid.Column="1">
      <views:ProjectEditView IsVisible="{Binding IsEditing}"/>
      <TextBlock Classes="Caption" Text="Select a project, or click New."
                 HorizontalAlignment="Center" VerticalAlignment="Center"
                 IsVisible="{Binding IsNotEditing}"/>
    </Panel>
  </Grid>
</Grid>
```

Move the Delete action into `ProjectEditView.axaml`'s header actions (next to Save/Cancel):

```xml
<Button Classes="danger" Content="Delete" Command="{Binding DeleteCommand}"/>
```

- [ ] **Step 3: Build and run, verify** the projects split layout behaves like invoices.

Run: `"C:\Program Files\dotnet\dotnet.exe" build` then run.

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Views/ProjectsView.axaml src/FreelanceManager.App/Views/ProjectEditView.axaml src/FreelanceManager.App/ViewModels/ProjectsViewModel.cs
git commit -m "feat: projects master-detail layout"
```

---

### Task 11: Clients master-detail (retire the dialog for editing)

**Files:**
- Create: `src/FreelanceManager.App/Views/ClientEditView.axaml` (+ `.axaml.cs`)
- Modify: `src/FreelanceManager.App/ViewModels/ClientsViewModel.cs`
- Modify: `src/FreelanceManager.App/Views/ClientsView.axaml`
- Test: `tests/FreelanceManager.Tests/ClientsViewModelEditorTests.cs`

**Interfaces:**
- Produces on `ClientsViewModel`: `[ObservableProperty] ClientEditViewModel? Editor`, `bool IsEditing`/`IsNotEditing`, `NewCommand`/`SaveCommand`/`CancelCommand`/`DeleteCommand`. `NewCommand` and selecting a row both set `Editor`. `SaveCommand` persists and reloads. (The confirm-delete dialog stays; only the *edit* dialog is retired.)

- [ ] **Step 1: Write the failing test** at `tests/FreelanceManager.Tests/ClientsViewModelEditorTests.cs`:

```csharp
using FreelanceManager.App.ViewModels;
using Xunit;

public class ClientsViewModelEditorTests
{
    [Fact]
    public void New_opens_a_blank_editor()
    {
        var vm = ClientsTestFixture.Empty();   // existing fake repo helper
        vm.NewCommand.Execute(null);
        Assert.True(vm.IsEditing);
        Assert.NotNull(vm.Editor);
    }

    [Fact]
    public void Cancel_closes_the_editor()
    {
        var vm = ClientsTestFixture.Empty();
        vm.NewCommand.Execute(null);
        vm.CancelCommand.Execute(null);
        Assert.False(vm.IsEditing);
    }
}
```

- [ ] **Step 2: Run to confirm failure**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter ClientsViewModelEditorTests`
Expected: FAIL — members don't exist.

- [ ] **Step 3: Refactor `ClientsViewModel.cs`** to the inline-editor pattern (mirroring `ProjectsViewModel`). Replace the dialog-based `New`/`Edit` with:

```csharp
[ObservableProperty] private ClientEditViewModel? _editor;

public bool IsEditing => Editor is not null;
public bool IsNotEditing => Editor is null;

partial void OnEditorChanged(ClientEditViewModel? value)
{
    OnPropertyChanged(nameof(IsEditing));
    OnPropertyChanged(nameof(IsNotEditing));
}

partial void OnSelectedChanged(Client? value)
{
    if (value is not null) Editor = new ClientEditViewModel(value);
}

[RelayCommand] private void New() => Editor = new ClientEditViewModel(new Client());

[RelayCommand] private void Cancel() => Editor = null;

[RelayCommand]
private async Task Save()
{
    if (Editor is null) return;
    if (!Editor.IsValid) { _notes.Show("Name is required.", NotificationKind.Error); return; }
    try
    {
        if (Editor.Id == 0)
        {
            var model = new Client();
            Editor.ApplyTo(model);
            await _repo.AddAsync(model);
        }
        else
        {
            var model = await _repo.GetAsync(Editor.Id);
            if (model is not null) { Editor.ApplyTo(model); await _repo.UpdateAsync(model); }
        }
        Editor = null;
        _notes.Show("Client saved.", NotificationKind.Success);
        await LoadAsync();
    }
    catch (System.Exception ex)
    {
        _notes.Show($"Save failed: {ex.Message}", NotificationKind.Error);
    }
}
```

Keep the existing `Delete` command (it uses the confirm dialog — unchanged). Confirm `ClientEditViewModel` exposes `Id`, `IsValid`, and `ApplyTo(Client)` (it does — used by the old dialog flow). Verify `ClientEditViewModel.IsValid` exists; if the dialog used a different validation member, use that exact name.

- [ ] **Step 4: Create `ClientEditView.axaml`** — reuse the field layout from `Dialogs/ClientEditDialog.axaml` (copy its form fields), bound to `Editor`, with Save/Cancel/Delete header actions:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             xmlns:c="using:FreelanceManager.App.Controls"
             x:Class="FreelanceManager.App.Views.ClientEditView"
             x:DataType="vm:ClientsViewModel">
  <ScrollViewer>
    <StackPanel Spacing="16" MaxWidth="560" HorizontalAlignment="Left">
      <c:PageHeader Title="Client">
        <c:PageHeader.Actions>
          <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Classes="primary" Content="Save" Command="{Binding SaveCommand}"/>
            <Button Classes="secondary" Content="Cancel" Command="{Binding CancelCommand}"/>
            <Button Classes="danger" Content="Delete" Command="{Binding DeleteCommand}"/>
          </StackPanel>
        </c:PageHeader.Actions>
      </c:PageHeader>
      <Border Classes="card">
        <StackPanel Spacing="10" DataContext="{Binding Editor}" x:CompileBindings="False">
          <StackPanel Spacing="4"><TextBlock Classes="FieldLabel" Text="Name *"/><TextBox Text="{Binding Name}"/></StackPanel>
          <StackPanel Spacing="4"><TextBlock Classes="FieldLabel" Text="Company"/><TextBox Text="{Binding Company}"/></StackPanel>
          <StackPanel Spacing="4"><TextBlock Classes="FieldLabel" Text="Email"/><TextBox Text="{Binding Email}"/></StackPanel>
          <StackPanel Spacing="4"><TextBlock Classes="FieldLabel" Text="Phone"/><TextBox Text="{Binding Phone}"/></StackPanel>
        </StackPanel>
      </Border>
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

Match the exact field bindings (`Name`/`Company`/`Email`/`Phone` etc.) to whatever `ClientEditViewModel` actually exposes — open `Dialogs/ClientEditDialog.axaml` and copy its field set verbatim. Create the trivial `ClientEditView.axaml.cs` (standard `InitializeComponent`).

- [ ] **Step 5: Replace `ClientsView.axaml` body** with the split layout (list + `views:ClientEditView` detail), same structure as Task 10, columns `360,*`, with the empty state from Task 4 and a "Select a client, or click New." placeholder bound to `IsNotEditing`. Add `xmlns:views="using:FreelanceManager.App.Views"` to the root.

- [ ] **Step 6: Run the tests**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter ClientsViewModelEditorTests`
Expected: PASS.

- [ ] **Step 7: Build and run, verify** clients now edit inline (no dialog); delete still confirms; add/edit/save round-trips.

Run: `"C:\Program Files\dotnet\dotnet.exe" build` then run.

- [ ] **Step 8: Commit**

```bash
git add src/FreelanceManager.App/Views/ClientEditView.axaml src/FreelanceManager.App/Views/ClientEditView.axaml.cs src/FreelanceManager.App/ViewModels/ClientsViewModel.cs src/FreelanceManager.App/Views/ClientsView.axaml tests/FreelanceManager.Tests/ClientsViewModelEditorTests.cs
git commit -m "feat: clients master-detail inline editing"
```

---

## Phase 5 — Onboarding & line-item polish

### Task 12: App-state service + onboarding steps

**Files:**
- Create: `src/FreelanceManager.App/Services/IAppStateService.cs`
- Create: `src/FreelanceManager.App/Services/AppStateService.cs`
- Modify: `src/FreelanceManager.App/ServiceConfiguration.cs` (register singleton)
- Test: `tests/FreelanceManager.Tests/AppStateServiceTests.cs`

**Interfaces:**
- Produces: `interface IAppStateService { bool OnboardingDismissed { get; } void DismissOnboarding(); }` backed by a JSON file at `Path.Combine(AppPaths.DataDir, "app-state.json")`. Reads on construction; writes on dismiss.

- [ ] **Step 1: Write the failing test** at `tests/FreelanceManager.Tests/AppStateServiceTests.cs`:

```csharp
using System.IO;
using FreelanceManager.App.Services;
using Xunit;

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
```

- [ ] **Step 2: Run to confirm failure**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter AppStateServiceTests`
Expected: FAIL — type doesn't exist.

- [ ] **Step 3: Implement** the interface and service:

`IAppStateService.cs`:

```csharp
namespace FreelanceManager.App.Services;

public interface IAppStateService
{
    bool OnboardingDismissed { get; }
    void DismissOnboarding();
}
```

`AppStateService.cs`:

```csharp
using System.IO;
using System.Text.Json;

namespace FreelanceManager.App.Services;

public class AppStateService : IAppStateService
{
    private record State(bool OnboardingDismissed);
    private readonly string _path;
    private State _state;

    public AppStateService(string path)
    {
        _path = path;
        _state = File.Exists(_path)
            ? JsonSerializer.Deserialize<State>(File.ReadAllText(_path)) ?? new State(false)
            : new State(false);
    }

    public bool OnboardingDismissed => _state.OnboardingDismissed;

    public void DismissOnboarding()
    {
        _state = _state with { OnboardingDismissed = true };
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_state));
    }
}
```

- [ ] **Step 4: Register in DI** in `ServiceConfiguration.cs`:

```csharp
services.AddSingleton<IAppStateService>(_ =>
    new AppStateService(System.IO.Path.Combine(AppPaths.DataDir, "app-state.json")));
```

Use whatever the existing `AppPaths` member for the data directory is (the file shows `AppPaths.DatabasePath`/`AppPaths.DefaultBackupDir` exist — use the directory those live in; add an `AppPaths.DataDir` if not present).

- [ ] **Step 5: Run to verify pass**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter AppStateServiceTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/FreelanceManager.App/Services/IAppStateService.cs src/FreelanceManager.App/Services/AppStateService.cs src/FreelanceManager.App/ServiceConfiguration.cs tests/FreelanceManager.Tests/AppStateServiceTests.cs
git commit -m "feat: app-state service for onboarding dismissal"
```

---

### Task 13: First-run onboarding strip on dashboard

**Files:**
- Modify: `src/FreelanceManager.App/ViewModels/DashboardViewModel.cs`
- Modify: `src/FreelanceManager.App/Views/DashboardView.axaml`
- Test: `tests/FreelanceManager.Tests/DashboardViewModelOnboardingTests.cs`

**Interfaces:**
- Consumes: `IAppStateService`, `IBusinessProfileRepository`, `IClientRepository`/`IInvoiceRepository` (for derived step completion).
- Produces on `DashboardViewModel`: `bool ShowOnboarding` (true when not dismissed and not all steps done), `bool StepProfileDone`, `bool StepClientDone`, `bool StepInvoiceDone`, `DismissOnboardingCommand`.

- [ ] **Step 1: Write the failing test** at `tests/FreelanceManager.Tests/DashboardViewModelOnboardingTests.cs`:

```csharp
using Xunit;

public class DashboardViewModelOnboardingTests
{
    [Fact]
    public async System.Threading.Tasks.Task ShowOnboarding_false_after_dismiss()
    {
        var vm = DashboardTestFixture.Fresh();   // empty data, not dismissed
        await vm.RefreshAsync();
        Assert.True(vm.ShowOnboarding);

        vm.DismissOnboardingCommand.Execute(null);
        Assert.False(vm.ShowOnboarding);
    }
}
```

- [ ] **Step 2: Run to confirm failure**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter DashboardViewModelOnboardingTests`
Expected: FAIL.

- [ ] **Step 3: Implement.** Add `IAppStateService` and `IBusinessProfileRepository` to the `DashboardViewModel` constructor (update DI usage is automatic). Add:

```csharp
[ObservableProperty] private bool _showOnboarding;
[ObservableProperty] private bool _stepProfileDone;
[ObservableProperty] private bool _stepClientDone;
[ObservableProperty] private bool _stepInvoiceDone;

[RelayCommand]
private void DismissOnboarding()
{
    _appState.DismissOnboarding();
    ShowOnboarding = false;
}
```

In `RefreshAsync`, after loading, compute completion:

```csharp
var profile = await _profiles.GetAsync();
StepProfileDone = !string.IsNullOrWhiteSpace(profile.Name);
StepClientDone  = (await _clients.GetAllAsync()).Any();
StepInvoiceDone = invoices.Count > 0;
ShowOnboarding  = !_appState.OnboardingDismissed
                  && !(StepProfileDone && StepClientDone && StepInvoiceDone);
```

(Add `IClientRepository _clients` and `IBusinessProfileRepository _profiles` fields + constructor params + `using` for the repos.)

- [ ] **Step 4: Run to verify pass**

Run: `"C:\Program Files\dotnet\dotnet.exe" test --filter DashboardViewModelOnboardingTests`
Expected: PASS.

- [ ] **Step 5: Add the onboarding strip** to the top of `DashboardView.axaml` (above the metrics strip), visible only when `ShowOnboarding`:

```xml
<Border Classes="card" IsVisible="{Binding ShowOnboarding}">
  <StackPanel Spacing="8">
    <Grid ColumnDefinitions="*,Auto">
      <TextBlock Grid.Column="0" Classes="SectionHeading" Text="Welcome — let's get you set up"/>
      <Button Grid.Column="1" Classes="ghost" Content="Dismiss" Command="{Binding DismissOnboardingCommand}"/>
    </Grid>
    <CheckBox IsChecked="{Binding StepProfileDone, Mode=OneWay}" IsHitTestVisible="False" Content="Add your business profile &amp; logo"/>
    <CheckBox IsChecked="{Binding StepClientDone, Mode=OneWay}" IsHitTestVisible="False" Content="Add your first client"/>
    <CheckBox IsChecked="{Binding StepInvoiceDone, Mode=OneWay}" IsHitTestVisible="False" Content="Create your first invoice"/>
  </StackPanel>
</Border>
```

- [ ] **Step 6: Build and run, verify** a fresh DB shows the checklist; completing steps ticks them; Dismiss hides it permanently (survives restart).

Run: `"C:\Program Files\dotnet\dotnet.exe" build` then run.

- [ ] **Step 7: Commit**

```bash
git add src/FreelanceManager.App/ViewModels/DashboardViewModel.cs src/FreelanceManager.App/Views/DashboardView.axaml tests/FreelanceManager.Tests/DashboardViewModelOnboardingTests.cs
git commit -m "feat: first-run onboarding checklist on dashboard"
```

---

### Task 14: Cleaner invoice line-item editor

**Files:**
- Modify: `src/FreelanceManager.App/Views/InvoiceEditView.axaml`

Pure-XAML; the totals logic in `InvoiceEditViewModel` already exists and is unchanged.

- [ ] **Step 1: Replace the line-items + totals block** in `InvoiceEditView.axaml` (the `DataGrid`, Add line button, and totals `StackPanel`) with an aligned grid and a clearer live totals card:

```xml
<TextBlock Classes="SectionHeading" Text="Line items" Margin="0,6,0,0"/>
<DataGrid ItemsSource="{Binding Lines}" AutoGenerateColumns="False" Height="220"
          CanUserReorderColumns="False" GridLinesVisibility="Horizontal">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Description" Binding="{Binding Description}" Width="*"/>
    <DataGridTextColumn Header="Qty"  Binding="{Binding Quantity}"  Width="80"/>
    <DataGridTextColumn Header="Rate" Binding="{Binding UnitPrice}" Width="100"/>
    <DataGridTextColumn Header="Amount" Binding="{Binding LineTotal, StringFormat={}{0:0.00}}"
                        Width="110" IsReadOnly="True"/>
  </DataGrid.Columns>
</DataGrid>
<Button Classes="ghost" Content="+ Add line item" Command="{Binding AddLineCommand}" HorizontalAlignment="Left"/>

<Border Classes="card" HorizontalAlignment="Right" Padding="14,10" Margin="0,8,0,0">
  <Grid ColumnDefinitions="Auto,Auto" RowDefinitions="Auto,Auto,Auto" >
    <TextBlock Grid.Row="0" Grid.Column="0" Classes="Caption" Text="Subtotal" Margin="0,0,24,2"/>
    <TextBlock Grid.Row="0" Grid.Column="1" Classes="Metric" FontSize="13" HorizontalAlignment="Right"
               Text="{Binding Subtotal, StringFormat={}{0:0.00}}"/>
    <TextBlock Grid.Row="1" Grid.Column="0" Classes="Caption" Text="Tax" Margin="0,0,24,6"/>
    <TextBlock Grid.Row="1" Grid.Column="1" Classes="Metric" FontSize="13" HorizontalAlignment="Right"
               Text="{Binding Tax, StringFormat={}{0:0.00}}"/>
    <TextBlock Grid.Row="2" Grid.Column="0" Classes="SectionHeading" Text="Total" Margin="0,0,24,0"/>
    <TextBlock Grid.Row="2" Grid.Column="1" Classes="Metric" FontSize="17" HorizontalAlignment="Right"
               Text="{Binding Total, StringFormat={}{0:0.00}}"/>
  </Grid>
</Border>
```

- [ ] **Step 2: Build and run, verify** the line-item grid is aligned, Add line works, and Subtotal/Tax/Total update live with tabular figures.

Run: `"C:\Program Files\dotnet\dotnet.exe" build` then run; open an invoice and edit lines.

- [ ] **Step 3: Run the full test suite** to confirm nothing regressed:

Run: `"C:\Program Files\dotnet\dotnet.exe" test`
Expected: all tests pass (the original 50 plus the new ones).

- [ ] **Step 4: Commit**

```bash
git add src/FreelanceManager.App/Views/InvoiceEditView.axaml
git commit -m "style: cleaner invoice line-item editor with live totals"
```

---

## Self-review notes (coverage map)

- Spec §1 Visual system → Tasks 1, 2, 3
- Spec §2 Shell & navigation (quick-create) → Task 5
- Spec §3 Home = Agenda → Tasks 6, 7, 8
- Spec §4 Records master-detail → Tasks 9 (Invoices), 10 (Projects), 11 (Clients)
- Spec §5.1 calmer pills → Task 2
- Spec §5.2 empty states → Task 4
- Spec §5.3 onboarding → Tasks 12, 13
- Spec §5.4 line-item editor → Task 14
- Open questions resolved: pinned = derived (Task 7); onboarding state = JSON app-state file (Task 12); narrow-width = fixed proportional split columns (`360,*`) with no responsive collapse — acceptable given the wide default window. *(ponytail: fixed split; add responsive collapse only if users actually shrink the window.)*
