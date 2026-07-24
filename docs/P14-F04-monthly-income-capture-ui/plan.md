# Implementation Plan: Monthly Income Capture UI

**Prerequisites:**
- Node/npm toolchain already configured for `Financial.Web` (Vite, Vitest, React Testing Library)
- No new dependencies

### Stage 1: API Layer

**1. Income Types and Client Methods** - Add the income read/create/update DTOs and the four corresponding API client methods (list by month, create, update, delete), following the exact request/response shape already established for expenses.

### Stage 2: Page State

**2. Income State in useMonthly** - Extend the Monthly page's existing reducer with an income data slice and create/edit form fields, fetched alongside the page's other month-scoped data. Replace the current single-form-open flag with one that also tracks which entity's form is active, so the expense and income forms share the same panel without both being open at once. Add the submit/edit/delete handlers, mirroring the expense handlers' validation and refetch-on-success behavior.

### Stage 3: UI Components

**3. Income List Section** - Add a presentational list component for income entries (new-entry button, table with edit/delete actions), structured like the existing expense list component.

**4. Income Form and Page Composition** - Add the income entry form (date, source, conditional gross value, net value, bank) into the Monthly page, sharing the existing form panel with the expense form, and render the new income list alongside the expense list.
