# Implementation Plan: F04. Reserve Movement Lock & Indicator

**Prerequisites:**
- .NET SDK / existing `Financial.slnx` build toolchain
- Node/npm for `Financial.Web`
- No new libraries, no new environment variables, no API/data changes (F02 already shipped `IncomeId`/`incomeId` and the server-side 409 rejection)

### Stage 1: React Reserve Movement Lock & Indicator

**1. Movement Row Locked Derivation** - Add a derived locked flag to the Reserve section's movement row model, computed from the existing income-link field on each movement, alongside the existing group-total derivation.

**2. Reserve Grid Wiring** - Add a lock indicator column to the movement grid, shown only for locked rows, carrying the explanatory text as its accessible name/tooltip; disable the Edit and Delete controls on a locked row while leaving unlocked rows fully unaffected.

### Stage 2: WPF Reserve Movement Lock & Indicator

**3. Movement Row and Command Locked Gating** - Mirror the React row model's locked derivation in the view model's row type; gate the existing Edit/Delete commands' `CanExecute` on the row's locked state so the bound buttons disable themselves automatically.

**4. Reserve Grid View Wiring** - Add the equivalent lock indicator column to the movement grid view, with a tooltip/accessible name carrying the explanatory text; ensure the Edit/Delete buttons' own tooltips remain visible once disabled.

### Stage 3: Testing

**5. React Tests** - Cover the row model's locked derivation and the grid's conditional rendering/disabling for locked vs. unlocked rows, including a regression check that the existing group-delete warning still works for an unlocked grouped movement.

**6. WPF Tests** - Cover the row model's locked derivation and the Edit/Delete commands' `CanExecute` gating for locked vs. unlocked rows.
