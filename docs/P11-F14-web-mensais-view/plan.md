# Implementation Plan: F14. Web — Mensais View

**Prerequisites:**
- F06's `/mensais/*` endpoints already exist and are unchanged by this feature
- F13's `ApiError` and hook/page conventions already established
- No new npm packages

### Stage 1: API Client Layer

**1. Mensais DTOs and client methods** - Add the recurring bill instance type and the update-request type, plus the two new `financialApiClient` methods that call F06's existing endpoints.

**2. API client tests** - Add tests for the new methods' request shape.

### Stage 2: Page Logic and UI

**3. useMensais hook** - Add the hook that tracks the selected month (defaulting to the current month), fetches and groups instances into Brasil/UK on month change, manages the single-row inline-edit state, and re-fetches after a successful update.

**4. MensaisPage component** - Add the presentational page: month picker, loading/error states, two grouped tables with inline per-row status/value editing, wired to `useMensais`.

**5. Wire up routing** - Replace the `/cashflow/mensais` placeholder route in `main.tsx` with `MensaisPage`.

**6. Hook and component tests** - Add tests for `useMensais`'s fetch/group/edit behavior and for `MensaisPage`'s rendering of both grouped sections and the edit flow.

### Stage 3: Verification

**7. Full-suite validation** - Run the Web project's full test suite and typecheck, confirming no regression to any existing test.

**8. Manual verification** - Run the Web dev server against a locally running `Financial.Api`, navigate to `/cashflow/mensais`, and exercise the view directly (confirm Brasil/UK sections render for the current month, change the month and confirm a fresh set of instances loads, edit an instance's status/value and confirm it updates without affecting other months) to confirm the behavior matches the acceptance criteria end-to-end.
