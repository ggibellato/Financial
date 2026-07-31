# Implementation Plan: F05. Reserva Reserve Pool & Automated Split

**Prerequisites:**
- Existing `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure` projects and `data-cashflow.json` repository pattern from F02
- No new NuGet packages

### Stage 1: Domain Layer

**1. Reserve bucket enum** - Add the 5-member `ReserveBucket` enum (Dizimo, Investimento, house-treats, Ariana, Gleison), using the accurate "house-treats" name rather than the spreadsheet's legacy "Viagem" label.

**2. Flesh out the ReserveMovement entity** - Replace the placeholder `ReserveMovement` with its real fields (bucket, amount, date, description) and a `Create` factory.

**3. Remove-by-id on the root aggregate** - Add a `RemoveReserveMovement` method to `CashFlowData`, used later for rollback on a partial save failure.

**4. Reserve split calculator** - Add a pure calculation rule that takes net salaries plus Lottery and Dividendo/Juros and returns the Dizimo amount and the four-way Limpo split, matching the exact tithe-then-thirds/sixths math.

**5. Domain tests** - Add tests for the split calculator's math (including the boundary that Lottery/Dividendo/Juros affect only Dizimo), the entity's factory, and the root aggregate's removal method.

### Stage 2: Repository Extension

**6. Extend the repository abstraction** - Add a `DeleteReserveMovement` method to `ICashFlowRepository` and implement it in `CashFlowJsonRepository`, mirroring the existing `DeleteExpense` pattern from F03.

### Stage 3: Application Layer — Service and Validation

**7. Reserve DTOs** - Add the income-split request/result, withdrawal request, movement read model, and balance read model DTOs.

**8. Overdraft exception and bucket parser** - Add the exception type that signals an unconfirmed overdraft withdrawal, and a bucket-name parser matching the existing enum-parsing convention.

**9. Reserve service** - Add `IReserveService`/`ReserveService` covering the income split (validating no negative inputs, computing the split, posting all 5 movements atomically with rollback on a partial save failure) and manual withdrawals (validating the amount and bucket, enforcing the overdraft-confirmation flow), plus bucket-balance and movement-history queries.

**10. Register the new service** - Add `IReserveService` to the existing `CashFlowApplicationServiceCollectionExtensions`.

**11. Application-layer tests** - Add service tests covering the split math end-to-end, negative-input rejection, the rollback-on-save-failure path, both sides of the overdraft-confirmation flow, and the balance/history queries.

### Stage 4: Presentation Layer

**12. Reserve controller** - Add HTTP endpoints for the income split, withdrawals, bucket balances, and movement history, translating validation failures to 400 and unconfirmed overdrafts to 409.

**13. API integration tests** - Add endpoint tests covering the full income-split and withdrawal round trip over HTTP, including the negative-input and overdraft-confirmation error responses.

### Stage 5: Verification

**14. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow features from F01-F03.

**15. Manual verification** - Run `Financial.Api` locally and exercise the new endpoints directly (income split, withdrawal within balance, unconfirmed and confirmed overdraft withdrawal, balances, movement history) to confirm the behavior matches the acceptance criteria end-to-end.
