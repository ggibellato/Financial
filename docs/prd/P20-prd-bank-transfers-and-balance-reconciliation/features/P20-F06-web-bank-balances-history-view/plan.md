# Implementation Plan: Web Bank Balances & History View

**Prerequisites:**
- Existing Financial.Web toolchain (Vite, Vitest, React Testing Library) — no new dependencies

### Stage 1: API Client

**1. History and Delete Client Methods** - Add client methods for fetching a month's transfers, fetching a bank's balance adjustments, and deleting a transfer or a balance adjustment, following the existing request pattern.

### Stage 2: History Hook

**2. Bank History Hook** - Add a hook that fetches a month's transfers and each bank's adjustments, combines them per bank into a single reverse-chronological list classified by type (transfer in, transfer out, adjustment), and exposes delete operations that confirm before calling the backend and refresh both themselves and the caller's balance data afterward.

### Stage 3: Grid and Styling

**3. Banks Grid Actions and History** - Extend the banks grid so each row gains "Move money" and "Correct balance" actions and an expandable section listing that bank's combined history with edit and delete actions per entry.

### Stage 4: Page Composition

**4. Transfer Form Preselected Bank** - Extend the transfer form's create-open action to accept an optional bank to preselect as the source, so opening it from a specific bank row starts from that bank instead of the first one in the list.

**5. Monthly Page Wiring** - Compose the balances, transfer form, balance adjustment form, and bank history hooks on the Monthly page: render the two forms when open, wire the banks grid's new actions to open them pre-filled as appropriate, and make every successful save or delete refresh both the displayed balances and the history list.
