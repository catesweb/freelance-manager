# Editable & aligned status blips — design

Date: 2026-06-18

## Problem

The status blip shown in the Projects and Invoices list pages is read-only and
visually misaligned:

1. Project status cannot be changed directly from the list — only via the full
   edit form.
2. Invoice status cannot be changed directly from the list — only via the full
   edit form.
3. The project status blip's label is not centered within the pill, and the pill
   is not centered in its column (cosmetic).
4. The invoice status blip has the same alignment defects (cosmetic).

Note: status *is* already editable through the full edit form (both
`ProjectEditView` and `InvoiceEditView` expose a Status `ComboBox`). The request
is to make the blip in the list itself directly editable, with the change saved
immediately.

## Approach

Wrap the existing read-only `StatusBadge` in a flat, transparent `Button` whose
`Flyout` lists the selectable statuses. Clicking the blip selects the row and
opens the flyout; choosing a status invokes a ViewModel command that persists the
change immediately and refreshes the list. The `StatusBadge` control stays a pure
display control (still reusable read-only elsewhere, e.g. the dashboard), so the
alignment fix is made in one place.

Rejected alternatives:
- **Restyle a `ComboBox` to look like the pill** — heavy custom theming across
  light/dark variants, and duplicates the existing badge visual and its
  theme-refresh logic for little gain.
- **Bake the dropdown into `StatusBadge`** — couples display with editing; the
  badge is also used in read-only contexts, which would then need an opt-out
  toggle anyway.

## Components & behavior

### Status cell (both list views)

In the Status `DataGridTemplateColumn` cell template of `ProjectsView.axaml` and
`InvoicesView.axaml`:

- The `StatusBadge` becomes the content of a flat/transparent `Button`.
- The `Button` carries a `Flyout` listing the selectable statuses for that
  entity. Each option, when chosen, invokes the list ViewModel's
  `SetStatusCommand` with the chosen status enum as the command parameter.
- The option command is reached from inside the cell template via the parent
  `DataGrid`'s `DataContext` (the list ViewModel).

Selectable statuses:

- **Projects:** `Lead`, `Active`, `Complete`, `Archived` (all values of
  `ProjectStatus`).
- **Invoices:** `Draft`, `Sent`, `Paid` (the stored values of `InvoiceStatus`).
  `Overdue` is **excluded** — it is derived by `OverduePolicy`, never stored.

### Save-on-change (ViewModels)

`ProjectsViewModel.SetStatusCommand(ProjectStatus status)`:

1. No-op if `Selected` is null.
2. Load the project fresh via `_projects.GetAsync(Selected.Id)`.
3. Set `Status`, call `_projects.UpdateAsync(model)`.
4. Show a success notification (error notification on exception).
5. Call `LoadAsync()` to refresh the list — mirrors the existing `Save` flow.

`InvoicesViewModel.SetStatusCommand(InvoiceStatus status)`:

- Same shape, operating on the selected invoice (`_invoices.GetAsync(Selected.Id)`
  → set `Status` → `UpdateAsync` → notify → `LoadAsync`).
- Because `LoadAsync` rebuilds each `InvoiceRow` through
  `OverduePolicy.EffectiveStatus`, the effective status recomputes automatically:
  setting `Sent` on a past-due invoice immediately shows `Overdue`; setting `Paid`
  clears the overdue state.

Each command takes a single enum parameter and acts on the currently `Selected`
row. Clicking the blip selects its row (DataGrid default) before the flyout
option is chosen, so `Selected` is the intended target. Keeping the parameter to
a single enum keeps the XAML binding simple and the command unit-testable.

### Alignment fix (#3 / #4)

In `StatusBadge.axaml`:

- Give the `TextBlock` explicit `HorizontalAlignment="Center"`,
  `VerticalAlignment="Center"`, and `TextAlignment="Center"` so the label is
  centered within the pill. Adjust the `Border` `Padding` (currently `9,2`) only
  if the Lato baseline still looks visually off after this change.

In both cell templates:

- Change the badge wrapper's `HorizontalAlignment` from `Left` to `Center` so the
  pill is centered in the Status column.

## Error handling

Persistence is wrapped in try/catch following the existing pattern; failures show
an error notification via `INotificationService`. On failure, `LoadAsync` reverts
the visual to the stored truth.

## Testing

Unit tests for the two `SetStatusCommand`s, using the existing in-memory `TestDb`
pattern from the current `ProjectsViewModel` / `InvoicesViewModel` tests:

- Setting a status persists the chosen value to the repository.
- Invoice command recomputes effective status after save: `Sent` + past-due due
  date → row shows `Overdue`; `Paid` → row shows `Paid`.
- The command no-ops when `Selected` is null.

The flyout interaction and the visual alignment are verified manually by running
the app (Avalonia XAML is not unit-tested here).

## Files touched

- `src/FreelanceManager.App/Controls/StatusBadge.axaml` — text centering.
- `src/FreelanceManager.App/Views/ProjectsView.axaml` — editable blip cell +
  centered column.
- `src/FreelanceManager.App/Views/InvoicesView.axaml` — editable blip cell +
  centered column.
- `src/FreelanceManager.App/ViewModels/ProjectsViewModel.cs` — `SetStatusCommand`.
- `src/FreelanceManager.App/ViewModels/InvoicesViewModel.cs` — `SetStatusCommand`.
- `tests/FreelanceManager.Tests/` — new tests for the two commands.

## Out of scope

The other TODO items (invoice email, payment tracking, search/filter, etc.) are
not addressed here.
