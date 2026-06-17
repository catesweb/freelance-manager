# Freelance Manager — Foundation + Thin Slices (Design)

**Date:** 2026-06-16
**Status:** Approved design, pre-implementation
**Scope:** First build of a larger application. Establishes the shared foundation plus
minimal working slices of invoicing and project tracking. Later builds deepen each area
and add the deferred engines (invoice sending, payments, project auto-scrape summary).

---

## 1. Vision & Constraints

A native desktop application for a freelance web designer to manage clients, projects, and
invoices in one place.

**Constraints (decided during brainstorming):**

- **Local-first, online when needed.** All data lives on the user's machine; no cloud
  account or server is required and the app fully functions offline. It may reach the
  internet on demand for future features (sending invoices, payments, fetching repo/site
  info).
- **Truly native — no web technologies, no web wrapper.** Not a browser tab, not Electron.
- **Windows first, macOS later with no rewrite.**
- **Single user, no authentication.** Personal app on the user's own machine.

**Chosen stack:** .NET 10 (current LTS, GA Nov 2025) + **Avalonia UI** (native cross-platform rendering engine),
**SQLite** via **EF Core**, **MVVM** via CommunityToolkit.Mvvm, **QuestPDF** for document
generation, Microsoft.Extensions.DependencyInjection for wiring.

Rationale: Avalonia is the only evaluated option satisfying all three platform constraints
(native, Windows-first, Mac-without-rewrite) while offering first-class libraries for every
item on the roadmap (PDF, email, payments, HTTP scraping).

---

## 2. Scope of This Build

### In scope
- **App shell:** native window with left-nav — Dashboard, Clients, Projects, Invoices,
  Settings.
- **Clients:** full CRUD. Shared by both invoicing and projects.
- **Projects (thin):** CRUD capturing all fields the future auto-scrape summary will need.
- **Invoices (thin):** create/edit/save, optional project link, line items, auto numbering,
  tax/total calculation, status, PDF export.
- **Settings:** business profile (name, address, logo, email), default currency, default tax
  rate, invoice-number format, backup location.
- **Local storage:** single SQLite file in the app-data folder + manual backup/export.

### Explicitly deferred (foundation accommodates them; data captured now)
- Invoice **sending** (email).
- **Payment** integration.
- Project **auto-scrape summary generator** (deterministic parsing of the GitHub repo / live
  site into a customer-facing handover document — no AI).

### Assumptions
- Single user, no login.
- One default currency at a time.
- Tax is a single configurable rate per invoice, overridable per invoice.

---

## 3. Architecture

Layered separation so logic is testable in isolation from the UI.

```
FreelanceManager.sln
├─ FreelanceManager.Core    // domain models, business logic, service interfaces — no UI/DB
│   ├─ Models/              // Client, Project, Invoice, InvoiceLineItem, BusinessProfile, enums
│   └─ Services/            // interfaces: IInvoiceNumberGenerator, IPdfExporter, IBackupService...
├─ FreelanceManager.Data    // EF Core + SQLite: DbContext, migrations, repositories
├─ FreelanceManager.App     // Avalonia UI: Views (.axaml) + ViewModels (MVVM), DI wiring
└─ FreelanceManager.Tests   // unit tests (Core/Data) + ViewModel tests
```

- **MVVM:** Views are declarative XAML; ViewModels hold state and commands; no business logic
  in code-behind.
- **DI** wires services and the DbContext into ViewModels.
- **Business logic lives in Core/Data**, never in the UI — invoice math, numbering, overdue
  derivation, and PDF generation are all unit-testable without launching the app.
- **QuestPDF** generates invoice PDFs (and, later, summary PDFs) directly from domain models.

---

## 4. Data Model

```
BusinessProfile (singleton)
  name, address, email, phone, logoPath,
  defaultCurrency, defaultTaxRate, invoiceNumberFormat

Client
  id, name, company, email, phone, address, notes, createdAt
  → has many Projects, has many Invoices

Project
  id, clientId, title, status (Lead|Active|Complete|Archived),
  repoUrl, liveSiteUrl, hostingNotes, credentialsLocation,
  buildStackNotes, generalNotes, startDate, dueDate, createdAt
  → has many Invoices

Invoice
  id, number, clientId, projectId? (nullable = standalone),
  issueDate, dueDate, status (Draft|Sent|Paid|Overdue),
  currency, taxRate, notes
  → has many InvoiceLineItem

InvoiceLineItem
  id, invoiceId, description, quantity, unitPrice
  (lineTotal computed; invoice subtotal/tax/total computed)
```

**Rules:**
- `Invoice.projectId` is **nullable** — invoices usually belong to a project but standalone
  is allowed.
- Deleting a client is **blocked** if they have projects or invoices (no orphans), with a
  clear message.
- Money stored as **`decimal`**, never float. Rounding applied once, at display/PDF time.
- **Overdue is derived** (due date passed + not Paid), not a manually set state.

---

## 5. Feature Behavior (Thin Slices)

**Clients** — list + add/edit form. Validation: name required; email optional but must be a
valid address if provided. Delete blocked when projects/invoices exist.

**Projects** — list filterable by status; add/edit form with all Section 4 fields; status via
dropdown; project detail shows linked invoices. The "Generate summary" action is present but
disabled ("coming soon") in this build.

**Invoices** — most logic-heavy slice:
- Create from scratch, or pre-filled from a project (auto-pulls the client).
- Add/remove/reorder line items with live subtotal → tax → total recalculation.
- **Numbering:** auto-generated from the Settings format (e.g. `INV-{YYYY}-{0001}`),
  sequential, no duplicates.
- **Statuses:** Draft → Sent → Paid set manually; **Overdue derived**. "Sent" is only a flag
  here — actual emailing is a later build.
- **PDF export:** branded invoice PDF (business profile + logo, client, line items, totals)
  to a user-chosen location.

**Dashboard** — counts of active projects, outstanding invoice total, overdue invoices,
recent activity.

**Settings** — edit business profile, logo, currency, default tax rate, invoice-number
format; **Backup now** copies the SQLite file to a chosen folder, timestamped.

---

## 6. Error Handling & Testing

- **Validation** at the ViewModel boundary; friendly inline messages; never crash on bad
  input.
- **DB failures** surfaced as readable dialogs; writes wrapped so a failure doesn't corrupt
  state.
- **Money** as `decimal` throughout; rounding applied once at display/PDF time.
- **Migrations:** EF Core migrations so the schema can evolve as deferred features arrive.
- **Testing:**
  - Core unit tests: invoice math, numbering, overdue derivation.
  - Data tests: repositories against an in-memory/temp SQLite database.
  - ViewModel tests: command and validation logic.
  - UI kept thin so most logic is covered without driving the GUI.

---

## 7. Roadmap (Beyond This Build)

1. **Invoice sending** — SMTP/email with PDF attachment; "Sent" becomes a real action.
2. **Payment integration** — record/track payments; later online providers (e.g. Stripe).
3. **Project auto-scrape summary** — fetch GitHub repo / live site, deterministically parse
   stack, dependencies, and structure, merge with manual fields, export a customer handover
   document (stack, hosting, credentials location, future instructions).
