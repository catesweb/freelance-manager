# Freelance Manager — UI/UX Refresh Design

**Date:** 2026-06-19
**Status:** Approved (design); pending implementation plan
**Scope:** UI/UX refresh of existing features. No new backend, data model, or
deferred-feature work. Targets the Avalonia app (`FreelanceManager.App`) only.

## Goal

Move the app from a clean-but-generic SaaS-blue look to a **calm, professional,
sellable** product. The bones work; this refresh sharpens visual identity,
makes the home screen actionable, unifies how records are edited, and adds the
first-impression polish a paying user expects.

Driving constraints (from brainstorm):
- **Audience:** a product to sell/share — first impressions, onboarding, and
  empty states matter.
- **Personality:** calm & professional (Linear/Things territory), not bold or
  playful.
- **No rewrite:** re-tune the existing design-token system and restructure a few
  views; keep MVVM structure, DI, and the light/dark token architecture.

## 1. Visual system

A **graphite-neutral base warmed toward "Calm Air"**, with a single restrained
indigo accent.

- **Accent:** indigo `#5E6AD2` (replaces the current `#4F6BED` blue), used
  sparingly — active nav, primary buttons, key figures, focus rings. Not on
  every status pill.
- **Neutrals:** near-monochrome grays with a hint of warm off-white in surfaces
  (slightly warmer than pure Linear). Hairline borders.
- **Numbers:** tabular numerals (`font-variant-numeric: tabular-nums`) for all
  money/counts so columns align and don't jitter.
- **Corners:** tighten toward ~6px (`RadiusSm`) as the default; keep larger radii
  only where a surface needs to feel soft.
- **Spacing:** a touch more breathing room than Linear's density — keep the
  existing `Space*` scale, lean on the larger steps.
- **Implementation:** this is a **re-tune of `Themes/Tokens.axaml`** (Light +
  Dark dictionaries) plus `Typography.axaml` / `Controls.axaml`, not a new
  system. Both light and dark variants must be updated together.

**Acceptance:** existing screens render under the new tokens in both light and
dark with no hardcoded colors left behind; accent appears only on intentional
elements.

## 2. Shell & navigation

Keep the 220px sidebar and its five destinations. Add a **persistent
quick-create** affordance so creating records isn't buried inside each page.

- A `＋ New` control (button with a small menu: New invoice / New project /
  New client) reachable from the shell regardless of the current page.
- Selecting an item navigates to the relevant editor in the new master-detail
  layout (§4) with a fresh record.

**Acceptance:** quick-create is reachable from every page and creates the
correct record type.

## 3. Home screen — Agenda

Replace the read-only metrics board with a **time-first agenda** (brainstorm
option C).

- **Layout:** two columns.
  - **Left — This week:** a chronological list grouped by day, showing
    **project deadlines** and **invoice due dates** drawn from existing data
    (project dates; invoice due/issue dates). Each entry links to its record.
  - **Right — Quick actions + Pinned projects:** quick-create buttons and a
    short list of active/pinned projects.
- **Data:** read-only composition of existing fields. No new persisted data
  required for the agenda itself. *(Open: "pinned" may need a single boolean on
  Project — see Open Questions.)*
- The metrics (active projects, outstanding total, overdue count) are **not
  deleted** — fold them into a compact header strip above the agenda so the
  pulse is still one glance away.

**Acceptance:** opening the app shows upcoming deadlines/due dates for the
current week from real data; quick actions create records; empty week shows an
inviting empty state (§5).

## 4. Records — Master–detail split

Apply one consistent pattern to **Clients, Projects, and Invoices**, replacing
the current inconsistency (Clients use a dialog; Projects/Invoices take the
whole page).

- **Layout:** list pane (~40%) on the left, live detail/editor pane on the right.
  Selecting a row updates the right pane in place; the list never disappears.
- **Create:** quick-create or a `＋ New` button in the list header opens a blank
  detail pane.
