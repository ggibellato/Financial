# Implementation Plan: Web Dynamic Picklist

**Prerequisites:**
- F02 (Category entity-reference wire contract) and F03 (`GET /categories`) merged to `main`
- No new npm packages

### Stage 1: Contract Catch-Up

**1. Frontend Types** - Add the `CategoryDto` type, and rename every existing frontend type still using the legacy `category` name-string field (Expense create/update/read DTOs) to the `categoryId`/`categoryName` shape the backend has used since F02.

**2. API Client** - Add the list call for categories to the shared API client, following the existing GET-list method pattern already used for banks and credit cards.

### Stage 2: Data Access

**3. Category Fetch in useMonthly** - Fold the category list fetch into the existing `Promise.all` in `useMonthly.ts` (alongside banks/income sources), since categories have no update capability and don't warrant a separate hook. Default the create-form's category selection to the first active category, once fetched, mirroring the existing default-bank pattern.

### Stage 3: UI

**4. Expense Form Category Dropdown** - Replace the hardcoded category-name list in the expense form with the live, active-only category list, submitting the selected category's identifier instead of its name.

**5. Downstream Renames** - Update every remaining place that read the old `category` name field (expense list display, expense create/edit state, submitted request bodies) to use the new identifier/name-pair fields end to end.

### Stage 4: Tests

**6. API and Hook Tests** - Add coverage for the new client method, and update any existing test relying on the old category-name contract in `useMonthly.test.ts`.

**7. Component Tests** - Update the expense form's dropdown tests and the expense list's category-display test for the new identifier-based contract.
