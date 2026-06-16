# Design System & Shell Foundation (Design)

**Date:** 2026-06-16
**Status:** Approved design, pre-implementation
**Scope:** Second build of Freelance Manager. Establishes the visual foundation — a
token-based theme system (light + dark), reusable component styles, a redesigned app shell,
and a hybrid editor framework — then retrofits all existing views onto it. No new product
features; this is the design layer every future feature will render into.

**Builds on:** [2026-06-16-freelance-manager-foundation-design.md](2026-06-16-freelance-manager-foundation-design.md)

---

## 1. Vision & Rationale

The foundation build is functionally complete but visually unstyled: it runs on Avalonia's
default FluentTheme with hardcoded hex colors in the views, plain stacked-button navigation,
watermark-only inputs, raw enum text for statuses, and editing crammed into a narrow docked
panel. Before adding features (time tracking, dashboards, charts) we establish the visual
language **once**, so every later feature inherits a consistent, themeable, professional look
instead of being restyled later.

**Chosen visual direction:** "Calm Professional" — neutral slate greys, generous whitespace,
a single confident blue accent. The trusted SaaS-dashboard look (Linear / Stripe), built to
age well.

**Theming requirement:** true light + dark, built now (not deferred). Theme follows the OS by
default and flips live when the OS changes, with a manual override (System / Light / Dark)
that wins when set. Building both now is only marginally more work than light-only because the
core task — replacing hardcoded hex with named tokens — must happen regardless; a dark palette
is then just a second set of values for the same token names.

---

## 2. Scope of This Build

### In scope
- **Theme tokens:** central resource dictionaries with light + dark values for color, spacing,
  typography, radii, and elevation.
- **Reusable component styles:** buttons (primary/secondary/ghost/danger), labeled text inputs,
  card/surface, section header, status badge/pill, empty-state component.
- **Icons:** add `Projektanker.Icons.Avalonia` with the Lucide set for sidebar + buttons.
- **App shell redesign:** branded sidebar with icon nav + active-state highlight; a reusable
  page-header region (title + primary action).
- **Editor framework (hybrid):**
  - Full-page editor host for field-heavy records (Projects, Invoices).
  - Modal dialog service (DI-injected) for short forms (Clients) and confirmation dialogs.
  - Inline validation + toast/inline notifications, replacing the bottom red-text line.
- **Retrofit:** migrate all five existing views (Dashboard, Clients, Projects, Invoices,
  Settings) onto the new system. No half-styled screens.
- **Theme switcher:** Settings gains a System / Light / Dark selector; choice persists.

### Explicitly deferred (later slices)
- Search / filter / sort on list pages.
- Dashboard charts and richer reporting.
- Any new product features (time tracking, expenses, recurring invoices, the three engines
  already deferred in the foundation spec).

### Non-goals
- No change to the domain model, business logic, or persistence beyond persisting the theme
  preference.
- No change to invoice math, numbering, overdue derivation, or PDF export behavior.

---

## 3. Architecture

All work lands in `FreelanceManager.App` (plus tests). Core/Data are untouched except for one
new persisted setting.

```
FreelanceManager.App
├─ Themes/
│   ├─ Tokens.axaml          // ThemeDictionaries: Light + Dark values for every named token
│   ├─ Controls.axaml        // ControlThemes/Styles: buttons, inputs, cards, badges, empty-state
│   └─ Typography.axaml      // text styles: PageTitle, SectionHeading, Body, Caption/Label
├─ Controls/                 // small custom controls where a style isn't enough
│   ├─ StatusBadge.axaml(.cs)
│   ├─ EmptyState.axaml(.cs)
│   └─ PageHeader.axaml(.cs)
├─ Services/
│   ├─ IDialogService.cs     // modal forms + confirmations; testable interface
│   ├─ DialogService.cs
│   ├─ INotificationService.cs  // toast / inline success+error
│   ├─ NotificationService.cs
│   ├─ IThemeService.cs      // applies System/Light/Dark, listens to OS changes
│   └─ ThemeService.cs
├─ Views/  (existing five, migrated; Projects/Invoices gain full-page editor views)
└─ ViewModels/  (existing, refactored to use dialog/notification services)
```

- **Tokens** are referenced everywhere via `{DynamicResource TokenName}` so theme switches apply
  live without restarts.
- **Component styles** mean views compose styled primitives, not per-view inline styling.
- **Services** (dialog, notification, theme) are interfaces wired through the existing
  Microsoft.Extensions.DependencyInjection setup, keeping ViewModels testable.

