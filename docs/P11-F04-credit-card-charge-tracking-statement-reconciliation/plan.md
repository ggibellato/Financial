# Implementation Plan: F04. Credit Card Charge Tracking & Statement Reconciliation

**Prerequisites:**
- Existing `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure` projects and `data-cashflow.json` repository pattern from F02
- F03's `Expense` entity (with its `CardTag` field) and `CreditCard` enum already exist
- No new NuGet packages
- No repository interface changes needed — F02's existing `GetCardStatements`/`AddCardStatement` and F03's existing `GetExpenses` are sufficient

### Stage 1: Domain Layer

**1. Flesh out CardStatement** - Replace the placeholder with its real fields (card, year, month, paid flag) and a `Create` factory, plus an idempotent `MarkPaid` method.

**2. Domain tests** - Add tests for the entity's factory and its idempotent mark-paid method.

### Stage 2: Application Layer

**3. Card statement DTO** - Add the joined read model carrying the computed outstanding total alongside the paid state.

**4. Card statement service** - Add `ICardStatementService`/`CardStatementService` covering idempotent lazy generation of a month's 5 card statements, the outstanding-total calculation derived from that month's tagged expenses, and the idempotent mark-paid action with rollback on save failure.

**5. Register the new service** - Add `ICardStatementService` to the existing `CashFlowApplicationServiceCollectionExtensions`.

**6. Application-layer tests** - Add service tests covering the generation idempotency guarantee, the outstanding-total derivation (including exclusion of other months/cards and zeroing once paid), the mark-paid no-op-on-repeat behavior, the rollback-on-save-failure path, and the unknown-id error.

### Stage 3: Presentation Layer

**7. Card statements controller** - Add HTTP endpoints for getting (and lazily generating) a month's 5 statements and marking one paid, translating a not-found failure to 404.

**8. API integration tests** - Add endpoint tests covering the full get-month → mark-paid round trip over HTTP, including the repeat-mark-paid and not-found responses.

### Stage 4: Verification

**9. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow features from F01-F03 and F05-F08.

**10. Manual verification** - Run `Financial.Api` locally and exercise the new endpoints directly (create a card-tagged expense, fetch the month's statements and confirm the outstanding total reflects it, mark that statement paid and confirm the total zeroes out, call mark-paid again and confirm it still succeeds, fetch an unrelated month and confirm its statements are unaffected) to confirm the behavior matches the acceptance criteria end-to-end.
