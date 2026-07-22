# Implementation Plan: F15. Web — Controle Mae View

**Prerequisites:**
- F07's `/controle-mae/*` endpoints already exist and are unchanged by this feature
- F13/F14's `ApiError` and hook/page conventions already established
- No new npm packages

### Stage 1: API Client Layer

**1. Controle Mae DTOs and client methods** - Add the ledger entry type, the create-request type, and the values-only update-request type, plus the three new `financialApiClient` methods that call F07's existing endpoints.

**2. API client tests** - Add tests for the new methods' request shape.

### Stage 2: Page Logic and UI

**3. useControleMae hook** - Add the hook that tracks the selected month (defaulting to the current month), fetches entries on month change, manages the create-entry form state/submit, manages the single-row inline values-edit state/submit, and re-fetches after each successful action.

**4. ControleMaePage component** - Add the presentational page: month picker, loading/error states, create-entry form, and the entry table with inline BRL/GBP edit per row, wired to `useControleMae`.

**5. Wire up routing** - Replace the `/cashflow/controle-mae` placeholder route in `main.tsx` with `ControleMaePage`.

**6. Hook and component tests** - Add tests for `useControleMae`'s fetch/create/edit behavior and for `ControleMaePage`'s rendering of both currencies and both forms.

### Stage 3: Verification

**7. Full-suite validation** - Run the Web project's full test suite and typecheck, confirming no regression to any existing test.

**8. Manual verification** - Run the Web dev server against a locally running `Financial.Api`, navigate to `/cashflow/controle-mae`, and exercise the view directly (create an entry with a real past date and confirm both currencies populate via the live FX lookup, manually adjust one entry's values and confirm it saves, change the month and confirm a fresh set of entries loads) to confirm the behavior matches the acceptance criteria end-to-end.
