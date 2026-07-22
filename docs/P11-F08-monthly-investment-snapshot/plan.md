# Implementation Plan: F08. Monthly Investment Snapshot

**Prerequisites:**
- Existing `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure` projects and `data-cashflow.json` repository pattern from F02
- No new NuGet packages
- No repository interface changes needed — F02's existing `GetInvestmentSnapshots`/`AddInvestmentSnapshot` are sufficient

### Stage 1: Domain Layer

**1. Investment account enum and classification** - Add the 11-member `InvestmentAccount` enum and the `InvestmentAccountClassification` rule identifying the 5 liability accounts.

**2. Flesh out InvestmentSnapshot** - Replace the placeholder with its real fields (account, year, month, value) and a `Create` factory, plus an `Update` method for in-place value edits.

**3. Domain tests** - Add tests for the entity's factory and update method, and for the classification rule's liability lookups.

### Stage 2: Application Layer

**4. Investment snapshot DTOs** - Add the joined read model and the value-only update request.

**5. Account parser** - Add a parser for `InvestmentAccount` string values, following the existing enum-parsing convention.

**6. Investment snapshot service** - Add `IInvestmentSnapshotService`/`InvestmentSnapshotService` covering idempotent lazy generation of a month's 11 snapshots (joined with each account's liability classification) and independent per-snapshot value updates with non-negative validation.

**7. Register the new service** - Add `IInvestmentSnapshotService` to the existing `CashFlowApplicationServiceCollectionExtensions`.

**8. Application-layer tests** - Add service tests covering the generation idempotency guarantee, the liability classification join, value updates leaving other months/accounts untouched, negative-value rejection, and the account parser.

### Stage 3: Presentation Layer

**9. Investment snapshots controller** - Add HTTP endpoints for getting (and lazily generating) a month's 11 snapshots and updating a single snapshot's value, translating validation and not-found failures to 400/404.

**10. API integration tests** - Add endpoint tests covering the full get-month → update-value round trip over HTTP, including the negative-value and not-found error responses.

### Stage 4: Verification

**11. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow features from F01-F07.

**12. Manual verification** - Run `Financial.Api` locally and exercise the new endpoints directly (fetch a month and confirm exactly 11 accounts are generated with the correct liability flags, update one account's value, re-fetch the same month and a different month to confirm nothing else changed) to confirm the behavior matches the acceptance criteria end-to-end.