- **Inline status editing** (project status pill, invoice status pill) is
  preserved — it now lives in the detail pane and/or the list row.
- **Migration notes:**
  - `ClientEditDialog` → folded into the Clients master-detail detail pane;
    retire the dialog for the standard edit flow (confirm-delete dialog stays).
  - `ProjectEditView` / `InvoiceEditView` → become the detail pane content
    rather than full-page navigations.
  - Delete-blocking rules (client with projects/invoices) and validation are
    unchanged — surfaced inline in the detail pane.

**Acceptance:** all three entities browse and edit through the same split
layout; selecting records never loses list position; existing validation,
delete-blocking, and inline status editing still work.

## 5. Polish set

All four, since they collectively signal "premium" to a buyer.

### 5.1 Calmer status pills
Replace filled solid-color pills with a **soft tint + colored dot**. Semantics
unchanged: grey = draft, amber = sent, green = paid, red = overdue, plus project
statuses (Lead/Active/Complete/Archived). Quieter in long lists, still scannable.
Touches `Controls/StatusBadge.axaml` and `Converters/StatusToBrushConverter.cs`.

### 5.2 Inviting empty states
Every primary screen's empty view becomes an icon + one-line explanation + a
primary action ("No clients yet → ＋ Add a client"). Extend the existing
`Controls/EmptyState.axaml` and wire it into Clients, Projects, Invoices, and the
agenda.

### 5.3 First-run setup checklist
On first launch, a short guided flow: business profile + logo → currency/tax
defaults → add first client → create first invoice. A **dismissible progress
strip** on the dashboard tracks completion until done or dismissed.
- **State:** a small persisted flag set (steps completed / onboarding dismissed)
  — see Open Questions for where it lives.
- Reuses existing Settings business-profile fields; doesn't duplicate them.

### 5.4 Cleaner invoice line-item editor
Re-lay the busiest form as an aligned grid (description · qty · rate · amount)
with inline add/remove and a right-aligned **live subtotal / tax / total** block.
Builds on existing `InvoiceEditViewModel` / `LineItemViewModel` (live totals
already exist) — this is layout/visual, not new calculation logic.

**Acceptance:** pills render as tint+dot in both themes; empty states appear when
lists are empty with a working primary action; first-run checklist appears on a
fresh install and can be completed/dismissed; line-item editor totals update live
as before with the new layout.

## Out of scope

- Deferred features: invoice email sending, payment integration, project
  auto-scrape handover summary. Untouched.
- Data model changes beyond the two small flags noted in Open Questions.
- macOS-specific work (cross-platform structure preserved; not a target now).

## Open questions (resolve during planning)

1. **Pinned projects:** add a `bool IsPinned` to the Project model, or derive the
   right-column list from "active, most recently updated"? (Leaning derived to
   avoid a schema change.)
2. **Onboarding state:** store completion flags in app settings
   (`%AppData%\FreelanceManager\`) vs. the SQLite DB. (Leaning settings/app file.)
3. **Master-detail on narrow widths:** the window minimum is comfortably wide,
   but confirm behavior if the user shrinks it — collapse detail to overlay, or
   set a sensible min width?

## Affected files (indicative, not exhaustive)

- `Themes/Tokens.axaml`, `Themes/Typography.axaml`, `Themes/Controls.axaml`
- `Views/MainWindow.axaml` (+ VM) — quick-create
- `Views/DashboardView.axaml` (+ `DashboardViewModel`) — agenda
- `Views/ClientsView`, `ProjectsView`, `InvoicesView` (+ VMs) — master-detail
- `Views/ProjectEditView`, `InvoiceEditView`, `Dialogs/ClientEditDialog` — fold
  into detail panes
- `Controls/StatusBadge.axaml`, `Controls/EmptyState.axaml`,
  `Converters/StatusToBrushConverter.cs`
- New: onboarding checklist view/VM + first-run state
