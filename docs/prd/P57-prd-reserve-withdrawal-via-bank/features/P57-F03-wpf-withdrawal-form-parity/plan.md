# Implementation Plan: WPF Withdrawal Form Parity

**Prerequisites:**
- F01 and F02 merged (F02's "Field behavior contract" is the UX reference)
- Work on its own branch (never on `main`); PR title uses a Conventional Commits prefix
- .NET SDK on Windows (WPF); no new packages or configuration
- Read `docs/rules/ui.md`, `docs/ui/wpf.md` and `docs/ui/forms-data-and-visualisations.md` before coding; no comments in new code

### Stage 1: View Model Rules and State

**1. Bank option and category rules** - Add the small bank option type and the category rules helper described in the spec's Component Overview, mirroring F02's category eligibility and bucket-name default.

**2. Validation** - Extend the withdrawal validation with the "category required when a bank is selected" rule, keeping the existing callers working.

**3. Withdrawal view model** - Add the bank and category state, derived default, options, field error, editable-while-saving flag and reference-data loading, reset the new fields when the form opens, and send both ids on the request without changing the overdraft flow.

### Stage 2: Loading and Composition

**4. Reserva view model and composition root** - Load banks and categories with the reference data tolerantly, hand them to the withdrawal view model, rename the refresh parameter, and register the two extra services for the view model in the app's composition root.

### Stage 3: View

**5. Withdrawal form view** - Rework the form into the four-column reflowing layout in the parity table's order with the conditional category field, contextual help, disabled-while-saving state and the aligned confirm labels.

### Stage 4: Verification

**6. Tests** - Add the rules, validation, withdrawal and Reserva view-model tests from the spec, updating the Reserva test factory for the new constructor arguments.

**7. Manual check, UI review and Definition of Done** - Build and launch the WPF app on a temporary copy of the data and exercise both withdrawal paths, complete `docs/ui/review-checklist.md`, get the UI reviewer's sign-off, and complete the Definition of Done from `docs/rules/implementation.md`, including a grep of the diff for added comments. Leave the PRD's manual-check and cross-feature boxes for the user's confirmation.
