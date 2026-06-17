# Freelance Manager — Foundation + Thin Slices Implementation Plan

**Goal:** Build the native desktop foundation (app shell, clients, local SQLite database) plus working-but-minimal slices of invoicing and project tracking, with invoice PDF export.

**Architecture:** Layered .NET solution — `Core` (domain models + logic + service interfaces, no UI/DB), `Data` (EF Core + SQLite), `App` (Avalonia MVVM UI), `Tests` (xUnit). Business logic lives in Core/Data and is unit-tested without launching the GUI. DI wires services and the DbContext into ViewModels.

**Tech Stack:** .NET 10 (current LTS), Avalonia UI 11, CommunityToolkit.Mvvm, EF Core 10 + SQLite, QuestPDF, xUnit.

---

## Spec Reference

Implements `docs/specs/2026-06-16-freelance-manager-foundation-design.md`.

## Conventions for the executor

- Run all commands from the repository root: `c:/Users/Christian/Documents/2 CUSTOM SOFTWARE/Freelance Manager` unless a task says otherwise.
- The shell is PowerShell. Where a command spans the solution, the working directory is the repo root.
- Package versions below target .NET 10; if a restore fails on a version, use the latest `10.x`-compatible release of that package (and the latest stable for non-Microsoft packages).
- Commit after every task using the message shown. Never use `--no-verify`.
- TDD: write the failing test, watch it fail, implement, watch it pass, commit.

## File Structure (decomposition)

```
FreelanceManager.sln
├─ src/
│  ├─ FreelanceManager.Core/
│  │  ├─ Models/
│  │  │  ├─ Client.cs
│  │  │  ├─ Project.cs
│  │  │  ├─ ProjectStatus.cs
│  │  │  ├─ Invoice.cs
│  │  │  ├─ InvoiceStatus.cs
│  │  │  ├─ InvoiceLineItem.cs
│  │  │  └─ BusinessProfile.cs
│  │  └─ Services/
│  │     ├─ InvoiceCalculator.cs          // pure math (static)
│  │     ├─ OverduePolicy.cs              // pure derivation (static)
│  │     ├─ IInvoiceNumberGenerator.cs
│  │     ├─ InvoiceNumberGenerator.cs
│  │     ├─ IPdfExporter.cs
│  │     ├─ IBackupService.cs
│  │     └─ IClock.cs
│  ├─ FreelanceManager.Data/
│  │  ├─ AppDbContext.cs
│  │  ├─ AppDbContextFactory.cs           // design-time factory for migrations
│  │  ├─ Repositories/
│  │  │  ├─ IClientRepository.cs / ClientRepository.cs
│  │  │  ├─ IProjectRepository.cs / ProjectRepository.cs
│  │  │  ├─ IInvoiceRepository.cs / InvoiceRepository.cs
│  │  │  └─ IBusinessProfileRepository.cs / BusinessProfileRepository.cs
│  │  ├─ ClientInUseException.cs
│  │  ├─ BackupService.cs
│  │  └─ Migrations/                       // generated
│  └─ FreelanceManager.App/
│     ├─ Program.cs
│     ├─ App.axaml / App.axaml.cs
│     ├─ ServiceConfiguration.cs           // DI container setup
│     ├─ AppPaths.cs                        // app-data + db path resolution
│     ├─ Pdf/QuestPdfInvoiceExporter.cs
│     ├─ ViewModels/
│     │  ├─ ViewModelBase.cs
│     │  ├─ MainWindowViewModel.cs
│     │  ├─ DashboardViewModel.cs
│     │  ├─ ClientsViewModel.cs
│     │  ├─ ClientEditViewModel.cs
│     │  ├─ ProjectsViewModel.cs
│     │  ├─ ProjectEditViewModel.cs
│     │  ├─ InvoicesViewModel.cs
│     │  ├─ InvoiceEditViewModel.cs
│     │  ├─ LineItemViewModel.cs
│     │  └─ SettingsViewModel.cs
│     └─ Views/
│        ├─ MainWindow.axaml (+ .cs)
│        ├─ DashboardView, ClientsView, ClientEditView,
│        ├─ ProjectsView, ProjectEditView,
│        ├─ InvoicesView, InvoiceEditView, SettingsView (each .axaml + .cs)
└─ tests/
   └─ FreelanceManager.Tests/
      ├─ InvoiceCalculatorTests.cs
      ├─ OverduePolicyTests.cs
      ├─ InvoiceNumberGeneratorTests.cs
      ├─ ClientRepositoryTests.cs
      ├─ ProjectRepositoryTests.cs
      ├─ InvoiceRepositoryTests.cs
      ├─ BackupServiceTests.cs
      ├─ ClientEditViewModelTests.cs
      ├─ InvoiceEditViewModelTests.cs
      └─ TestDb.cs                          // shared SQLite test fixture helper
```

---

## Task 0: Prerequisites (environment)

**No code. This must be done before any build step works — the machine currently has no .NET SDK.**

- [ ] **Step 1: Install the .NET 10 SDK (x64)**

Download and install the latest .NET 10 SDK (x64) from https://dotnet.microsoft.com/download/dotnet/10.0 . On Windows you can also run:

```powershell
winget install Microsoft.DotNet.SDK.10
```

- [ ] **Step 2: Open a NEW terminal and verify the SDK is found**

Run: `dotnet --list-sdks`
Expected: at least one line like `10.0.xxx [C:\Program Files\dotnet\sdk]`. If empty, the new terminal did not pick up PATH — close and reopen, or log out/in.

- [ ] **Step 3: Install the Avalonia project templates**

Run: `dotnet new install Avalonia.Templates`
Expected: output listing installed templates including `Avalonia .NET App` (`avalonia.app`) and `Avalonia MVVM App` (`avalonia.mvvm`).

---

## Task 1: Solution and project scaffolding

**Files:**
- Create: `FreelanceManager.sln`, the four project files, and a `.gitignore` already exists.

- [ ] **Step 1: Create solution and the four projects**

```powershell
dotnet new sln -n FreelanceManager
dotnet new classlib -n FreelanceManager.Core -o src/FreelanceManager.Core -f net10.0
dotnet new classlib -n FreelanceManager.Data -o src/FreelanceManager.Data -f net10.0
dotnet new avalonia.mvvm -n FreelanceManager.App -o src/FreelanceManager.App
dotnet new xunit -n FreelanceManager.Tests -o tests/FreelanceManager.Tests -f net10.0
```

> Note: `dotnet new` classlib/xunit honor `-f net10.0`. The `avalonia.mvvm` template does not take `-f`; open `src/FreelanceManager.App/FreelanceManager.App.csproj` and confirm `<TargetFramework>net10.0</TargetFramework>` — if it shows an older TFM, change it to `net10.0`. All four projects must share the same TFM.

- [ ] **Step 2: Delete the default placeholder class files**

```powershell
Remove-Item src/FreelanceManager.Core/Class1.cs -ErrorAction SilentlyContinue
Remove-Item src/FreelanceManager.Data/Class1.cs -ErrorAction SilentlyContinue
Remove-Item tests/FreelanceManager.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 3: Add projects to the solution**

```powershell
dotnet sln add src/FreelanceManager.Core src/FreelanceManager.Data src/FreelanceManager.App tests/FreelanceManager.Tests
```

- [ ] **Step 4: Wire project references**

```powershell
dotnet add src/FreelanceManager.Data reference src/FreelanceManager.Core
dotnet add src/FreelanceManager.App reference src/FreelanceManager.Core src/FreelanceManager.Data
dotnet add tests/FreelanceManager.Tests reference src/FreelanceManager.Core src/FreelanceManager.Data
```

- [ ] **Step 5: Add NuGet packages**

```powershell
dotnet add src/FreelanceManager.Core package CommunityToolkit.Mvvm
dotnet add src/FreelanceManager.Data package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.0
dotnet add src/FreelanceManager.Data package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add src/FreelanceManager.App package Microsoft.Extensions.DependencyInjection --version 10.0.0
dotnet add src/FreelanceManager.App package QuestPDF
dotnet add tests/FreelanceManager.Tests package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.0
```

(`CommunityToolkit.Mvvm` is added to Core so models/VMs can use `ObservableObject`. The Avalonia template usually already adds it to App; if `dotnet add` reports it's already present, that's fine.)

- [ ] **Step 6: Build the empty solution**

Run: `dotnet build`
Expected: `Build succeeded` with 0 errors (warnings OK).

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "chore: scaffold solution (Core, Data, App, Tests)"
```

---

## Task 2: Core domain models and enums

**Files:**
- Create: `src/FreelanceManager.Core/Models/*.cs` (all model + enum files)

No tests in this task — these are plain data holders; their logic is tested in Tasks 3-5.

- [ ] **Step 1: Create the enums**

`src/FreelanceManager.Core/Models/ProjectStatus.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public enum ProjectStatus
{
    Lead,
    Active,
    Complete,
    Archived
}
```

`src/FreelanceManager.Core/Models/InvoiceStatus.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue
}
```

- [ ] **Step 2: Create `Client`**

`src/FreelanceManager.Core/Models/Client.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Project> Projects { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
}
```

- [ ] **Step 3: Create `Project`**

`src/FreelanceManager.Core/Models/Project.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public class Project
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public string Title { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Lead;

    public string? RepoUrl { get; set; }
    public string? LiveSiteUrl { get; set; }
    public string? HostingNotes { get; set; }
    public string? CredentialsLocation { get; set; }
    public string? BuildStackNotes { get; set; }
    public string? GeneralNotes { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Invoice> Invoices { get; set; } = new();
}
```

- [ ] **Step 4: Create `InvoiceLineItem`**

`src/FreelanceManager.Core/Models/InvoiceLineItem.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public class InvoiceLineItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
```

- [ ] **Step 5: Create `Invoice`**

`src/FreelanceManager.Core/Models/Invoice.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public class Invoice
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public int? ProjectId { get; set; }   // nullable: standalone invoices allowed
    public Project? Project { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string Currency { get; set; } = "USD";
    public decimal TaxRate { get; set; }   // e.g. 0.20 for 20%
    public string? Notes { get; set; }

    public List<InvoiceLineItem> LineItems { get; set; } = new();
}
```

- [ ] **Step 6: Create `BusinessProfile`**

`src/FreelanceManager.Core/Models/BusinessProfile.cs`:

```csharp
namespace FreelanceManager.Core.Models;

public class BusinessProfile
{
    public int Id { get; set; }   // always 1 (singleton row)
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LogoPath { get; set; }

    public string DefaultCurrency { get; set; } = "USD";
    public decimal DefaultTaxRate { get; set; }
    public string InvoiceNumberFormat { get; set; } = "INV-{YYYY}-{0000}";
}
```

- [ ] **Step 7: Build**

