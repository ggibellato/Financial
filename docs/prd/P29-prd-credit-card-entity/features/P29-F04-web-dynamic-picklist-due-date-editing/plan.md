# Implementation Plan: Web Dynamic Picklist & Due-Date Editing

**Prerequisites:**
- F02 (CreditCardId wire contract) and F03 (`GET`/`PUT /credit-cards`) merged to `main`
- No new npm packages

### Stage 1: Contract Catch-Up

**1. Frontend Types** - Add the `CreditCardDto`/`UpdateCreditCardDto` types, and rename every existing frontend type still using the legacy `cardTag`/`card` string fields (Expense create/update/read DTOs, card statement DTO) to the `creditCardId`/`creditCardName` shape the backend has used since F02.

**2. API Client** - Add the list and update calls for credit cards to the shared API client, following the existing GET-list/PUT-update method pairs already used for banks and reserve buckets.

### Stage 2: Data Access

**3. Credit Cards Hook** - Add a hook that fetches the credit card list and exposes a per-card update operation with loading/error state, following the existing fetch-and-update hook pattern used for investment snapshots.

### Stage 3: UI

**4. Expense Form Card Dropdown** - Replace the hardcoded card-name list in the expense form with the live, active-only card list, submitting the selected card's identifier instead of its name.

**5. Credit Card Tab Additions** - Add a new table to the Credit Card tab listing every card with an editable due-date field and active toggle per row, wired to save immediately on change; update the existing card-statement table and expense list to use the renamed display fields.

**6. Downstream Renames** - Update every remaining place that read the old `cardTag`/`card` fields (expense create/edit state, submitted request bodies) to use the new identifier-based fields end to end.

### Stage 4: Tests

**7. API and Hook Tests** - Add coverage for the new client methods and hook, and update any existing test relying on the old card-tag contract.

**8. Component Tests** - Add coverage for the new due-date/active-toggle table and the expense form's live dropdown, and update existing component tests whose assertions reference the old field names.
