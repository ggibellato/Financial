# Implementation Plan: High-Risk Branch Coverage

**Prerequisites:**
- An up-to-date `main`; F08 (clock and delay seams), F09 (ratchet, baseline-reminder) and F06/F07 (constructor-guard theories, categories) are on `main`
- `pwsh` available; a per-file branch-coverage script is built from the CI `Cobertura.xml` artifacts (class-level lines, covered/total conditions)
- Read `testing-guide-Financial` and `docs/rules/implementation.md` §Tests before writing tests; no comments in new code, no new `Task.Delay`, no wall-clock reads
- One PR per stage; every stage leaves `main` green. The PRD boxes are ticked in the last stage's own commit
- Each stage measures its target classes from its own CI run and states the figures in the PR body

### Stage 1: WPF CashFlow View Models (PR1)

**1. Reserva** - Add the failed-load, empty-bucket, general-error, null-row, declined-delete and failed-delete tests to `ReservaViewModelTests`, asserting messages, unchanged collections and re-enabled commands.

**2. Income split and reporting currency** - Cover `SplitGeneralSaveError`, `ShowSplitFormFields` and the reporting-currency setters (GBP, BRL, USD, enabled), awaiting `PropertyChanged` rather than delaying.

**3. Measure** - Read the stage's CI artifact, record per-class branch figures (Reserva must reach 85%), and write the figures in the PR body.

### Stage 2: WPF Cards, Transfer and Corporate Actions (PR2)

**4. Cards and transfer** - Add the null-statement, unchanged-card, same-bank, last-used resolution and general-error tests to the two workflow test files.

**5. Corporate actions** - Cover the generic-error, cancelled-form, no-service, `ResolveAffectedAsset` and can-execute paths in `CorporateActionsTabViewModelTests`; make `RatioFactorToFraction` `internal` only if its branches cannot be reached otherwise.

**6. WPF job check** - Compare the wpf job's branch figure with 85%; if short, add a top-up from the next-lowest view models in the same report and list them in the PR.

### Stage 3: Backend Branches and Pinned-Clock Edge Cases (PR3)

**7. Admin service and replay** - Add the missing-portfolio and scope-fallback tests for `AssetAdminService` and the ratio, allocation and unsupported-combination tests for `CorporateActionReplay`.

**8. Summary controller** - Verify whether the blank-name guards are dead behind model validation; delete them if so, otherwise test the controller directly.

**9. Edge cases** - Add the payments-due 29th/`DueDay=2` pin (cited from P42), the new `AnnualAverageMonthsCalculator` tests and the future-date `UsdBasedExchangeRateProvider` test, all on pinned clocks.

### Stage 4: Web Branches and PRD Ticks (PR4)

**10. Recharts mock** - Change the `recharts` mock in the Credits and Transactions tests so `Tooltip`, `LabelList` and `Bar` invoke their `formatter` and `dataKey` props.

**11. Credits, transactions and expense form** - Add the yield-sort, unknown-type, inflow/neutral-row, formatter and tithe-checkbox tests.

**12. PRD ticks** - Tick the satisfied F10 boxes and the F10 cross-feature box in their own commit; leave unticked, with the measured figure, any target that was not reached.
