# Implementation Plan: WPF Dynamic Picklist & Due-Date Editing

**Prerequisites:**
- F02 (CreditCardId wire contract) and F03 (`ICreditCardService.UpdateCreditCardAsync`, already consumed in-process) merged to `main`
- F04 (web equivalent) merged, useful as a UX reference but not a code dependency
- No new NuGet packages

### Stage 1: ViewModel Contract Catch-Up

**1. Credit Card State and Update Method** - Replace the hardcoded card-name list with a fetched, active-filterable collection of credit card entities, and add an update operation with its own loading/error state, following the existing card-statement region's structure in the same ViewModel.

**2. Expense Form Field Rewire** - Change the expense form's card field from a name string to a card identifier, updating the form-open, form-save, and validation logic that reads it, mirroring how the existing payment-source (bank) field already works by identifier.

### Stage 2: UI

**3. Expense Form Card Picker** - Switch the card dropdown to list only active cards fetched live, selecting by identifier instead of by name, mirroring the adjacent bank picker's binding shape.

**4. Credit Card Entity Grid** - Add a new grid to the Credit Card tab listing every card with an editable due-date field and active toggle per row, saving immediately on change.

**5. Stale Binding Fixes** - Correct the two leftover bindings to the pre-F02 card field name across the Credit Card tab's charge list and the card-statements grid.

### Stage 3: Tests

**6. Stub and ViewModel Tests** - Extend the credit card service test stub to track update requests, and add coverage for fetching/filtering active cards, saving a card's due date and active flag, the resulting dropdown update, and the expense form's identifier-based submission.

**7. Regression Fixes** - Update every existing test that constructs data or asserts against the old card-name-list contract to the identifier-based one.