---

## 4. Design Tokens

Token names are stable; only their values differ per variant.

**Color** — `BgCanvas`, `BgSurface`, `BgSurfaceAlt`, `Border`, `TextPrimary`, `TextMuted`,
`AccentPrimary`, `AccentPrimaryHover`, and semantic `Success`, `Warning`, `Danger`, `Info`.

**Spacing** — a named scale: `4, 8, 12, 16, 24, 32`.

**Typography** — `PageTitle`, `SectionHeading`, `Body`, `Caption`/`Label`, one font family.

**Radii & elevation** — corner-radius tokens and shadow tokens for cards/surfaces/modals.

All existing hardcoded hex values (e.g. `#eef`, `#efe`, `#fee`, `#f4f4f4`, `#a00`) are removed
and replaced with the appropriate semantic token.

---

## 5. Component & Shell Behavior

**Status badge** — maps invoice statuses (Draft / Sent / Paid / Overdue) and project statuses
(Lead / Active / Complete / Archived) to semantic colors. Overdue uses `Danger`; the mapping is
data-driven so it reads from the existing derived status, not a new field.

**Empty state** — shown when a list is empty: short message + primary "New …" action.

**Buttons** — `primary` (accent), `secondary` (neutral surface), `ghost` (text-only),
`danger` (destructive, e.g. Delete).

**Labeled inputs** — label-above-field replaces watermark-only inputs; validation errors show
inline beneath the field.

**Sidebar** — branding at top, nav items with Lucide icons and an active-state highlight bound
to the existing `Show…Command`s on `MainWindowViewModel`. No behavior change to navigation.

**Page header** — reused title + primary-action region across pages.

**Editor framework:**
- *Full-page (Projects, Invoices):* "New" / selecting a row navigates the content area to a
  roomy multi-column, sectioned form; a breadcrumb/back returns to the list. Replaces the
  docked side panel. The existing edit ViewModels are reused; only their host changes.
- *Modal (Clients, confirmations):* `IDialogService` opens a focused dialog over a dimmed
  backdrop for short forms, and a confirm dialog for destructive actions (e.g. delete client —
  which already blocks when projects/invoices exist; the dialog surfaces that message cleanly).
- *Notifications:* `INotificationService` shows transient success and error feedback, replacing
  the bottom `StatusMessage` red text.

**Theme switcher (Settings)** — System / Light / Dark. `System` follows the OS variant and
updates live; a manual choice overrides until changed. The selection persists (see §6).

---

## 6. Data & Persistence

One new persisted value: the **theme preference** (`System` | `Light` | `Dark`). Stored on the
existing `BusinessProfile` singleton (a new column via an EF Core migration), consistent with how
other app-wide settings are stored. On startup, `ThemeService` reads it and applies the variant;
`System` subscribes to OS theme-change notifications.

No other schema or model changes.

---

## 7. Error Handling & Testing

- **Live theming:** `DynamicResource` everywhere so switching variant never requires a restart
  and never leaves a half-themed screen.
- **Dialog/notification services** are interfaces with unit-testable ViewModels; tests assert
  that, e.g., a blocked client delete raises the expected confirmation/error path without driving
  the GUI.
- **Theme persistence** is unit-tested: saving a preference writes it; startup reads and applies
  the correct variant; `System` resolves to the OS variant.
- **Regression:** all existing Core/Data/ViewModel tests continue to pass through the
  editor-host refactor; edit ViewModels keep their current contracts.
- **Visual verification:** styles and layout are checked by running the app in both light and
  dark; this is explicitly a manual step, not automated.

---

## 8. Build Sequence (high level)

1. Token dictionaries (light + dark) + typography.
2. Component styles + custom controls (badge, empty-state, page-header).
3. App shell redesign (sidebar, icons, page header).
4. Theme service + persistence + Settings switcher.
5. Dialog + notification services.
6. Editor framework: full-page host (Projects, Invoices) and modal forms (Clients).
7. Retrofit all five views onto tokens/components; remove all hardcoded hex.
8. Manual light/dark pass; ensure test suite green.

---

## 9. Roadmap (Beyond This Slice)

Once the foundation is in place, later slices render into it: dashboard charts & reporting,
list search/filter/sort, time tracking → invoicing, expenses, recurring invoices/quotes, and
the previously deferred engines (invoice email sending, payments, project auto-scrape summary).
Distribution work (installer, code signing, auto-update, app icon, onboarding, crash logging)
is its own later slice.
