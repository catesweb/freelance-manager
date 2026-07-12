# Design: Dark-mode fixes, needs-attention inbox, sample data, payment reminders

Date: 2026-07-12
Status: approved

## Scope

Four items from the roadmap, ordered by build sequence:

1. Fix dark/light mode defects.
2. "Needs attention" inbox on the dashboard + clickable active projects.
3. Sample data loader on the welcome checklist + clickable checklist steps.
4. Payment-reminder emails for sent/overdue invoices.

## 1. Dark/light mode

Investigation (live repro, screenshots) showed theme switching, persistence, and
System-follows-OS all work. The actual defects:

- **DataGrid column header renders pure black in dark mode.** The Avalonia Fluent
  DataGrid theme's header brushes are never overridden, and its dark default is
  black — jarring against `BgSurface`. Fix: style `DataGridColumnHeader`
  (background `BgSurface`, foreground `TextMuted`) in `Themes/Controls.axaml`
  using existing tokens so both variants render correctly.
- **Theme only applies on Save.** Changing the Settings combo does nothing until
  Save, which reads as broken. Fix: apply the variant immediately when the combo
  changes (`OnThemeChanged` → `IThemeService.Apply`); persistence still happens on
  Save.
- Not a bug: the dark title bar is the OS "accent color on title bars" setting
  (`ColorPrevalence=1`). The `StatusBadge` already re-resolves brushes on variant
  change; left as is.

Side find fixed with it: projects with a stored `0001-01-01` due date (legacy bad
rows) display that literal date in the Projects grid. A small date-to-text
converter renders null/`DateTime.MinValue` as blank.

## 2. Needs-attention inbox + clickable active projects

- **Core:** static `AttentionBuilder.Build(projects, invoices, today)` returning
  ordered `AttentionItem(Kind, Title, Detail, ProjectId?, InvoiceId?)`:
  overdue invoices first (oldest due date first), then invoices/projects due
  within 7 days, then stale leads (status Lead, created more than 14 days ago).
  Pure function, unit-tested.
- **Dashboard:** a "Needs attention" card above the agenda listing those items;
  each row is a button. Active-projects entries become buttons too. Empty state:
  card hidden when there is nothing to show.
- **Navigation:** `WeakReferenceMessenger` (CommunityToolkit.Mvvm, already a
  dependency) with `OpenProjectMessage(int)` / `OpenInvoiceMessage(int)`.
  `MainWindowViewModel` registers, switches page, and asks the target list VM to
  select the record once its initial load completes (`OpenAsync(id)` awaits the
  stored load task). Selecting triggers the existing edit pane.

## 3. Sample data + clickable checklist

- "Load sample data" button on the existing welcome checklist card: seeds 3
  clients, 4 projects (Lead/Active/Complete, one overdue), 5 invoices
  (draft/sent/paid/overdue, one part-paid) through the existing repositories.
  All names carry a "(Sample)" suffix; **no schema flag** — user removes them like
  any record (decision: keep schema untouched).
- Guard: if a client containing "(Sample)" exists, notify and skip.
- Checklist steps become clickable, navigating to Settings/Clients/Invoices via
  the same messenger (`OpenPageMessage(string)`).

## 4. Payment reminders

- "Send reminder" button on the Invoices page, visible/enabled for effective
  status Sent or Overdue. Reuses the existing PDF-attach + SMTP pipeline and
  confirm dialog.
- Body composed by a small pure helper (`ReminderEmail` in Core, unit-tested):
  subject "Payment reminder: Invoice {number}", body with outstanding balance
  (total minus recorded payments), due date, and days overdue when applicable.
- No status change, no reminder history. Add history/scheduling only if needed
  later.

## Testing

- xUnit: `AttentionBuilder` ordering/filters, `ReminderEmail` composition,
  seeder creates expected record counts (real SQLite, as existing repo tests do).
- Manual verification: live app run — dark-mode grids, theme combo instant apply,
  inbox navigation, reminder flow (SMTP unconfigured path), sample data load.

## Explicitly skipped

Reminder history/scheduling, inbox snooze/dismiss, configurable thresholds
(7/14 days are constants), sample-data schema flag, StatusBadge refactor.
