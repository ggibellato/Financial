# Implementation Plan: F06. CashFlow Bill & Mãe Entry Forms

**Prerequisites:**
- F01-F05 merged (tokens, shared primitives, naming fixes, and the proven per-field-validation pattern
  are all available, though this feature adds no validation of its own).
- No new tools/libraries.

### Stage 1: WPF Field Reorder and Row Continuity

**1. Reorder `AddBillFormView.xaml`** - Move the Area field to the front of the field sequence (Area →
Description → Due Day → Value → Note), per spec.md Decision D3.

**2. Restructure `EditBillFormView.xaml`** - Expand from 5 to 8 rows, reserving empty fixed-height rows
for Add's non-shared fields, pinning Value to the row it now occupies in the reordered
`AddBillFormView.xaml`, and moving Status/Error/Buttons down accordingly, per spec.md Decisions D6.

**3. Reorder `CreateEntryFormView.xaml` and fix its button width** - Move the Currency field to
immediately follow Date (per spec.md Decision D4), and change the primary button's `Width="100"` to
`"90"`.

**4. Restructure `EditEntryFormView.xaml`** - Expand from 5 to 8 rows so its trailing Error/Buttons rows
land on the same row indices as the reordered `CreateEntryFormView.xaml`'s (rows 6/7); BRL/GBP keep
their existing rows 1/2 since neither is shared with Create.

### Stage 2: Web Field Reorder and Button Text

**5. Reorder `MensaisPage.tsx`'s Add Bill field block and fix its confirm-button text** - Move the Area
field to the front of the Add form's field sequence, matching Stage 1's WPF order (spec.md Decision D3);
change the confirm button's `'Add'`/`'Adding...'` text to `'Add Bill'`/`'Adding Bill...'` (spec.md
Decision D1).

**6. Reorder `ControleMaePage.tsx`'s Create Entry field block** - Move the Currency field to immediately
follow Date in the Create-mode field block, matching Stage 1's WPF order (spec.md Decision D4).

### Stage 3: Standards Documentation

**7. Add the "Add/Edit variant layout continuity" rule** - Add the audit's already-drafted subsection to
`docs/ui/forms-data-and-visualisations.md` under "## Forms", after "### Layout" (spec.md Decision D7).

### Stage 4: Test Suite Alignment and Manual Verification

**8. Update and extend the affected test suites** - Update `MensaisPage.test.tsx` and
`ControleMaePage.test.tsx` for the new field order; add a button-text assertion to
`MensaisPage.test.tsx` for the Add Bill confirm button.

**9. Manually verify both platforms** - Open Add/Edit Bill and Create/Edit Entry on WPF (field order,
row continuity when switching Add↔Edit, button width) and on Web (field order, confirm-button text) per
`docs/ui/review-checklist.md`.
