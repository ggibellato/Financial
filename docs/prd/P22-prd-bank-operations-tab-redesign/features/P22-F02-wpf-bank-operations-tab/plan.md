# Implementation Plan: F02. WPF Bank Operations Tab

**Prerequisites:**
- .NET/WPF toolchain already used by `Financial.App` (no new packages)
- No environment variables or configuration changes
- No new DI registrations beyond what `MonthlyViewModel` already has in `App.xaml.cs`

### Stage 1: Data Model for Bank Operations

**1. Introduce the flat operations row type** - Add a new view-model type representing one row of the combined Transfer/Balance-Adjustment list, carrying both the fields needed to display it (date, type, bank label, signed amount, note) and the fields needed to filter it (source/destination for transfers, single bank for adjustments), plus a reference back to its underlying DTO for edit/delete. Reference the spec's Component Overview for the exact shape.

**2. Retire the per-bank expandable history model** - Remove the type and per-row expand state that supported the old expandable, per-bank history view, since that presentation is being replaced wholesale by the new flat list rather than patched.

**3. Simplify the Summary Banks grid's row model** - Reduce the Summary grid's row type down to just the bank name, balance, and round-up total the trimmed grid needs, dropping the expand/history members retired in the previous step.

### Stage 2: ViewModel - Flat Operations List & Filtering

**4. Build the month-scoped operations collection** - During the shared Monthly refresh cycle, reshape the transfers and per-bank adjustments already being fetched (no new service calls) into one flat, newest-first collection covering every bank for the selected month.

**5. Add bank-filter state** - Introduce a filter selection defaulting to "All Banks," an options list derived from the currently configured banks, and the source/destination-OR / exact-bank matching logic that produces the filtered collection the Bank tab displays, re-evaluated entirely client-side whenever the filter or the underlying list changes.

**6. Repoint edit/delete onto the flat rows** - Adapt the existing transfer and adjustment edit/delete commands so they operate on the new flat rows instead of the retired per-bank grouping, preserving their existing confirm-then-refresh behavior for both operation types.

### Stage 3: ViewModel - Bank-Picker-First Balance Correction & Generic Entry Points

**7. Rework the Correct Balance form's opening flow** - Change the Balance Adjustment form so it can open with no bank chosen, revealing the remaining fields and that bank's current calculated balance only once a bank is picked, while keeping the bank fixed and non-reselectable when the form is opened to edit an existing adjustment.

**8. Gate saving on bank selection** - Ensure the Correct Balance form's save action stays disabled until a bank has been chosen, consistent with how the form already disables saving while a save is in progress.

**9. Wire the two generic entry-point commands** - Adapt the Move Money and Correct Balance commands so the Bank tab's top-level buttons open each form without any row-specific context, replacing the retired per-row triggers that used to live on the Summary grid.

### Stage 4: Views - Bank Tab & Simplified Summary Grid

**10. Simplify the Summary Banks grid view** - Trim the grid to bank name, balance, and round-up columns plus its existing totals row, removing the expand control, the per-row action buttons, the embedded forms, and the row-details template.

**11. Add the Bank tab and its view** - Create the new Bank section view hosting the two entry-point buttons, the reused Move Money and Correct Balance forms, the bank filter control, the flat operations list, and its empty state; then add it as the Monthly view's fourth tab, after Summary/Expense/Income.

**12. Add the bank-selection field to the Correct Balance form** - Insert the bank picker as the form's first field, following the existing reference-row layout convention already used in this app's forms (error text and each field kept in its own row, never overlapping), and gate the rest of the form's visibility behind a bank having been picked.

**13. Wire the operations list's row actions** - Bind each row's Edit and Delete controls to the repointed commands from Stage 2, following the same kind-based visibility pattern already used elsewhere in this app for rows that can represent more than one underlying type.
