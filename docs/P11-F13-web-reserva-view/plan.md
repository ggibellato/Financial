# Implementation Plan: F13. Web — Reserva View

**Prerequisites:**
- F05's `/reserve/*` endpoints already exist and are unchanged by this feature
- F11's Web app shell, routing, and `CashFlowLayout` nav already exist
- No new npm packages

### Stage 1: API Client Layer

**1. Typed API error** - Add an `ApiError` class carrying the HTTP status alongside the message, and switch the shared `request()` helper in `financialApiClient` to throw it instead of a plain `Error`.

**2. Reserva DTOs and client methods** - Add the bucket-balance, movement, income-split request/result, and withdrawal-request types, plus the four new `financialApiClient` methods that call F05's existing endpoints.

**3. API client tests** - Add tests for the new methods' request shape and for `ApiError`'s `status` field on a non-2xx response, alongside a regression check that existing error-throwing tests still pass with the new error type.

### Stage 2: Page Logic and UI

**4. useReserva hook** - Add the hook that loads balances and movement history on mount, manages both forms' state, submits the income split and withdrawal actions, re-fetches on success, and implements the 409-triggered confirm-and-resubmit round trip for withdrawals.

**5. ReservaPage component** - Add the presentational page rendering the loading/error states, the balances table, the movement history table, and both forms, wired to `useReserva`.

**6. Wire up routing** - Replace the `/cashflow/reserva` placeholder route in `main.tsx` with `ReservaPage`.

**7. Hook and component tests** - Add tests for `useReserva`'s fetch-on-mount, submit/re-fetch, and overdraft-confirmation behavior, and for `ReservaPage`'s rendering of all states and forms.

### Stage 3: Verification

**8. Full-suite validation** - Run the Web project's full test suite (`npm test`) and typecheck (`tsc -b --noEmit`), confirming no regression to any existing Investments-domain test.

**9. Manual verification** - Run the Web dev server against a locally running `Financial.Api`, navigate to `/cashflow/reserva`, and exercise the view directly (confirm all 5 buckets and movement history render, post an income split and confirm the balances update immediately, attempt a withdrawal that exceeds a bucket's balance and confirm the overdraft prompt appears and a confirmed resubmit succeeds) to confirm the behavior matches the acceptance criteria end-to-end.
