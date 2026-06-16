# Freelance Manager

A native, **local-first** Windows desktop application for freelance web designers to manage
clients, projects, and invoices in one place. All data lives on your machine — no cloud
account or server required — and the app works fully offline, reaching the internet only for
opt-in features (e.g. future invoice sending and payments).

> **Status:** Foundation build complete — builds clean, 38/38 tests passing.
> This README is kept continuously up to date as the project evolves.

---

## Tech stack

| Layer | Choice |
|-------|--------|
| Runtime | .NET 10 (current LTS) |
| UI | Avalonia UI 12 (MVVM, native rendering — not a web wrapper) |
| MVVM | CommunityToolkit.Mvvm |
| Persistence | SQLite via EF Core 10 (`IDbContextFactory`, per-operation context) |
| PDF | QuestPDF |
| DI | Microsoft.Extensions.DependencyInjection |
| Tests | xUnit |

Cross-platform by design (Windows first; macOS later with no rewrite).

## Features

**Available now**
- App shell with navigation: Dashboard, Clients, Projects, Invoices, Settings.
- **Clients** — full CRUD; name required, email optional-but-validated; delete blocked while
  the client has projects or invoices.
- **Projects** — CRUD capturing handover details (repo URL, live site, hosting notes,
  credentials location, build-stack notes, general notes, dates) and status
  (Lead → Active → Complete → Archived).
- **Invoices** — create/edit, optional link to a project, line items with live
  subtotal/tax/total, auto invoice numbering from a configurable format, statuses
  (Draft/Sent/Paid) with **derived Overdue**, and branded **PDF export**.
- **Settings** — business profile (name, address, logo, email), default currency, default tax
  rate, invoice-number format, and a one-click timestamped **Backup**.
- **Dashboard** — active project count, outstanding invoice total, overdue count.

**Deferred (data captured now; engines later)**
- Invoice email sending.
- Payment integration.
- Project end-of-project **auto-scrape summary** (generate a customer handover document from
  the GitHub repo / live site). The "Generate summary" button is present but disabled.

## Project structure

```
FreelanceManager.slnx
├─ src/
│  ├─ FreelanceManager.Core/   # domain models, business logic, service interfaces (no UI/DB)
│  ├─ FreelanceManager.Data/   # EF Core + SQLite: DbContext, migrations, repositories, backup
│  └─ FreelanceManager.App/    # Avalonia UI: Views (.axaml) + ViewModels (MVVM), DI, PDF export
└─ tests/
   └─ FreelanceManager.Tests/  # xUnit: Core logic, repositories (real SQLite), ViewModels
docs/superpowers/              # specs/ (design) and plans/ (implementation plan)
```

## Getting started

**Prerequisite:** the **.NET 10 SDK (x64)**. Verify with:

```powershell
dotnet --version   # should print 10.x
```

> If you have an x86 .NET host earlier on your PATH, `dotnet` may report "No .NET SDKs
> were found." Ensure `C:\Program Files\dotnet` (x64) resolves first, or prefix commands with
> `$env:Path = 'C:\Program Files\dotnet;' + $env:Path;`.

**Build, test, run:**

```powershell
dotnet build                              # build the solution
dotnet test                               # run all tests
dotnet run --project src/FreelanceManager.App   # launch the app
```

The database and backups are created under `%AppData%\FreelanceManager\`
(`freelance-manager.db`, `backups/`). EF Core migrations are applied automatically on startup.

## Branching & workflow

- **`master`** — production/release line.
- **`staging`** — default integration branch for ongoing development.
- **feature branches** — branch off `staging`, open a PR back into `staging`.
- Promote `staging` → `master` for releases.

## Documentation

- Design spec: [`docs/superpowers/specs/2026-06-16-freelance-manager-foundation-design.md`](docs/superpowers/specs/2026-06-16-freelance-manager-foundation-design.md)
- Implementation plan: [`docs/superpowers/plans/2026-06-16-freelance-manager-foundation.md`](docs/superpowers/plans/2026-06-16-freelance-manager-foundation.md)

## Roadmap

1. Invoice email sending (SMTP + PDF attachment).
2. Payment tracking, then online payment providers.
3. Project auto-scrape summary / customer handover document.
