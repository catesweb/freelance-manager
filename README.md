
<p align="center"> <a href="https://git.io/typing-svg"><img src="https://readme-typing-svg.demolab.com?font=Fira+Code&size=30&pause=1000&color=4262F7&background=4EC6FF00&center=true&vCenter=true&width=435&lines=Freelance+Manager" alt="Typing SVG" /></a></p>

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Avalonia UI](https://img.shields.io/badge/Avalonia%20UI-12-8B44AC)](https://avaloniaui.net/)
[![SQLite](https://img.shields.io/badge/SQLite-EF%20Core%2010-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![CI (master)](https://img.shields.io/github/actions/workflow/status/catesweb/freelance-manager/ci.yml?branch=master&label=build%20%28master%29)](https://github.com/catesweb/freelance-manager/actions/workflows/ci.yml?query=branch%3Amaster)
[![CI (staging)](https://img.shields.io/github/actions/workflow/status/catesweb/freelance-manager/ci.yml?branch=staging&label=build%20%28staging%29)](https://github.com/catesweb/freelance-manager/actions/workflows/ci.yml?query=branch%3Astaging)
[![Last commit](https://img.shields.io/github/last-commit/catesweb/freelance-manager/staging)](https://github.com/catesweb/freelance-manager/commits/staging)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)](#)
[![License](https://img.shields.io/github/license/catesweb/freelance-manager)](LICENSE)

A native, **local-first** Windows desktop application for freelance web designers to manage
clients, projects, and invoices in one place. All data lives on your machine — no cloud
account or server required — and the app works fully offline, reaching the internet only for
opt-in features (SMTP invoice email and update checks).

> **Status:** Actively developed — builds clean, 78/78 tests passing.
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
  (Lead → Active → Complete → Archived), **editable inline** from the list by clicking the
  status pill.
- **Invoices** — create/edit, optional link to a project, line items with live
  subtotal/tax/total, auto invoice numbering from a configurable format, statuses
  (Draft/Sent/Paid) with **derived Overdue** — settable inline from the list pill — and
  branded **PDF export**.
- **Payments** — record partial/full payments per invoice (auto-marks Paid when covered),
  running balance shown in the editor.
- **Email** — send an invoice (PDF attached) over your own SMTP, and one-click
  **payment reminders** for sent/overdue invoices that quote the outstanding balance.
- **Dashboard** — a **"Needs attention" inbox** (overdue invoices, items due within a week,
  stale leads) where every row opens the exact record, a this-week agenda, active-project
  shortcuts, key totals, and a welcome checklist with **one-click sample data** for a
  fresh install.
- **Search** — filter clients, projects, and invoices lists as you type.
- **Settings** — business profile (name, address, logo, email), default currency, default tax
  rate, invoice-number format, SMTP config with a **connection test**, System/Light/Dark
  theme (applies instantly), and a one-click timestamped **Backup**.
- **In-app updates** — the installed app checks GitHub Releases and updates itself.
- **Themed toast notifications** — success/error feedback styled to the app's design tokens
  (light/dark aware).

**Deferred (data captured now; engines later)**
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
docs/                          # specs/ (design) and plans/ (implementation plan)
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
Set the `FREELANCEMANAGER_DATA_DIR` environment variable to point the app at a different
data directory (useful for testing against a scratch database).

## Releases & updates

Distribution is a **Velopack** installer (per-user managed folders), not a loose
self-contained exe. Pushing to `master` runs [`release.yml`](.github/workflows/release.yml):
if `<VersionPrefix>` in [`Directory.Build.props`](Directory.Build.props) has no matching
`vX.Y.Z` tag yet, the workflow publishes, packs, tags, and uploads the release to GitHub
Releases; otherwise the push is a no-op. **Bumping the version is what cuts a release.**

The installed app checks GitHub Releases for newer versions: silently on startup
(notifies only if one is waiting) and on demand via **Settings → Check for updates**,
which downloads and restarts into the new version. Both are no-ops in a dev/unpackaged
run, so updates only work once installed from a release.

## Branching & workflow

- **`master`** — production/release line.
- **`staging`** — default integration branch for ongoing development.
- **feature branches** — branch off `staging`, open a PR back into `staging`.
- Promote `staging` → `master` for releases.

## Documentation

- Foundation design spec: [`docs/specs/2026-06-16-freelance-manager-foundation-design.md`](docs/specs/2026-06-16-freelance-manager-foundation-design.md)
- Foundation implementation plan: [`docs/plans/2026-06-16-freelance-manager-foundation.md`](docs/plans/2026-06-16-freelance-manager-foundation.md)
- Latest design spec: [`docs/specs/2026-07-12-dark-mode-attention-inbox-design.md`](docs/specs/2026-07-12-dark-mode-attention-inbox-design.md)

## Roadmap

1. Proposals / estimates that convert into a project and first invoice.
2. Time tracking and per-client profitability.
3. Recurring invoices.
4. Online payment providers (extends manual payment tracking).
5. Project auto-scrape summary / customer handover document.

## License

Released under the [MIT License](LICENSE).

## Designed By
[Created & Maintained by CATESWEB.COM](https://catesweb.com)

<a href="https://hits.sh/github.com/catesweb/"><img alt="Hits" src="https://hits.sh/github.com/catesweb.svg?style=plastic&label=repo%20views&color=26c210&labelColor=000000"/></a>
