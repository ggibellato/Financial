# Implementation Plan: F16. Web — Investment Snapshots View

**Prerequisites:**
- F08's `/investment-snapshots/*` endpoints already exist and are unchanged by this feature
- F13/F14/F15's `ApiError` and hook/page conventions already established
- No new npm packages

### Stage 1: API Client Layer

**1. Investment snapshot DTOs and client methods** - Add the snapshot type and the value-only update-request type, plus the two new `financialApiClient` methods that call F08's existing endpoints.

**2. API client tests** - Add tests for the new methods' request shape.

### Stage 2: Page Logic and UI

**3. useInvestmentSnapshots hook** - Add the hook that tracks the selected month (defaulting to the current month), fetches the month's 11 snapshots on month change, manages the single-row inline value-edit state, and re-fetches after a successful update.

**4. InvestmentSnapshotsPage component** - Add the presentational page: month picker, loading/error states, a single table of all 11 accounts with inline per-row value editing, wired to `useInvestmentSnapshots`.

**5. Wire up routing** - Replace the `/cashflow/investment-snapshots` placeholder route in `main.tsx` with `InvestmentSnapshotsPage`.

**6. Hook and component tests** - Add tests for `useInvestmentSnapshots`'s fetch/edit behavior and for `InvestmentSnapshotsPage`'s rendering of all 11 accounts and the edit flow.

### Stage 3: Verification

**7. Full-suite validation** - Run the Web project's full test suite and typecheck, confirming no regression to any existing test.

**8. Manual verification** - Run the Web dev server against a locally running `Financial.Api`, navigate to `/cashflow/investment-snapshots`, and exercise the view directly (confirm all 11 accounts render for the current month, edit one account's value and confirm it saves without affecting other accounts, change the month and confirm a fresh set of snapshots loads) to confirm the behavior matches the acceptance criteria end-to-end.