Run: `dotnet build src/FreelanceManager.Core`
Expected: `Build succeeded`.

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "feat(core): add domain models and enums"
```

---

## Task 3: Invoice calculation logic (TDD)

Money math centralized and rounded once. Banker's rounding avoided — use `MidpointRounding.AwayFromZero` to 2 decimals, which matches invoice conventions.

**Files:**
- Create: `src/FreelanceManager.Core/Services/InvoiceCalculator.cs`
- Test: `tests/FreelanceManager.Tests/InvoiceCalculatorTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/FreelanceManager.Tests/InvoiceCalculatorTests.cs`:

```csharp
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceCalculatorTests
{
    private static Invoice MakeInvoice(decimal taxRate, params (decimal qty, decimal price)[] lines)
    {
        var inv = new Invoice { TaxRate = taxRate };
        foreach (var (qty, price) in lines)
            inv.LineItems.Add(new InvoiceLineItem { Quantity = qty, UnitPrice = price });
        return inv;
    }

    [Fact]
    public void Subtotal_sums_line_totals()
    {
        var inv = MakeInvoice(0m, (2m, 50m), (1m, 25m));
        Assert.Equal(125m, InvoiceCalculator.Subtotal(inv));
    }

    [Fact]
    public void Tax_is_subtotal_times_rate_rounded_to_two_places()
    {
        var inv = MakeInvoice(0.2m, (1m, 99.99m));
        Assert.Equal(20.00m, InvoiceCalculator.Tax(inv));
    }

    [Fact]
    public void Total_is_subtotal_plus_tax()
    {
        var inv = MakeInvoice(0.2m, (1m, 100m));
        Assert.Equal(120.00m, InvoiceCalculator.Total(inv));
    }

    [Fact]
    public void Empty_invoice_totals_zero()
    {
        var inv = MakeInvoice(0.2m);
        Assert.Equal(0m, InvoiceCalculator.Subtotal(inv));
        Assert.Equal(0m, InvoiceCalculator.Tax(inv));
        Assert.Equal(0m, InvoiceCalculator.Total(inv));
    }

    [Fact]
    public void Rounding_is_away_from_zero_at_midpoint()
    {
        // subtotal 10.125 * tax 1.0 -> 10.13 (not 10.12)
        var inv = MakeInvoice(1.0m, (1m, 10.125m));
        Assert.Equal(10.13m, InvoiceCalculator.Tax(inv));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter InvoiceCalculatorTests`
Expected: FAIL — `InvoiceCalculator` does not exist (compile error).

- [ ] **Step 3: Implement**

`src/FreelanceManager.Core/Services/InvoiceCalculator.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.Core.Services;

public static class InvoiceCalculator
{
    public static decimal Subtotal(Invoice invoice)
    {
        decimal sum = 0m;
        foreach (var item in invoice.LineItems)
            sum += item.LineTotal;
        return Round(sum);
    }

    public static decimal Tax(Invoice invoice)
        => Round(Subtotal(invoice) * invoice.TaxRate);

    public static decimal Total(Invoice invoice)
        => Subtotal(invoice) + Tax(invoice);

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter InvoiceCalculatorTests`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(core): invoice subtotal/tax/total calculation"
```

---

## Task 4: Overdue derivation (TDD)

Overdue is computed, never stored. A `Sent` invoice past its due date is overdue; `Draft` and `Paid` are never overdue.

**Files:**
- Create: `src/FreelanceManager.Core/Services/IClock.cs`, `src/FreelanceManager.Core/Services/OverduePolicy.cs`
- Test: `tests/FreelanceManager.Tests/OverduePolicyTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/FreelanceManager.Tests/OverduePolicyTests.cs`:

```csharp
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

namespace FreelanceManager.Tests;

public class OverduePolicyTests
{
    private static readonly DateTime Today = new(2026, 6, 16);

    private static Invoice Inv(InvoiceStatus status, DateTime due)
        => new() { Status = status, DueDate = due };

    [Fact]
    public void Sent_and_past_due_is_overdue()
        => Assert.True(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Sent, Today.AddDays(-1)), Today));

    [Fact]
    public void Sent_and_due_today_is_not_overdue()
        => Assert.False(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Sent, Today), Today));

    [Fact]
    public void Paid_is_never_overdue()
        => Assert.False(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Paid, Today.AddDays(-30)), Today));

    [Fact]
    public void Draft_is_never_overdue()
        => Assert.False(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Draft, Today.AddDays(-30)), Today));

    [Fact]
    public void EffectiveStatus_reports_Overdue_for_overdue_invoice()
        => Assert.Equal(InvoiceStatus.Overdue,
            OverduePolicy.EffectiveStatus(Inv(InvoiceStatus.Sent, Today.AddDays(-5)), Today));

    [Fact]
    public void EffectiveStatus_passes_through_when_not_overdue()
        => Assert.Equal(InvoiceStatus.Paid,
            OverduePolicy.EffectiveStatus(Inv(InvoiceStatus.Paid, Today.AddDays(-5)), Today));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter OverduePolicyTests`
Expected: FAIL — `OverduePolicy` does not exist.

- [ ] **Step 3: Implement `IClock` and `OverduePolicy`**

`src/FreelanceManager.Core/Services/IClock.cs`:

```csharp
namespace FreelanceManager.Core.Services;

public interface IClock
{
    DateTime Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Today => DateTime.Today;
}
```

`src/FreelanceManager.Core/Services/OverduePolicy.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.Core.Services;

public static class OverduePolicy
{
    public static bool IsOverdue(Invoice invoice, DateTime today)
        => invoice.Status == InvoiceStatus.Sent && invoice.DueDate.Date < today.Date;

    public static InvoiceStatus EffectiveStatus(Invoice invoice, DateTime today)
        => IsOverdue(invoice, today) ? InvoiceStatus.Overdue : invoice.Status;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter OverduePolicyTests`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(core): derive overdue invoice status"
```

---

## Task 5: Invoice number generator (TDD)

Generates the next sequential number from the configured format. Supported tokens: `{YYYY}` → 4-digit year; a run of zeros (e.g. `{0000}`) → zero-padded sequence. The generator is given the highest existing sequence number for the current year and returns the next.

**Files:**
- Create: `src/FreelanceManager.Core/Services/IInvoiceNumberGenerator.cs`, `InvoiceNumberGenerator.cs`
- Test: `tests/FreelanceManager.Tests/InvoiceNumberGeneratorTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/FreelanceManager.Tests/InvoiceNumberGeneratorTests.cs`:

```csharp
using FreelanceManager.Core.Services;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceNumberGeneratorTests
{
    private readonly InvoiceNumberGenerator _gen = new();

    [Fact]
    public void First_number_of_year_uses_sequence_one()
        => Assert.Equal("INV-2026-0001", _gen.Next("INV-{YYYY}-{0000}", 2026, lastSequenceThisYear: 0));

    [Fact]
    public void Increments_from_last_sequence()
        => Assert.Equal("INV-2026-0008", _gen.Next("INV-{YYYY}-{0000}", 2026, lastSequenceThisYear: 7));

    [Fact]
    public void Pads_to_width_of_zero_run()
        => Assert.Equal("INV-2026-042", _gen.Next("INV-{YYYY}-{000}", 2026, lastSequenceThisYear: 41));

    [Fact]
    public void Sequence_wider_than_padding_is_not_truncated()
        => Assert.Equal("INV-2026-1000", _gen.Next("INV-{YYYY}-{000}", 2026, lastSequenceThisYear: 999));

    [Fact]
    public void Year_token_is_substituted()
        => Assert.Equal("2027/0001", _gen.Next("{YYYY}/{0000}", 2027, lastSequenceThisYear: 0));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter InvoiceNumberGeneratorTests`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement**

`src/FreelanceManager.Core/Services/IInvoiceNumberGenerator.cs`:

```csharp
namespace FreelanceManager.Core.Services;

public interface IInvoiceNumberGenerator
{
    string Next(string format, int year, int lastSequenceThisYear);
}
```

`src/FreelanceManager.Core/Services/InvoiceNumberGenerator.cs`:

```csharp
using System.Text.RegularExpressions;

namespace FreelanceManager.Core.Services;

public sealed class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    public string Next(string format, int year, int lastSequenceThisYear)
    {
        int seq = lastSequenceThisYear + 1;
        string result = format.Replace("{YYYY}", year.ToString("D4"));

        result = Regex.Replace(result, @"\{(0+)\}", m =>
        {
            int width = m.Groups[1].Value.Length;
            return seq.ToString(new string('0', width));
        });

        return result;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter InvoiceNumberGeneratorTests`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(core): sequential invoice number generator"
```

---

## Task 6: EF Core DbContext + design-time factory

**Files:**
- Create: `src/FreelanceManager.Data/AppDbContext.cs`, `src/FreelanceManager.Data/AppDbContextFactory.cs`

- [ ] **Step 1: Create `AppDbContext`**

`src/FreelanceManager.Data/AppDbContext.cs`:

```csharp
using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.HasMany(c => c.Projects).WithOne(p => p.Client!)
                .HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Invoices).WithOne(i => i.Client!)
                .HasForeignKey(i => i.ClientId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Project>(e =>
        {
            e.HasMany(p => p.Invoices).WithOne(i => i.Project!)
                .HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Invoice>(e =>
        {
            e.HasMany(i => i.LineItems).WithOne()
                .HasForeignKey(li => li.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.Property(i => i.TaxRate).HasPrecision(9, 4);
        });

        b.Entity<InvoiceLineItem>(e =>
        {
            e.Ignore(li => li.LineTotal);   // computed, not persisted
            e.Property(li => li.Quantity).HasPrecision(18, 4);
            e.Property(li => li.UnitPrice).HasPrecision(18, 4);
        });

        b.Entity<BusinessProfile>(e =>
        {
            e.Property(p => p.DefaultTaxRate).HasPrecision(9, 4);
        });
    }
}
```

- [ ] **Step 2: Create the design-time factory (needed for `dotnet ef migrations`)**

`src/FreelanceManager.Data/AppDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FreelanceManager.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design_time.db")
            .Options;
        return new AppDbContext(options);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/FreelanceManager.Data`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feat(data): AppDbContext with relationships and precision"
```

---

## Task 7: Initial EF migration

**Files:**
- Create: `src/FreelanceManager.Data/Migrations/*` (generated)

- [ ] **Step 1: Install the EF tool (once per machine)**

Run: `dotnet tool install --global dotnet-ef --version 10.0.0`
Expected: success, or "already installed". If the global tool path isn't on PATH, open a new terminal.

- [ ] **Step 2: Create the initial migration**

Run from repo root:

```powershell
dotnet ef migrations add InitialCreate --project src/FreelanceManager.Data --startup-project src/FreelanceManager.Data
```

Expected: `Done.` and a new `Migrations/` folder with `*_InitialCreate.cs`.

- [ ] **Step 3: Verify it builds**

Run: `dotnet build src/FreelanceManager.Data`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feat(data): initial EF Core migration"
```

---

## Task 8: Shared test DB fixture

A helper that spins up a real SQLite database on a temp file (so foreign-key/`Restrict` behavior is exercised, unlike the in-memory provider) and applies the schema via `EnsureCreated`.

**Files:**
- Create: `tests/FreelanceManager.Tests/TestDb.cs`

- [ ] **Step 1: Create the fixture helper**

`tests/FreelanceManager.Tests/TestDb.cs`:

```csharp
using System;
using FreelanceManager.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Tests;

/// <summary>
/// A disposable SQLite database backed by a shared in-memory connection.
/// FK enforcement is on, so DeleteBehavior.Restrict is actually tested.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public DbContextOptions<AppDbContext> Options { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Cache=Shared");
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = NewContext();
        ctx.Database.EnsureCreated();
    }

    public AppDbContext NewContext() => new(Options);

    public void Dispose() => _connection.Dispose();
}
```

- [ ] **Step 2: Add the SQLite package to tests (if not already present from Task 1)**

Run: `dotnet add tests/FreelanceManager.Tests package Microsoft.Data.Sqlite --version 10.0.0`
Expected: success (this provides `SqliteConnection`).

- [ ] **Step 3: Build tests**

Run: `dotnet build tests/FreelanceManager.Tests`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "test: shared SQLite test database fixture"
```

---

## Task 9: Client repository + in-use guard (TDD)

**Files:**
- Create: `src/FreelanceManager.Data/ClientInUseException.cs`, `Repositories/IClientRepository.cs`, `Repositories/ClientRepository.cs`
- Test: `tests/FreelanceManager.Tests/ClientRepositoryTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/FreelanceManager.Tests/ClientRepositoryTests.cs`:

```csharp
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ClientRepositoryTests
{
    [Fact]
    public async Task Add_then_GetAll_returns_the_client()
    {
        using var db = new TestDb();
        var repo = new ClientRepository(db.NewContext());

        await repo.AddAsync(new Client { Name = "Acme" });

        var repo2 = new ClientRepository(db.NewContext());
        var all = await repo2.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Acme", all[0].Name);
    }

    [Fact]
    public async Task Delete_client_without_dependents_succeeds()
    {
        using var db = new TestDb();
        var repo = new ClientRepository(db.NewContext());
        var c = await repo.AddAsync(new Client { Name = "Temp" });

        await new ClientRepository(db.NewContext()).DeleteAsync(c.Id);

        var all = await new ClientRepository(db.NewContext()).GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task Delete_client_with_project_throws_ClientInUse()
    {
        using var db = new TestDb();
        var c = await new ClientRepository(db.NewContext()).AddAsync(new Client { Name = "Busy" });

        await using (var ctx = db.NewContext())
        {
            ctx.Projects.Add(new Project { ClientId = c.Id, Title = "Site" });
            await ctx.SaveChangesAsync();
        }

        var repo = new ClientRepository(db.NewContext());
        await Assert.ThrowsAsync<ClientInUseException>(() => repo.DeleteAsync(c.Id));
    }

    [Fact]
    public async Task Delete_client_with_invoice_throws_ClientInUse()
    {
        using var db = new TestDb();
        var c = await new ClientRepository(db.NewContext()).AddAsync(new Client { Name = "Billed" });

        await using (var ctx = db.NewContext())
        {
            ctx.Invoices.Add(new Invoice { ClientId = c.Id, Number = "INV-1" });
            await ctx.SaveChangesAsync();
        }

        var repo = new ClientRepository(db.NewContext());
        await Assert.ThrowsAsync<ClientInUseException>(() => repo.DeleteAsync(c.Id));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter ClientRepositoryTests`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement the exception and repository**

`src/FreelanceManager.Data/ClientInUseException.cs`:

```csharp
namespace FreelanceManager.Data;

public class ClientInUseException : Exception
{
    public ClientInUseException(string message) : base(message) { }
}
```

`src/FreelanceManager.Data/Repositories/IClientRepository.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IClientRepository
{
    Task<List<Client>> GetAllAsync();
    Task<Client?> GetAsync(int id);
    Task<Client> AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(int id);   // throws ClientInUseException if dependents exist
}
```

`src/FreelanceManager.Data/Repositories/ClientRepository.cs`:

```csharp
using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;
    public ClientRepository(AppDbContext db) => _db = db;

    public async Task<List<Client>> GetAllAsync()
        => await _db.Clients.OrderBy(c => c.Name).ToListAsync();

    public async Task<Client?> GetAsync(int id)
        => await _db.Clients.FindAsync(id);

    public async Task<Client> AddAsync(Client client)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return client;
    }

    public async Task UpdateAsync(Client client)
    {
        _db.Clients.Update(client);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        bool hasProjects = await _db.Projects.AnyAsync(p => p.ClientId == id);
        bool hasInvoices = await _db.Invoices.AnyAsync(i => i.ClientId == id);
        if (hasProjects || hasInvoices)
            throw new ClientInUseException(
                "This client has projects or invoices and cannot be deleted.");

        var client = await _db.Clients.FindAsync(id);
        if (client is null) return;
        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter ClientRepositoryTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(data): client repository with in-use delete guard"
```

---

## Task 10: Project repository (TDD)

**Files:**
- Create: `src/FreelanceManager.Data/Repositories/IProjectRepository.cs`, `ProjectRepository.cs`
- Test: `tests/FreelanceManager.Tests/ProjectRepositoryTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/FreelanceManager.Tests/ProjectRepositoryTests.cs`:

```csharp
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ProjectRepositoryTests
{
    private static async Task<int> SeedClientAsync(TestDb db)
    {
        var c = await new ClientRepository(db.NewContext()).AddAsync(new Client { Name = "Acme" });
        return c.Id;
    }

    [Fact]
    public async Task Add_persists_all_handover_fields()
    {
        using var db = new TestDb();
        int clientId = await SeedClientAsync(db);
        var repo = new ProjectRepository(db.NewContext());

        await repo.AddAsync(new Project
        {
            ClientId = clientId,
            Title = "Marketing site",
            Status = ProjectStatus.Active,
            RepoUrl = "https://github.com/acme/site",
            LiveSiteUrl = "https://acme.com",
            HostingNotes = "Netlify",
            CredentialsLocation = "1Password vault 'Acme'",
            BuildStackNotes = "Astro + Tailwind",
            GeneralNotes = "Launch June"
        });

        var saved = (await new ProjectRepository(db.NewContext()).GetAllAsync()).Single();
        Assert.Equal("Marketing site", saved.Title);
        Assert.Equal(ProjectStatus.Active, saved.Status);
        Assert.Equal("Netlify", saved.HostingNotes);
        Assert.Equal("1Password vault 'Acme'", saved.CredentialsLocation);
        Assert.Equal("Astro + Tailwind", saved.BuildStackNotes);
    }

    [Fact]
    public async Task GetByClient_filters_to_that_client()
    {
        using var db = new TestDb();
        int a = await SeedClientAsync(db);
        int b = (await new ClientRepository(db.NewContext()).AddAsync(new Client { Name = "B" })).Id;
        var repo = new ProjectRepository(db.NewContext());
        await repo.AddAsync(new Project { ClientId = a, Title = "A1" });
        await repo.AddAsync(new Project { ClientId = b, Title = "B1" });

        var forA = await new ProjectRepository(db.NewContext()).GetByClientAsync(a);
        Assert.Single(forA);
        Assert.Equal("A1", forA[0].Title);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter ProjectRepositoryTests`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement**

`src/FreelanceManager.Data/Repositories/IProjectRepository.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync();
    Task<List<Project>> GetByClientAsync(int clientId);
    Task<Project?> GetAsync(int id);
    Task<Project> AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(int id);
}
```

`src/FreelanceManager.Data/Repositories/ProjectRepository.cs`:

```csharp
using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _db;
    public ProjectRepository(AppDbContext db) => _db = db;

    public async Task<List<Project>> GetAllAsync()
        => await _db.Projects.Include(p => p.Client).OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<List<Project>> GetByClientAsync(int clientId)
        => await _db.Projects.Where(p => p.ClientId == clientId)
                             .OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<Project?> GetAsync(int id)
        => await _db.Projects.Include(p => p.Client)
                             .Include(p => p.Invoices)
                             .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Project> AddAsync(Project project)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        _db.Projects.Update(project);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p is null) return;
        _db.Projects.Remove(p);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter ProjectRepositoryTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(data): project repository"
```

---

## Task 11: Invoice repository + next-sequence query (TDD)

The repository persists invoices with line items and exposes the highest sequence number used in a given year (parsed from existing `Number`s) so the generator can produce the next one.

**Files:**
- Create: `src/FreelanceManager.Data/Repositories/IInvoiceRepository.cs`, `InvoiceRepository.cs`
- Test: `tests/FreelanceManager.Tests/InvoiceRepositoryTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/FreelanceManager.Tests/InvoiceRepositoryTests.cs`:

```csharp
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceRepositoryTests
{
    private static async Task<int> SeedClientAsync(TestDb db)
        => (await new ClientRepository(db.NewContext()).AddAsync(new Client { Name = "Acme" })).Id;

    [Fact]
    public async Task Add_persists_invoice_with_line_items()
    {
        using var db = new TestDb();
        int clientId = await SeedClientAsync(db);
        var repo = new InvoiceRepository(db.NewContext());

        await repo.AddAsync(new Invoice
        {
            ClientId = clientId,
            Number = "INV-2026-0001",
            TaxRate = 0.2m,
            LineItems = { new InvoiceLineItem { Description = "Design", Quantity = 2, UnitPrice = 100 } }
        });

        var saved = (await new InvoiceRepository(db.NewContext()).GetAllAsync()).Single();
        Assert.Equal("INV-2026-0001", saved.Number);
        Assert.Single(saved.LineItems);
        Assert.Equal(200m, saved.LineItems[0].LineTotal);
    }

    [Fact]
    public async Task MaxSequenceForYear_is_zero_when_no_invoices()
    {
        using var db = new TestDb();
        var repo = new InvoiceRepository(db.NewContext());
        Assert.Equal(0, await repo.GetMaxSequenceForYearAsync(2026));
    }

    [Fact]
    public async Task MaxSequenceForYear_reads_trailing_number_of_matching_year()
    {
        using var db = new TestDb();
        int clientId = await SeedClientAsync(db);
        var repo = new InvoiceRepository(db.NewContext());
        await repo.AddAsync(new Invoice { ClientId = clientId, Number = "INV-2026-0003" });
        await repo.AddAsync(new Invoice { ClientId = clientId, Number = "INV-2026-0007" });
        await repo.AddAsync(new Invoice { ClientId = clientId, Number = "INV-2025-0099" });

        Assert.Equal(7, await new InvoiceRepository(db.NewContext()).GetMaxSequenceForYearAsync(2026));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter InvoiceRepositoryTests`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement**

`src/FreelanceManager.Data/Repositories/IInvoiceRepository.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IInvoiceRepository
{
    Task<List<Invoice>> GetAllAsync();
    Task<Invoice?> GetAsync(int id);
    Task<Invoice> AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(int id);

    /// <summary>Highest trailing sequence among invoice numbers ending in -NNNN for the given year.</summary>
    Task<int> GetMaxSequenceForYearAsync(int year);
}
```

`src/FreelanceManager.Data/Repositories/InvoiceRepository.cs`:

```csharp
using System.Text.RegularExpressions;
using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;
    public InvoiceRepository(AppDbContext db) => _db = db;

    public async Task<List<Invoice>> GetAllAsync()
        => await _db.Invoices.Include(i => i.Client).Include(i => i.Project)
                             .Include(i => i.LineItems)
                             .OrderByDescending(i => i.IssueDate).ToListAsync();

    public async Task<Invoice?> GetAsync(int id)
        => await _db.Invoices.Include(i => i.Client).Include(i => i.Project)
                             .Include(i => i.LineItems)
                             .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Invoice> AddAsync(Invoice invoice)
    {
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        // replace line items wholesale to keep edit logic simple and correct
        var existing = await _db.Invoices.Include(i => i.LineItems)
                                         .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        if (existing is null) return;

        _db.InvoiceLineItems.RemoveRange(existing.LineItems);
        _db.Entry(existing).CurrentValues.SetValues(invoice);
        existing.LineItems = invoice.LineItems;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var inv = await _db.Invoices.FindAsync(id);
        if (inv is null) return;
        _db.Invoices.Remove(inv);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetMaxSequenceForYearAsync(int year)
    {
        var numbers = await _db.Invoices.Select(i => i.Number).ToListAsync();
        int max = 0;
        var rx = new Regex(@"(\d+)\s*$");      // trailing digits
        foreach (var n in numbers)
        {
            if (n.Contains(year.ToString("D4")))
            {
                var m = rx.Match(n);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int seq) && seq > max)
                    max = seq;
            }
        }
        return max;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter InvoiceRepositoryTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(data): invoice repository with next-sequence query"
```

---

## Task 12: BusinessProfile repository (get-or-create singleton)

**Files:**
- Create: `src/FreelanceManager.Data/Repositories/IBusinessProfileRepository.cs`, `BusinessProfileRepository.cs`
- Test: add to a new `tests/FreelanceManager.Tests/BusinessProfileRepositoryTests.cs`

- [ ] **Step 1: Write the failing test**

`tests/FreelanceManager.Tests/BusinessProfileRepositoryTests.cs`:

```csharp
using System.Threading.Tasks;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class BusinessProfileRepositoryTests
{
    [Fact]
    public async Task Get_creates_default_profile_when_none_exists()
    {
        using var db = new TestDb();
        var profile = await new BusinessProfileRepository(db.NewContext()).GetAsync();
        Assert.NotNull(profile);
        Assert.Equal("USD", profile.DefaultCurrency);
    }

    [Fact]
    public async Task Save_then_Get_round_trips_changes()
    {
        using var db = new TestDb();
        var repo = new BusinessProfileRepository(db.NewContext());
        var p = await repo.GetAsync();
        p.Name = "Christian Design Co";
        p.DefaultTaxRate = 0.2m;
        await new BusinessProfileRepository(db.NewContext()).SaveAsync(p);

        var reloaded = await new BusinessProfileRepository(db.NewContext()).GetAsync();
        Assert.Equal("Christian Design Co", reloaded.Name);
        Assert.Equal(0.2m, reloaded.DefaultTaxRate);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter BusinessProfileRepositoryTests`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement**

`src/FreelanceManager.Data/Repositories/IBusinessProfileRepository.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IBusinessProfileRepository
{
    Task<BusinessProfile> GetAsync();    // creates a default row if missing
    Task SaveAsync(BusinessProfile profile);
}
```

`src/FreelanceManager.Data/Repositories/BusinessProfileRepository.cs`:

```csharp
using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class BusinessProfileRepository : IBusinessProfileRepository
{
    private readonly AppDbContext _db;
    public BusinessProfileRepository(AppDbContext db) => _db = db;

    public async Task<BusinessProfile> GetAsync()
    {
        var profile = await _db.BusinessProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            profile = new BusinessProfile { Id = 1 };
            _db.BusinessProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }
        return profile;
    }

    public async Task SaveAsync(BusinessProfile profile)
    {
        _db.BusinessProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test --filter BusinessProfileRepositoryTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(data): business profile singleton repository"
```

---

## Task 13: Backup service (TDD)

Copies the live SQLite file to a chosen folder with a timestamped name.

**Files:**
- Create: `src/FreelanceManager.Core/Services/IBackupService.cs`, `src/FreelanceManager.Data/BackupService.cs`
- Test: `tests/FreelanceManager.Tests/BackupServiceTests.cs`

- [ ] **Step 1: Write the failing test**

`tests/FreelanceManager.Tests/BackupServiceTests.cs`:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using FreelanceManager.Core.Services;
using FreelanceManager.Data;
using Xunit;

namespace FreelanceManager.Tests;

public class BackupServiceTests
{
    [Fact]
    public async Task Backup_copies_db_file_into_target_folder_with_timestamp()
    {
        string temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(temp);
        string dbPath = Path.Combine(temp, "app.db");
        await File.WriteAllTextAsync(dbPath, "SQLITE");
        string targetDir = Path.Combine(temp, "backups");

        IBackupService svc = new BackupService();
        string backupPath = await svc.BackupAsync(dbPath, targetDir);

        Assert.True(File.Exists(backupPath));
        Assert.StartsWith(targetDir, backupPath);
        Assert.Equal("SQLITE", await File.ReadAllTextAsync(backupPath));

        Directory.Delete(temp, recursive: true);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter BackupServiceTests`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement**

`src/FreelanceManager.Core/Services/IBackupService.cs`:

```csharp
namespace FreelanceManager.Core.Services;

public interface IBackupService
{
    /// <summary>Copies the database file into targetDir, returns the new file path.</summary>
    Task<string> BackupAsync(string databasePath, string targetDir);
}
```

`src/FreelanceManager.Data/BackupService.cs`:

```csharp
using FreelanceManager.Core.Services;

namespace FreelanceManager.Data;

public class BackupService : IBackupService
{
    public async Task<string> BackupAsync(string databasePath, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string name = $"freelance-manager-backup-{stamp}.db";
        string dest = Path.Combine(targetDir, name);

        using var src = File.OpenRead(databasePath);
        using var dst = File.Create(dest);
        await src.CopyToAsync(dst);
        return dest;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test --filter BackupServiceTests`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: timestamped SQLite backup service"
```

---

## Task 14: PDF exporter interface + QuestPDF implementation

QuestPDF is a UI-layer concern (lives in App) because it depends on the rendering package. We test only that it produces a non-empty PDF file; layout is verified manually.

**Files:**
- Create: `src/FreelanceManager.Core/Services/IPdfExporter.cs`, `src/FreelanceManager.App/Pdf/QuestPdfInvoiceExporter.cs`

- [ ] **Step 1: Define the interface in Core**

`src/FreelanceManager.Core/Services/IPdfExporter.cs`:

```csharp
using FreelanceManager.Core.Models;

namespace FreelanceManager.Core.Services;

public interface IPdfExporter
{
    /// <summary>Renders the invoice to a PDF at the given path using the business profile for branding.</summary>
    void ExportInvoice(Invoice invoice, BusinessProfile profile, string outputPath);
}
```

- [ ] **Step 2: Implement with QuestPDF**

`src/FreelanceManager.App/Pdf/QuestPdfInvoiceExporter.cs`:

```csharp
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FreelanceManager.App.Pdf;

public class QuestPdfInvoiceExporter : IPdfExporter
{
    public void ExportInvoice(Invoice invoice, BusinessProfile profile, string outputPath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        decimal subtotal = InvoiceCalculator.Subtotal(invoice);
        decimal tax = InvoiceCalculator.Tax(invoice);
        decimal total = InvoiceCalculator.Total(invoice);
        string cur = invoice.Currency;

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(profile.Name).FontSize(16).Bold();
                        if (!string.IsNullOrWhiteSpace(profile.Address)) col.Item().Text(profile.Address);
                        if (!string.IsNullOrWhiteSpace(profile.Email)) col.Item().Text(profile.Email);
                    });
                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text("INVOICE").FontSize(20).Bold();
                        col.Item().AlignRight().Text(invoice.Number);
                        col.Item().AlignRight().Text($"Issued: {invoice.IssueDate:yyyy-MM-dd}");
                        col.Item().AlignRight().Text($"Due: {invoice.DueDate:yyyy-MM-dd}");
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Text($"Bill to: {invoice.Client?.Name}").Bold();
                    col.Item().PaddingBottom(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Description").Bold();
                            h.Cell().AlignRight().Text("Qty").Bold();
                            h.Cell().AlignRight().Text("Unit").Bold();
                            h.Cell().AlignRight().Text("Amount").Bold();
                        });

                        foreach (var li in invoice.LineItems)
                        {
                            table.Cell().Text(li.Description);
                            table.Cell().AlignRight().Text(li.Quantity.ToString("0.##"));
                            table.Cell().AlignRight().Text($"{cur} {li.UnitPrice:0.00}");
                            table.Cell().AlignRight().Text($"{cur} {li.LineTotal:0.00}");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Text($"Subtotal: {cur} {subtotal:0.00}");
                    col.Item().AlignRight().Text($"Tax ({invoice.TaxRate:P0}): {cur} {tax:0.00}");
                    col.Item().AlignRight().Text($"Total: {cur} {total:0.00}").FontSize(13).Bold();

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                        col.Item().PaddingTop(20).Text(invoice.Notes!);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Thank you for your business.");
                });
            });
        }).GeneratePdf(outputPath);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/FreelanceManager.App`
Expected: `Build succeeded`. (If QuestPDF reports a license error at runtime later, confirm the `LicenseType.Community` line is present.)

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feat(app): QuestPDF invoice exporter"
```

---

## Task 15: App paths + DI configuration

Resolves the per-user app-data folder, builds the runtime DbContext against the real SQLite file, applies migrations on startup, and registers all services + ViewModels.

**Files:**
- Create: `src/FreelanceManager.App/AppPaths.cs`, `src/FreelanceManager.App/ServiceConfiguration.cs`

- [ ] **Step 1: Create `AppPaths`**

`src/FreelanceManager.App/AppPaths.cs`:

```csharp
namespace FreelanceManager.App;

public static class AppPaths
{
    public static string DataDir
    {
        get
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(root, "FreelanceManager");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DatabasePath => Path.Combine(DataDir, "freelance-manager.db");
    public static string DefaultBackupDir => Path.Combine(DataDir, "backups");
}
```

- [ ] **Step 2: Create `ServiceConfiguration`**

`src/FreelanceManager.App/ServiceConfiguration.cs`:

```csharp
using FreelanceManager.App.Pdf;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Services;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App;

public static class ServiceConfiguration
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite($"Data Source={AppPaths.DatabasePath}"),
            ServiceLifetime.Transient);

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IPdfExporter, QuestPdfInvoiceExporter>();

        services.AddTransient<IClientRepository, ClientRepository>();
        services.AddTransient<IProjectRepository, ProjectRepository>();
        services.AddTransient<IInvoiceRepository, InvoiceRepository>();
        services.AddTransient<IBusinessProfileRepository, BusinessProfileRepository>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ClientsViewModel>();
        services.AddTransient<ProjectsViewModel>();
        services.AddTransient<InvoicesViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();

        // apply migrations on startup
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        return provider;
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/FreelanceManager.App`
Expected: build FAILS only because the ViewModels referenced above don't exist yet — that's expected; they're created in the next tasks. If it fails for any *other* reason, fix that. (Do not commit yet.)

> Note for the executor: Tasks 16-23 add the ViewModels and Views referenced here. The App project will not compile cleanly until Task 23. Each of those tasks still commits its own files; the green build checkpoint is at the end of Task 23.

---

## Task 16: ViewModelBase + MainWindow navigation VM

**Files:**
- Create: `src/FreelanceManager.App/ViewModels/ViewModelBase.cs`, `MainWindowViewModel.cs`

- [ ] **Step 1: Create `ViewModelBase`**

`src/FreelanceManager.App/ViewModels/ViewModelBase.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace FreelanceManager.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
}
```

- [ ] **Step 2: Create `MainWindowViewModel`**

`src/FreelanceManager.App/ViewModels/MainWindowViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        ShowDashboard();
    }

    [RelayCommand] private void ShowDashboard() => CurrentPage = _services.GetRequiredService<DashboardViewModel>();
    [RelayCommand] private void ShowClients()   => CurrentPage = _services.GetRequiredService<ClientsViewModel>();
    [RelayCommand] private void ShowProjects()  => CurrentPage = _services.GetRequiredService<ProjectsViewModel>();
    [RelayCommand] private void ShowInvoices()  => CurrentPage = _services.GetRequiredService<InvoicesViewModel>();
    [RelayCommand] private void ShowSettings()  => CurrentPage = _services.GetRequiredService<SettingsViewModel>();
}
```

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feat(app): view-model base and main navigation view-model"
```

---

## Task 17: Clients ViewModels (TDD on edit logic)

`ClientsViewModel` lists/loads/deletes; `ClientEditViewModel` holds form state + validation. We unit-test the edit VM's validation because that's the logic worth protecting.

**Files:**
- Create: `src/FreelanceManager.App/ViewModels/ClientEditViewModel.cs`, `ClientsViewModel.cs`
- Test: `tests/FreelanceManager.Tests/ClientEditViewModelTests.cs`
- Modify: `tests/FreelanceManager.Tests/FreelanceManager.Tests.csproj` — add reference to the App project.

- [ ] **Step 1: Reference the App project from tests**

Run: `dotnet add tests/FreelanceManager.Tests reference src/FreelanceManager.App`
Expected: success. (Lets tests construct ViewModels.)

- [ ] **Step 2: Write the failing tests**

`tests/FreelanceManager.Tests/ClientEditViewModelTests.cs`:

```csharp
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using Xunit;

namespace FreelanceManager.Tests;

public class ClientEditViewModelTests
{
    [Fact]
    public void Is_invalid_when_name_is_blank()
    {
        var vm = new ClientEditViewModel(new Client());
        vm.Name = "   ";
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void Is_invalid_when_email_is_malformed()
    {
        var vm = new ClientEditViewModel(new Client()) { Name = "Acme", Email = "not-an-email" };
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void Is_valid_with_name_and_blank_email()
    {
        var vm = new ClientEditViewModel(new Client()) { Name = "Acme", Email = "" };
        Assert.True(vm.IsValid);
    }

    [Fact]
    public void Is_valid_with_name_and_good_email()
    {
        var vm = new ClientEditViewModel(new Client()) { Name = "Acme", Email = "hi@acme.com" };
        Assert.True(vm.IsValid);
    }

    [Fact]
    public void ToModel_copies_fields_back()
    {
        var model = new Client();
        var vm = new ClientEditViewModel(model) { Name = "Acme", Company = "Acme Inc", Email = "hi@acme.com" };
        vm.ApplyTo(model);
        Assert.Equal("Acme", model.Name);
        Assert.Equal("Acme Inc", model.Company);
        Assert.Equal("hi@acme.com", model.Email);
    }
}
```

- [ ] **Step 3: Run to verify it fails**

Run: `dotnet test --filter ClientEditViewModelTests`
Expected: FAIL — type does not exist.

- [ ] **Step 4: Implement `ClientEditViewModel`**

`src/FreelanceManager.App/ViewModels/ClientEditViewModel.cs`:

```csharp
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.ViewModels;

public partial class ClientEditViewModel : ViewModelBase
{
    private static readonly Regex EmailRx =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public int Id { get; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _company;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _notes;

    public ClientEditViewModel(Client model)
    {
        Id = model.Id;
        _name = model.Name;
        _company = model.Company;
        _email = model.Email;
        _phone = model.Phone;
        _address = model.Address;
        _notes = model.Notes;
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        (string.IsNullOrWhiteSpace(Email) || EmailRx.IsMatch(Email));

    public void ApplyTo(Client model)
    {
        model.Name = Name.Trim();
        model.Company = Company;
        model.Email = Email;
        model.Phone = Phone;
        model.Address = Address;
        model.Notes = Notes;
    }
}
```

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test --filter ClientEditViewModelTests`
Expected: PASS (5 tests).

- [ ] **Step 6: Implement `ClientsViewModel`**

`src/FreelanceManager.App/ViewModels/ClientsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class ClientsViewModel : ViewModelBase
{
    private readonly IClientRepository _repo;

    public ObservableCollection<Client> Clients { get; } = new();

    [ObservableProperty] private Client? _selected;
    [ObservableProperty] private ClientEditViewModel? _editor;
    [ObservableProperty] private string? _statusMessage;

    public ClientsViewModel(IClientRepository repo)
    {
        _repo = repo;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Clients.Clear();
        foreach (var c in await _repo.GetAllAsync()) Clients.Add(c);
    }

    [RelayCommand] private void New() => Editor = new ClientEditViewModel(new Client());

    [RelayCommand]
    private void Edit()
    {
        if (Selected is not null) Editor = new ClientEditViewModel(Selected);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor is null || !Editor.IsValid)
        {
            StatusMessage = "Name is required and email must be valid.";
            return;
        }

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
        StatusMessage = "Saved.";
        await LoadAsync();
    }

    [RelayCommand] private void Cancel() => Editor = null;

    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        try
        {
            await _repo.DeleteAsync(Selected.Id);
            await LoadAsync();
            StatusMessage = "Deleted.";
        }
        catch (ClientInUseException ex)
        {
            StatusMessage = ex.Message;
        }
    }
}
```

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(app): clients list and edit view-models with validation"
```

---

## Task 18: Projects ViewModels

Mirrors clients. No new unit tests (fields are straight data; validation is just "title + client required", covered by a light check). 

**Files:**
- Create: `src/FreelanceManager.App/ViewModels/ProjectEditViewModel.cs`, `ProjectsViewModel.cs`

- [ ] **Step 1: Implement `ProjectEditViewModel`**

`src/FreelanceManager.App/ViewModels/ProjectEditViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.ViewModels;

public partial class ProjectEditViewModel : ViewModelBase
{
    public int Id { get; }

    [ObservableProperty] private int _clientId;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private ProjectStatus _status = ProjectStatus.Lead;
    [ObservableProperty] private string? _repoUrl;
    [ObservableProperty] private string? _liveSiteUrl;
    [ObservableProperty] private string? _hostingNotes;
    [ObservableProperty] private string? _credentialsLocation;
    [ObservableProperty] private string? _buildStackNotes;
    [ObservableProperty] private string? _generalNotes;
    [ObservableProperty] private System.DateTimeOffset? _startDate;
    [ObservableProperty] private System.DateTimeOffset? _dueDate;

    public ProjectStatus[] StatusOptions { get; } =
        (ProjectStatus[])System.Enum.GetValues(typeof(ProjectStatus));

    public ProjectEditViewModel(Project model)
    {
        Id = model.Id;
        _clientId = model.ClientId;
        _title = model.Title;
        _status = model.Status;
        _repoUrl = model.RepoUrl;
        _liveSiteUrl = model.LiveSiteUrl;
        _hostingNotes = model.HostingNotes;
        _credentialsLocation = model.CredentialsLocation;
        _buildStackNotes = model.BuildStackNotes;
        _generalNotes = model.GeneralNotes;
        _startDate = model.StartDate is null ? null : new System.DateTimeOffset(model.StartDate.Value);
        _dueDate = model.DueDate is null ? null : new System.DateTimeOffset(model.DueDate.Value);
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Title) && ClientId > 0;

    public void ApplyTo(Project model)
    {
        model.ClientId = ClientId;
        model.Title = Title.Trim();
        model.Status = Status;
        model.RepoUrl = RepoUrl;
        model.LiveSiteUrl = LiveSiteUrl;
        model.HostingNotes = HostingNotes;
        model.CredentialsLocation = CredentialsLocation;
        model.BuildStackNotes = BuildStackNotes;
        model.GeneralNotes = GeneralNotes;
        model.StartDate = StartDate?.DateTime;
        model.DueDate = DueDate?.DateTime;
    }
}
```

- [ ] **Step 2: Implement `ProjectsViewModel`**

`src/FreelanceManager.App/ViewModels/ProjectsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class ProjectsViewModel : ViewModelBase
{
    private readonly IProjectRepository _projects;
    private readonly IClientRepository _clients;

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<Client> ClientOptions { get; } = new();

    [ObservableProperty] private Project? _selected;
    [ObservableProperty] private ProjectEditViewModel? _editor;
    [ObservableProperty] private Client? _editorClient;
    [ObservableProperty] private string? _statusMessage;

    public ProjectsViewModel(IProjectRepository projects, IClientRepository clients)
    {
        _projects = projects;
        _clients = clients;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Projects.Clear();
        foreach (var p in await _projects.GetAllAsync()) Projects.Add(p);
        ClientOptions.Clear();
        foreach (var c in await _clients.GetAllAsync()) ClientOptions.Add(c);
    }

    [RelayCommand] private void New()
    {
        Editor = new ProjectEditViewModel(new Project());
        EditorClient = null;
    }

    [RelayCommand] private void Edit()
    {
        if (Selected is null) return;
        Editor = new ProjectEditViewModel(Selected);
        EditorClient = null;
        foreach (var c in ClientOptions) if (c.Id == Selected.ClientId) EditorClient = c;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor is null) return;
        if (EditorClient is not null) Editor.ClientId = EditorClient.Id;
        if (!Editor.IsValid) { StatusMessage = "Title and client are required."; return; }

        if (Editor.Id == 0)
        {
            var model = new Project();
            Editor.ApplyTo(model);
            await _projects.AddAsync(model);
        }
        else
        {
            var model = await _projects.GetAsync(Editor.Id);
            if (model is not null) { Editor.ApplyTo(model); await _projects.UpdateAsync(model); }
        }

        Editor = null;
        StatusMessage = "Saved.";
        await LoadAsync();
    }

    [RelayCommand] private void Cancel() => Editor = null;

    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        await _projects.DeleteAsync(Selected.Id);
        await LoadAsync();
        StatusMessage = "Deleted.";
    }
}
```

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feat(app): projects list and edit view-models"
```

---

## Task 19: Invoice line item + invoice edit ViewModels (TDD on totals)

`LineItemViewModel` is observable so the grid recomputes live; `InvoiceEditViewModel` exposes live subtotal/tax/total and produces a `Invoice` model. We unit-test live recomputation.

**Files:**
- Create: `src/FreelanceManager.App/ViewModels/LineItemViewModel.cs`, `InvoiceEditViewModel.cs`
- Test: `tests/FreelanceManager.Tests/InvoiceEditViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/FreelanceManager.Tests/InvoiceEditViewModelTests.cs`:

```csharp
using System.Linq;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceEditViewModelTests
{
    private static InvoiceEditViewModel NewVm()
        => new(new Invoice { TaxRate = 0.2m, Currency = "USD" });

    [Fact]
    public void Totals_start_at_zero()
    {
        var vm = NewVm();
        Assert.Equal(0m, vm.Subtotal);
        Assert.Equal(0m, vm.Total);
    }

    [Fact]
    public void Adding_a_line_updates_subtotal_and_total()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].Description = "Design";
        vm.Lines[0].Quantity = 2m;
        vm.Lines[0].UnitPrice = 100m;

        Assert.Equal(200m, vm.Subtotal);
        Assert.Equal(40m, vm.Tax);
        Assert.Equal(240m, vm.Total);
    }

    [Fact]
    public void Editing_a_line_quantity_recomputes_total()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].UnitPrice = 50m;
        vm.Lines[0].Quantity = 1m;
        Assert.Equal(60m, vm.Total);

        vm.Lines[0].Quantity = 3m;
        Assert.Equal(180m, vm.Total);
    }

    [Fact]
    public void Removing_a_line_recomputes_total()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].Quantity = 1m; vm.Lines[0].UnitPrice = 100m;
        var line = vm.Lines[0];
        vm.RemoveLineCommand.Execute(line);
        Assert.Equal(0m, vm.Total);
    }

    [Fact]
    public void ToModel_includes_lines_and_taxrate()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].Description = "Dev"; vm.Lines[0].Quantity = 1m; vm.Lines[0].UnitPrice = 500m;

        var model = vm.ToModel();
        Assert.Single(model.LineItems);
        Assert.Equal("Dev", model.LineItems.First().Description);
        Assert.Equal(0.2m, model.TaxRate);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter InvoiceEditViewModelTests`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement `LineItemViewModel`**

`src/FreelanceManager.App/ViewModels/LineItemViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.ViewModels;

public partial class LineItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private decimal _quantity = 1m;
    [ObservableProperty] private decimal _unitPrice;

    public decimal LineTotal => Quantity * UnitPrice;

    public LineItemViewModel() { }

    public LineItemViewModel(InvoiceLineItem model)
    {
        _description = model.Description;
        _quantity = model.Quantity;
        _unitPrice = model.UnitPrice;
    }

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));

    public InvoiceLineItem ToModel() => new()
    {
        Description = Description,
        Quantity = Quantity,
        UnitPrice = UnitPrice
    };
}
```

- [ ] **Step 4: Implement `InvoiceEditViewModel`**

`src/FreelanceManager.App/ViewModels/InvoiceEditViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;

namespace FreelanceManager.App.ViewModels;

public partial class InvoiceEditViewModel : ViewModelBase
{
    public int Id { get; }

    [ObservableProperty] private string _number = string.Empty;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private int? _projectId;
    [ObservableProperty] private System.DateTimeOffset _issueDate = System.DateTimeOffset.Now;
    [ObservableProperty] private System.DateTimeOffset _dueDate = System.DateTimeOffset.Now.AddDays(14);
    [ObservableProperty] private InvoiceStatus _status = InvoiceStatus.Draft;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<LineItemViewModel> Lines { get; } = new();

    public InvoiceStatus[] StatusOptions { get; } =
        (InvoiceStatus[])System.Enum.GetValues(typeof(InvoiceStatus));

    public InvoiceEditViewModel(Invoice model)
    {
        Id = model.Id;
        _number = model.Number;
        _clientId = model.ClientId;
        _projectId = model.ProjectId;
        _issueDate = new System.DateTimeOffset(model.IssueDate);
        _dueDate = new System.DateTimeOffset(model.DueDate);
        _status = model.Status;
        _currency = model.Currency;
        _taxRate = model.TaxRate;
        _notes = model.Notes;

        foreach (var li in model.LineItems) AddLineInternal(new LineItemViewModel(li));
        Lines.CollectionChanged += (_, _) => RecalculateTotals();
    }

    public decimal Subtotal => InvoiceCalculator.Subtotal(ToModel());
    public decimal Tax => InvoiceCalculator.Tax(ToModel());
    public decimal Total => InvoiceCalculator.Total(ToModel());

    partial void OnTaxRateChanged(decimal value) => RecalculateTotals();

    private void AddLineInternal(LineItemViewModel line)
    {
        line.PropertyChanged += OnLinePropertyChanged;
        Lines.Add(line);
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand] private void AddLine() => AddLineInternal(new LineItemViewModel());

    [RelayCommand]
    private void RemoveLine(LineItemViewModel? line)
    {
        if (line is null) return;
        line.PropertyChanged -= OnLinePropertyChanged;
        Lines.Remove(line);
        RecalculateTotals();
    }

    public bool IsValid => ClientId > 0 && !string.IsNullOrWhiteSpace(Number);

    public Invoice ToModel() => new()
    {
        Id = Id,
        Number = Number,
        ClientId = ClientId,
        ProjectId = ProjectId,
        IssueDate = IssueDate.DateTime,
        DueDate = DueDate.DateTime,
        Status = Status,
        Currency = Currency,
        TaxRate = TaxRate,
        Notes = Notes,
        LineItems = Lines.Select(l => l.ToModel()).ToList()
    };
}
```

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test --filter InvoiceEditViewModelTests`
Expected: PASS (5 tests).

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(app): invoice edit view-model with live totals"
```

---

## Task 20: InvoicesViewModel (list, create with auto-number, PDF export)

Wires the repositories, number generator, business profile, and PDF exporter. A `Func<Task<string?>>` save-path picker is injected so the VM stays UI-framework-free and testable; the View supplies the real file dialog.

**Files:**
- Create: `src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs`

- [ ] **Step 1: Implement `InvoicesViewModel`**

`src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class InvoicesViewModel : ViewModelBase
{
    private readonly IInvoiceRepository _invoices;
    private readonly IClientRepository _clients;
    private readonly IProjectRepository _projects;
    private readonly IInvoiceNumberGenerator _numbers;
    private readonly IBusinessProfileRepository _profiles;
    private readonly IPdfExporter _pdf;
    private readonly IClock _clock;

    /// <summary>Supplied by the View: returns a chosen output path or null if cancelled.</summary>
    public Func<string, Task<string?>>? SavePdfPathProvider { get; set; }

    public ObservableCollection<InvoiceRow> Invoices { get; } = new();
    public ObservableCollection<Client> ClientOptions { get; } = new();
    public ObservableCollection<Project> ProjectOptions { get; } = new();

    [ObservableProperty] private InvoiceRow? _selected;
    [ObservableProperty] private InvoiceEditViewModel? _editor;
    [ObservableProperty] private Client? _editorClient;
    [ObservableProperty] private string? _statusMessage;

    public InvoicesViewModel(
        IInvoiceRepository invoices, IClientRepository clients, IProjectRepository projects,
        IInvoiceNumberGenerator numbers, IBusinessProfileRepository profiles,
        IPdfExporter pdf, IClock clock)
    {
        _invoices = invoices; _clients = clients; _projects = projects;
        _numbers = numbers; _profiles = profiles; _pdf = pdf; _clock = clock;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Invoices.Clear();
        foreach (var i in await _invoices.GetAllAsync())
            Invoices.Add(new InvoiceRow(i, OverduePolicy.EffectiveStatus(i, _clock.Today)));

        ClientOptions.Clear();
        foreach (var c in await _clients.GetAllAsync()) ClientOptions.Add(c);
        ProjectOptions.Clear();
        foreach (var p in await _projects.GetAllAsync()) ProjectOptions.Add(p);
    }

    [RelayCommand]
    private async Task New()
    {
        var profile = await _profiles.GetAsync();
        int year = _clock.Today.Year;
        int lastSeq = await _invoices.GetMaxSequenceForYearAsync(year);
        string number = _numbers.Next(profile.InvoiceNumberFormat, year, lastSeq);

        Editor = new InvoiceEditViewModel(new Invoice
        {
            Number = number,
            Currency = profile.DefaultCurrency,
            TaxRate = profile.DefaultTaxRate,
            IssueDate = _clock.Today,
            DueDate = _clock.Today.AddDays(14)
        });
        EditorClient = null;
    }

    [RelayCommand]
    private async Task Edit()
    {
        if (Selected is null) return;
        var full = await _invoices.GetAsync(Selected.Id);
        if (full is null) return;
        Editor = new InvoiceEditViewModel(full);
        EditorClient = ClientOptions.FirstOrDefault(c => c.Id == full.ClientId);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor is null) return;
        if (EditorClient is not null) Editor.ClientId = EditorClient.Id;
        if (!Editor.IsValid) { StatusMessage = "Client and invoice number are required."; return; }

        var model = Editor.ToModel();
        if (model.Id == 0) await _invoices.AddAsync(model);
        else await _invoices.UpdateAsync(model);

        Editor = null;
        StatusMessage = "Saved.";
        await LoadAsync();
    }

    [RelayCommand] private void Cancel() => Editor = null;

    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        await _invoices.DeleteAsync(Selected.Id);
        await LoadAsync();
        StatusMessage = "Deleted.";
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (Selected is null || SavePdfPathProvider is null) return;
        var invoice = await _invoices.GetAsync(Selected.Id);
        if (invoice is null) return;

        string? path = await SavePdfPathProvider($"{invoice.Number}.pdf");
        if (string.IsNullOrWhiteSpace(path)) return;

        var profile = await _profiles.GetAsync();
        _pdf.ExportInvoice(invoice, profile, path);
        StatusMessage = $"Exported to {path}";
    }
}

public class InvoiceRow
{
    public InvoiceRow(Invoice inv, InvoiceStatus effectiveStatus)
    {
        Id = inv.Id;
        Number = inv.Number;
        ClientName = inv.Client?.Name ?? "";
        IssueDate = inv.IssueDate;
        DueDate = inv.DueDate;
        Status = effectiveStatus;
        Total = InvoiceCalculator.Total(inv);
        Currency = inv.Currency;
    }

    public int Id { get; }
    public string Number { get; }
    public string ClientName { get; }
    public System.DateTime IssueDate { get; }
    public System.DateTime DueDate { get; }
    public InvoiceStatus Status { get; }
    public decimal Total { get; }
    public string Currency { get; }
}
```

- [ ] **Step 2: Commit**

```powershell
git add -A
git commit -m "feat(app): invoices view-model with auto-numbering and PDF export"
```

---

## Task 21: Dashboard + Settings ViewModels

**Files:**
- Create: `src/FreelanceManager.App/ViewModels/DashboardViewModel.cs`, `SettingsViewModel.cs`

- [ ] **Step 1: Implement `DashboardViewModel`**

`src/FreelanceManager.App/ViewModels/DashboardViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IProjectRepository _projects;
    private readonly IInvoiceRepository _invoices;
    private readonly IClock _clock;

    [ObservableProperty] private int _activeProjects;
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private decimal _outstandingTotal;

    public DashboardViewModel(IProjectRepository projects, IInvoiceRepository invoices, IClock clock)
    {
        _projects = projects;
        _invoices = invoices;
        _clock = clock;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var projects = await _projects.GetAllAsync();
        ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);

        var invoices = await _invoices.GetAllAsync();
        decimal outstanding = 0m;
        int overdue = 0;
        foreach (var i in invoices)
        {
            var eff = OverduePolicy.EffectiveStatus(i, _clock.Today);
            if (eff == InvoiceStatus.Overdue) overdue++;
            if (eff is InvoiceStatus.Sent or InvoiceStatus.Overdue)
                outstanding += InvoiceCalculator.Total(i);
        }
        OverdueCount = overdue;
        OutstandingTotal = outstanding;
    }
}
```

- [ ] **Step 2: Implement `SettingsViewModel`**

`src/FreelanceManager.App/ViewModels/SettingsViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IBusinessProfileRepository _profiles;
    private readonly IBackupService _backup;
    private BusinessProfile _model = new();

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _logoPath;
    [ObservableProperty] private string _defaultCurrency = "USD";
    [ObservableProperty] private decimal _defaultTaxRate;
    [ObservableProperty] private string _invoiceNumberFormat = "INV-{YYYY}-{0000}";
    [ObservableProperty] private string? _statusMessage;

    public SettingsViewModel(IBusinessProfileRepository profiles, IBackupService backup)
    {
        _profiles = profiles;
        _backup = backup;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _model = await _profiles.GetAsync();
        Name = _model.Name;
        Address = _model.Address;
        Email = _model.Email;
        Phone = _model.Phone;
        LogoPath = _model.LogoPath;
        DefaultCurrency = _model.DefaultCurrency;
        DefaultTaxRate = _model.DefaultTaxRate;
        InvoiceNumberFormat = _model.InvoiceNumberFormat;
    }

    [RelayCommand]
    private async Task Save()
    {
        _model.Name = Name;
        _model.Address = Address;
        _model.Email = Email;
        _model.Phone = Phone;
        _model.LogoPath = LogoPath;
        _model.DefaultCurrency = DefaultCurrency;
        _model.DefaultTaxRate = DefaultTaxRate;
        _model.InvoiceNumberFormat = InvoiceNumberFormat;
        await _profiles.SaveAsync(_model);
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private async Task BackupNow()
    {
        try
        {
            string dest = await _backup.BackupAsync(AppPaths.DatabasePath, AppPaths.DefaultBackupDir);
            StatusMessage = $"Backed up to {dest}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
    }
}
```

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feat(app): dashboard and settings view-models"
```

---

## Task 22: Views (XAML) and DataTemplates

Avalonia resolves a ViewModel to its View via a `ViewLocator` (the MVVM template generated one). Each View is a `UserControl`. The MainWindow hosts navigation buttons + a `ContentControl` bound to `CurrentPage`.

**Files:**
- Modify: `src/FreelanceManager.App/Views/MainWindow.axaml`
- Create: `DashboardView`, `ClientsView`, `ProjectsView`, `InvoicesView`, `SettingsView` (`.axaml` + `.axaml.cs`)

> Each `.axaml.cs` is the standard code-behind:
> ```csharp
> using Avalonia.Controls;
> namespace FreelanceManager.App.Views;
> public partial class XxxView : UserControl { public XxxView() => InitializeComponent(); }
> ```
> Replace `XxxView` with the matching class name for each file.

- [ ] **Step 1: Replace `MainWindow.axaml`**

`src/FreelanceManager.App/Views/MainWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:FreelanceManager.App.ViewModels"
        x:Class="FreelanceManager.App.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Width="1100" Height="720"
        Title="Freelance Manager">
  <Grid ColumnDefinitions="200,*">
    <StackPanel Grid.Column="0" Margin="8" Spacing="6">
      <TextBlock Text="Freelance Manager" FontWeight="Bold" Margin="4,8"/>
      <Button Content="Dashboard" HorizontalAlignment="Stretch" Command="{Binding ShowDashboardCommand}"/>
      <Button Content="Clients"   HorizontalAlignment="Stretch" Command="{Binding ShowClientsCommand}"/>
      <Button Content="Projects"  HorizontalAlignment="Stretch" Command="{Binding ShowProjectsCommand}"/>
      <Button Content="Invoices"  HorizontalAlignment="Stretch" Command="{Binding ShowInvoicesCommand}"/>
      <Button Content="Settings"  HorizontalAlignment="Stretch" Command="{Binding ShowSettingsCommand}"/>
    </StackPanel>
    <ContentControl Grid.Column="1" Margin="8" Content="{Binding CurrentPage}"/>
  </Grid>
</Window>
```

- [ ] **Step 2: Create `DashboardView.axaml`**

`src/FreelanceManager.App/Views/DashboardView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             x:Class="FreelanceManager.App.Views.DashboardView"
             x:DataType="vm:DashboardViewModel">
  <StackPanel Spacing="12">
    <TextBlock Text="Dashboard" FontSize="22" FontWeight="Bold"/>
    <WrapPanel>
      <Border Margin="0,0,12,0" Padding="20" Background="#eef" CornerRadius="6">
        <StackPanel>
          <TextBlock Text="Active projects"/>
          <TextBlock Text="{Binding ActiveProjects}" FontSize="28" FontWeight="Bold"/>
        </StackPanel>
      </Border>
      <Border Margin="0,0,12,0" Padding="20" Background="#efe" CornerRadius="6">
        <StackPanel>
          <TextBlock Text="Outstanding"/>
          <TextBlock Text="{Binding OutstandingTotal, StringFormat={}{0:0.00}}" FontSize="28" FontWeight="Bold"/>
        </StackPanel>
      </Border>
      <Border Padding="20" Background="#fee" CornerRadius="6">
        <StackPanel>
          <TextBlock Text="Overdue"/>
          <TextBlock Text="{Binding OverdueCount}" FontSize="28" FontWeight="Bold"/>
        </StackPanel>
      </Border>
    </WrapPanel>
  </StackPanel>
</UserControl>
```

Plus `DashboardView.axaml.cs` (per the code-behind template above, class `DashboardView`).

- [ ] **Step 3: Create `ClientsView.axaml`**

`src/FreelanceManager.App/Views/ClientsView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             x:Class="FreelanceManager.App.Views.ClientsView"
             x:DataType="vm:ClientsViewModel">
  <Grid RowDefinitions="Auto,*,Auto">
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="6" Margin="0,0,0,8">
      <TextBlock Text="Clients" FontSize="22" FontWeight="Bold" VerticalAlignment="Center"/>
      <Button Content="New" Command="{Binding NewCommand}"/>
      <Button Content="Edit" Command="{Binding EditCommand}"/>
      <Button Content="Delete" Command="{Binding DeleteCommand}"/>
    </StackPanel>

    <Grid Grid.Row="1" ColumnDefinitions="*,Auto">
      <DataGrid Grid.Column="0" ItemsSource="{Binding Clients}"
                SelectedItem="{Binding Selected}" IsReadOnly="True"
                AutoGenerateColumns="False">
        <DataGrid.Columns>
          <DataGridTextColumn Header="Name" Binding="{Binding Name}"/>
          <DataGridTextColumn Header="Company" Binding="{Binding Company}"/>
          <DataGridTextColumn Header="Email" Binding="{Binding Email}"/>
        </DataGrid.Columns>
      </DataGrid>

      <Border Grid.Column="1" Width="320" Margin="8,0,0,0" Padding="10"
              Background="#f4f4f4" CornerRadius="6"
              IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}">
        <StackPanel Spacing="6" DataContext="{Binding Editor}">
          <TextBlock Text="Edit client" FontWeight="Bold"/>
          <TextBox Watermark="Name *" Text="{Binding Name}"/>
          <TextBox Watermark="Company" Text="{Binding Company}"/>
          <TextBox Watermark="Email" Text="{Binding Email}"/>
          <TextBox Watermark="Phone" Text="{Binding Phone}"/>
          <TextBox Watermark="Address" Text="{Binding Address}" AcceptsReturn="True" Height="60"/>
          <TextBox Watermark="Notes" Text="{Binding Notes}" AcceptsReturn="True" Height="60"/>
        </StackPanel>
      </Border>
    </Grid>

    <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="6" Margin="0,8,0,0">
      <Button Content="Save" Command="{Binding SaveCommand}"
              IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}"/>
      <Button Content="Cancel" Command="{Binding CancelCommand}"
              IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}"/>
      <TextBlock Text="{Binding StatusMessage}" Foreground="#a00" VerticalAlignment="Center"/>
    </StackPanel>
  </Grid>
</UserControl>
```

Plus `ClientsView.axaml.cs` (class `ClientsView`).

- [ ] **Step 4: Create `ProjectsView.axaml`**

`src/FreelanceManager.App/Views/ProjectsView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             x:Class="FreelanceManager.App.Views.ProjectsView"
             x:DataType="vm:ProjectsViewModel">
  <Grid RowDefinitions="Auto,*,Auto">
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="6" Margin="0,0,0,8">
      <TextBlock Text="Projects" FontSize="22" FontWeight="Bold" VerticalAlignment="Center"/>
      <Button Content="New" Command="{Binding NewCommand}"/>
      <Button Content="Edit" Command="{Binding EditCommand}"/>
      <Button Content="Delete" Command="{Binding DeleteCommand}"/>
    </StackPanel>

    <Grid Grid.Row="1" ColumnDefinitions="*,Auto">
      <DataGrid Grid.Column="0" ItemsSource="{Binding Projects}"
                SelectedItem="{Binding Selected}" IsReadOnly="True"
                AutoGenerateColumns="False">
        <DataGrid.Columns>
          <DataGridTextColumn Header="Title" Binding="{Binding Title}"/>
          <DataGridTextColumn Header="Client" Binding="{Binding Client.Name}"/>
          <DataGridTextColumn Header="Status" Binding="{Binding Status}"/>
          <DataGridTextColumn Header="Due" Binding="{Binding DueDate, StringFormat={}{0:yyyy-MM-dd}}"/>
        </DataGrid.Columns>
      </DataGrid>

      <ScrollViewer Grid.Column="1" Width="360" Margin="8,0,0,0"
                    IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}">
        <Border Padding="10" Background="#f4f4f4" CornerRadius="6">
          <StackPanel Spacing="6">
            <TextBlock Text="Edit project" FontWeight="Bold"/>
            <ComboBox Header="Client *" HorizontalAlignment="Stretch"
                      ItemsSource="{Binding ClientOptions}"
                      SelectedItem="{Binding EditorClient}">
              <ComboBox.ItemTemplate>
                <DataTemplate><TextBlock Text="{Binding Name}"/></DataTemplate>
              </ComboBox.ItemTemplate>
            </ComboBox>
            <StackPanel Spacing="6" DataContext="{Binding Editor}">
              <TextBox Watermark="Title *" Text="{Binding Title}"/>
              <ComboBox Header="Status" HorizontalAlignment="Stretch"
                        ItemsSource="{Binding StatusOptions}" SelectedItem="{Binding Status}"/>
              <TextBox Watermark="Repo URL" Text="{Binding RepoUrl}"/>
              <TextBox Watermark="Live site URL" Text="{Binding LiveSiteUrl}"/>
              <TextBox Watermark="Build stack notes" Text="{Binding BuildStackNotes}" AcceptsReturn="True" Height="50"/>
              <TextBox Watermark="Hosting notes" Text="{Binding HostingNotes}" AcceptsReturn="True" Height="50"/>
              <TextBox Watermark="Where credentials live" Text="{Binding CredentialsLocation}"/>
              <TextBox Watermark="General notes / future instructions" Text="{Binding GeneralNotes}" AcceptsReturn="True" Height="60"/>
              <DatePicker SelectedDate="{Binding StartDate}"/>
              <DatePicker SelectedDate="{Binding DueDate}"/>
            </StackPanel>
            <Button Content="Generate summary (coming soon)" IsEnabled="False"/>
          </StackPanel>
        </Border>
      </ScrollViewer>
    </Grid>

    <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="6" Margin="0,8,0,0">
      <Button Content="Save" Command="{Binding SaveCommand}"
              IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}"/>
      <Button Content="Cancel" Command="{Binding CancelCommand}"
              IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}"/>
      <TextBlock Text="{Binding StatusMessage}" Foreground="#a00" VerticalAlignment="Center"/>
    </StackPanel>
  </Grid>
</UserControl>
```

Plus `ProjectsView.axaml.cs` (class `ProjectsView`).

- [ ] **Step 5: Create `InvoicesView.axaml`**

`src/FreelanceManager.App/Views/InvoicesView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             x:Class="FreelanceManager.App.Views.InvoicesView"
             x:DataType="vm:InvoicesViewModel">
  <Grid RowDefinitions="Auto,*,Auto">
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="6" Margin="0,0,0,8">
      <TextBlock Text="Invoices" FontSize="22" FontWeight="Bold" VerticalAlignment="Center"/>
      <Button Content="New" Command="{Binding NewCommand}"/>
      <Button Content="Edit" Command="{Binding EditCommand}"/>
      <Button Content="Delete" Command="{Binding DeleteCommand}"/>
      <Button Content="Export PDF" Command="{Binding ExportPdfCommand}"/>
    </StackPanel>

    <Grid Grid.Row="1" ColumnDefinitions="*,Auto">
      <DataGrid Grid.Column="0" ItemsSource="{Binding Invoices}"
                SelectedItem="{Binding Selected}" IsReadOnly="True"
                AutoGenerateColumns="False">
        <DataGrid.Columns>
          <DataGridTextColumn Header="Number" Binding="{Binding Number}"/>
          <DataGridTextColumn Header="Client" Binding="{Binding ClientName}"/>
          <DataGridTextColumn Header="Issued" Binding="{Binding IssueDate, StringFormat={}{0:yyyy-MM-dd}}"/>
          <DataGridTextColumn Header="Due" Binding="{Binding DueDate, StringFormat={}{0:yyyy-MM-dd}}"/>
          <DataGridTextColumn Header="Status" Binding="{Binding Status}"/>
          <DataGridTextColumn Header="Total" Binding="{Binding Total, StringFormat={}{0:0.00}}"/>
        </DataGrid.Columns>
      </DataGrid>

      <ScrollViewer Grid.Column="1" Width="420" Margin="8,0,0,0"
                    IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}">
        <Border Padding="10" Background="#f4f4f4" CornerRadius="6">
          <StackPanel Spacing="6">
            <TextBlock Text="Edit invoice" FontWeight="Bold"/>
            <ComboBox Header="Client *" HorizontalAlignment="Stretch"
                      ItemsSource="{Binding ClientOptions}" SelectedItem="{Binding EditorClient}">
              <ComboBox.ItemTemplate>
                <DataTemplate><TextBlock Text="{Binding Name}"/></DataTemplate>
              </ComboBox.ItemTemplate>
            </ComboBox>
            <StackPanel Spacing="6" DataContext="{Binding Editor}">
              <TextBox Watermark="Number" Text="{Binding Number}"/>
              <ComboBox Header="Status" HorizontalAlignment="Stretch"
                        ItemsSource="{Binding StatusOptions}" SelectedItem="{Binding Status}"/>
              <DatePicker SelectedDate="{Binding IssueDate}"/>
              <DatePicker SelectedDate="{Binding DueDate}"/>
              <TextBox Watermark="Tax rate (e.g. 0.2)" Text="{Binding TaxRate}"/>
              <TextBlock Text="Line items" FontWeight="Bold" Margin="0,6,0,0"/>
              <DataGrid ItemsSource="{Binding Lines}" AutoGenerateColumns="False" Height="180"
                        CanUserAddRows="False">
                <DataGrid.Columns>
                  <DataGridTextColumn Header="Description" Binding="{Binding Description}" Width="*"/>
                  <DataGridTextColumn Header="Qty" Binding="{Binding Quantity}"/>
                  <DataGridTextColumn Header="Unit" Binding="{Binding UnitPrice}"/>
                  <DataGridTextColumn Header="Total" Binding="{Binding LineTotal, StringFormat={}{0:0.00}}" IsReadOnly="True"/>
                </DataGrid.Columns>
              </DataGrid>
              <StackPanel Orientation="Horizontal" Spacing="6">
                <Button Content="Add line" Command="{Binding AddLineCommand}"/>
              </StackPanel>
              <TextBlock Text="{Binding Subtotal, StringFormat=Subtotal: {0:0.00}}"/>
              <TextBlock Text="{Binding Tax, StringFormat=Tax: {0:0.00}}"/>
              <TextBlock Text="{Binding Total, StringFormat=Total: {0:0.00}}" FontWeight="Bold"/>
              <TextBox Watermark="Notes" Text="{Binding Notes}" AcceptsReturn="True" Height="50"/>
            </StackPanel>
          </StackPanel>
        </Border>
      </ScrollViewer>
    </Grid>

    <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="6" Margin="0,8,0,0">
      <Button Content="Save" Command="{Binding SaveCommand}"
              IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}"/>
      <Button Content="Cancel" Command="{Binding CancelCommand}"
              IsVisible="{Binding Editor, Converter={x:Static ObjectConverters.IsNotNull}}"/>
      <TextBlock Text="{Binding StatusMessage}" Foreground="#a00" VerticalAlignment="Center"/>
    </StackPanel>
  </Grid>
</UserControl>
```

Plus `InvoicesView.axaml.cs` — this one wires the file-save dialog into the VM:

```csharp
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FreelanceManager.App.ViewModels;

namespace FreelanceManager.App.Views;

public partial class InvoicesView : UserControl
{
    public InvoicesView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is InvoicesViewModel vm)
                vm.SavePdfPathProvider = SavePdfAsync;
        };
    }

    private async System.Threading.Tasks.Task<string?> SavePdfAsync(string suggestedName)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return null;
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            DefaultExtension = "pdf",
            FileTypeChoices = new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } }
        });
        return file?.Path.LocalPath;
    }
}
```

- [ ] **Step 6: Create `SettingsView.axaml`**

`src/FreelanceManager.App/Views/SettingsView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FreelanceManager.App.ViewModels"
             x:Class="FreelanceManager.App.Views.SettingsView"
             x:DataType="vm:SettingsViewModel">
  <ScrollViewer>
    <StackPanel Spacing="8" MaxWidth="480" HorizontalAlignment="Left">
      <TextBlock Text="Settings" FontSize="22" FontWeight="Bold"/>
      <TextBlock Text="Business profile" FontWeight="Bold"/>
      <TextBox Watermark="Business name" Text="{Binding Name}"/>
      <TextBox Watermark="Address" Text="{Binding Address}" AcceptsReturn="True" Height="60"/>
      <TextBox Watermark="Email" Text="{Binding Email}"/>
      <TextBox Watermark="Phone" Text="{Binding Phone}"/>
      <TextBox Watermark="Logo path" Text="{Binding LogoPath}"/>
      <TextBlock Text="Invoice defaults" FontWeight="Bold" Margin="0,8,0,0"/>
      <TextBox Watermark="Default currency (e.g. USD)" Text="{Binding DefaultCurrency}"/>
      <TextBox Watermark="Default tax rate (e.g. 0.2)" Text="{Binding DefaultTaxRate}"/>
      <TextBox Watermark="Invoice number format" Text="{Binding InvoiceNumberFormat}"/>
      <StackPanel Orientation="Horizontal" Spacing="6" Margin="0,8,0,0">
        <Button Content="Save" Command="{Binding SaveCommand}"/>
        <Button Content="Backup now" Command="{Binding BackupNowCommand}"/>
      </StackPanel>
      <TextBlock Text="{Binding StatusMessage}" Foreground="#080"/>
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

Plus `SettingsView.axaml.cs` (class `SettingsView`).

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(app): all views (XAML) and PDF save dialog"
```

---

## Task 23: App startup wiring + green build

Wire DI into Avalonia startup and resolve `MainWindowViewModel` from the container. The MVVM template's `App.axaml.cs` constructs `MainWindowViewModel` directly — replace that with the DI provider.

**Files:**
- Modify: `src/FreelanceManager.App/App.axaml.cs`

- [ ] **Step 1: Replace `App.axaml.cs`**

`src/FreelanceManager.App/App.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FreelanceManager.App.ViewModels;
using FreelanceManager.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ServiceConfiguration.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 2: Confirm the ViewLocator resolves VM→View**

Open `src/FreelanceManager.App/ViewLocator.cs` (generated by the template). It maps a ViewModel type name ending in `ViewModel` to a View type ending in `View` in the `Views` namespace. Our names follow that convention (`DashboardViewModel`→`DashboardView`, etc.), so no change is needed. If the file is missing, create it:

```csharp
using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FreelanceManager.App.ViewModels;

namespace FreelanceManager.App;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;
        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);
        return type is not null ? (Control)Activator.CreateInstance(type)! : new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
```

Ensure `App.axaml` includes the ViewLocator in `<Application.DataTemplates>` (the template adds this; verify it's present):

```xml
<Application.DataTemplates>
  <local:ViewLocator/>
</Application.DataTemplates>
```
(where `xmlns:local="using:FreelanceManager.App"`).

- [ ] **Step 3: Full solution build**

Run: `dotnet build`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test`
Expected: all tests PASS (Tasks 3,4,5,9,10,11,12,13,17,19 — ~35 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(app): wire DI into Avalonia startup"
```

---

## Task 24: Manual smoke test

No automated assertions — verify the wired application end-to-end.

- [ ] **Step 1: Run the app**

Run: `dotnet run --project src/FreelanceManager.App`
Expected: window opens on the Dashboard.

- [ ] **Step 2: Walk the happy path**

Verify each:
- Settings → fill business name, set default tax rate `0.2`, currency `USD`, click **Save** → "Settings saved."
- Clients → **New**, enter a name + valid email, **Save** → appears in the grid. Try a blank name → status shows the validation message.
- Projects → **New**, pick the client, enter a title, fill stack/hosting/credentials notes, **Save** → appears in the grid. Confirm "Generate summary" is disabled.
- Invoices → **New** → number auto-fills (e.g. `INV-2026-0001`), tax rate pre-filled from settings. Pick client, **Add line** ×2, enter quantities/prices → subtotal/tax/total update live. **Save** → appears with correct total.
- Select the invoice → **Export PDF** → choose a path → open the PDF and confirm branding, line items, and totals render.
- Set the invoice's due date in the past and status `Sent`, save → it shows **Overdue** in the list and Dashboard's overdue count increments.
- Settings → **Backup now** → confirm a timestamped `.db` file exists under `%AppData%\FreelanceManager\backups`.
- Delete a client that has a project → status shows the in-use message (deletion blocked).

- [ ] **Step 2: Commit any fixes found during smoke test**

```powershell
git add -A
git commit -m "fix: smoke test corrections"
```

(Skip this commit if nothing needed fixing.)

---

## Self-Review (completed during planning)

- **Spec coverage:** app shell (T16,22,23) ✔ · clients CRUD + delete guard (T9,17) ✔ · projects with handover fields (T10,18,22) ✔ · invoices create/edit/line items/numbering/status/PDF (T5,11,14,19,20,22) ✔ · overdue derived (T4,20,21) ✔ · settings + business profile + backup (T12,13,21,22) ✔ · local SQLite + migrations (T6,7,15) ✔ · dashboard (T21,22) ✔ · deferred summary present-but-disabled (T22) ✔ · testing strategy across Core/Data/VM (T3,4,5,9–13,17,19) ✔.
- **Placeholder scan:** no TBD/TODO; every code step contains complete code; the only intentionally non-compiling checkpoint (end of T15) is called out explicitly with the green-build point at T23.
- **Type consistency:** repository interfaces, `InvoiceCalculator`/`OverduePolicy`/`InvoiceNumberGenerator` signatures, `ApplyTo`/`ToModel` method names, and DI registrations are consistent across tasks and match their call sites.
