# Mutation Review — Plan and Progress

A slice-by-slice review of the test suite with Stryker.NET and StrykerJS. Each slice answers
three questions:

- **What is missing** — a Survived or NoCoverage mutant is a change no test noticed.
- **What is not worth keeping** — a test that kills no mutant in its own scope.
- **What is duplicated** — a test whose kills are all made by another test too.

Tests are judged against `.claude/skills/testing-guide-Financial/SKILL.md` §2 before any is
removed. AC-tracing tests and contract tests overlap on purpose and are never removed for
overlap alone.

## Status legend

| Mark | Meaning |
|---|---|
| ⬜ | Not started |
| 🔄 | In progress (branch named in the PR column) |
| ✅ | Done and merged |
| ⏭️ | Skipped, reason in Notes |

## Baseline (nightly run 2026-11-14, `18089c83`)

| Scope | Score | Mutants | Survived | No coverage | Run time (CI) |
|---|---|---|---|---|---|
| CashFlow.Domain | 90.5% | 1,005 | 85 | 10 | ~5 min |
| Investment.Domain | 78.0% | 1,150 | 177 | 76 | ~5 min |
| Web.Logic (`src/utils`, `src/hooks`) | 65.2% | 4,885 | 1,701 undetected | — | ~20 min |
| Application, Infrastructure, Shared, Api, web components/pages | not mutated | — | — | — | — |

## Local baseline (batch 0.4, 2026-11-14, `9a2be798`, bail off, whole project)

Every project's own test project only, `Category!=Live&Category!=E2E`. Run times are this
machine's, for the whole project; a slice is a fraction of that.

| Project | Score | Detected / valid | Compile errors | Run time |
|---|---|---|---|---|
| CashFlow.Domain | 90.5% | 910 / 1,005 | 54 | 3.4 min |
| Investment.Domain | 79.0% | 908 / 1,150 | 38 | 1.8 min |
| CashFlow.Application | 64.4% | 1,385 / 2,151 | 308 | 2.6 min |
| Investment.Application | 46.6% | 716 / 1,538 | 86 | 1.9 min |
| Shared.Abstractions | 63.9% | 53 / 83 | — | 0.7 min |
| Shared.Infrastructure | 74.3% | 113 / 152 | — | 1.7 min |
| CashFlow.Infrastructure | 80.2% | 199 / 248 | 43 | 7.2 min |
| Investment.Infrastructure | 81.1% | 215 / 265 | — | 4.7 min |
| Api | 77.3% | 306 / 396 | 38 | 28.3 min |

What it already shows:

- **Every slice fits one sitting**, so no slice below needs splitting for run time. Api is the
  only project near the limit as a whole (883 host tests per run); its slices are well under it.
- **The Application projects are the gap**: Investment.Application 46.6%, CashFlow.Application
  64.4%, against 79–91% for the Domains.
- **Some tests live in another project.** `AssetPriceLookupService` (71 of 72 mutants undetected)
  and `AssetPriceHistoryService` (31 of 32) are Investment.Application classes tested only from
  `Financial.Investment.Infrastructure.Tests`, which a per-project run does not use. They run
  the service over the real JSON repository, so they cannot move without the Application tests
  referencing Infrastructure; slice 4.5 mutates with both test projects instead
  (`-AlsoTestedBy Investment.Infrastructure`).
- **DI registration files score 0%** in each Application project (27 and 30 mutants): they are
  proven by the host tests in `Financial.Api.Tests`, not by the project's own tests. Exclude them
  from the per-project score (`!**/DependencyInjection/**`) rather than write tests for them.
- **Compile errors hide whole methods.** When a mutant does not compile, Stryker's Safe Mode drops
  every mutant in that method: `RecreateCalendarAndResyncAllLockedAsync`
  (`CreditCardCalendarSyncService`), `EnsureStatementsOpenForMove` (`ExpenseService`),
  `UpdateMovementAsync`, `ApplyOtherParticipant` (`AccountJsonConverter`). Those methods are not
  measured at all; their slices review them by reading.

## How one slice is worked

1. **Run** Stryker on the slice only, against the project's own test project, with bail off so
   the report lists every test that kills each mutant:
   - .NET: `pwsh scripts/stryker.ps1 -Target CashFlow.Domain -Mutate "**/Expense.cs" -Tests '\.ExpenseTests\.' -DisableBail -NoOpen`
     (reports under `TestResults/stryker/<target>/<timestamp>/`). `-Mutate` takes several
     patterns separated by commas; `-AlsoTestedBy <project>` adds another test project for code
     tested from outside its own project.
   - Web, in `Financial.Web`: `$env:STRYKER_TEST_INCLUDE = 'src/components/__tests__/CategoryTotalsGrid.test.tsx'; npx --no-install stryker run --mutate src/components/CategoryTotalsGrid.tsx --disableBail --concurrency 2 --force`,
     then `pwsh ../scripts/mutation-test-value.ps1 -Report reports/mutation/mutation-report.json -Tests 'CategoryTotalsGrid'`.
     `STRYKER_TEST_INCLUDE` (comma-separated globs) widens `vitest.mutation.config.ts` beyond the
     utils and hooks tests; `--force` stops incremental mode reusing an earlier slice's results.
2. **Report** with `scripts/mutation-test-value.ps1`: survivors and uncovered mutants by file;
   tests in scope that run the mutated code but kill nothing; tests in scope whose every kill
   another test also makes. `-Tests` (a regex on the test name) keeps the last two lists to the
   slice's own tests.
3. **Triage** each survivor as one of:
   - *missing/weak test* — add or strengthen the test;
   - *equivalent mutant* — record the count in this file, change nothing (no `// Stryker disable`
     comments);
   - *log or exception wording* — record the count; the "started"/"completed" information logs
     and message text are not asserted (the testing guide asserts spans, and logged failures by
     exception type only);
   - *dead code* — remove it in its own PR, never mixed with test changes.
4. **Prune** low-value and duplicated tests (merge into a `[Theory]` / `it.each`, or delete).
   The redundancy list holds candidates, not verdicts: every test that builds the entity kills
   the same constructor mutants, and the two sides of a rule (`IsInvestment_…_IsTrue` /
   `…_IsFalse`) kill the same mutant while each pins a different case. Remove a test only when
   it proves nothing the covering test does not, per the testing guide's §2.
5. **Confirm** by re-running the same slice; record the score before and after.
6. **Ship** one PR per slice, within the PR size target. The PR updates this file's row.

Slices are sized for one sitting: at most ~30 minutes of mutation run time. A slice that runs
longer is split before it is worked.

**Timeouts are not kills you can trust.** Stryker counts a timed-out mutant as detected. On
this machine StrykerJS's default 7 runners plus 8 type checkers timed out 14 of 20 mutants of
`CategoryTotalsGrid.tsx` and reported 100%; with `--concurrency 2` none timed out. The value
script warns when more than 10% of mutants time out; re-run with fewer processes before
recording a score.

## Branches and folders

The review runs alongside other work (a bug fix on `main` in the repo folder), so it never
uses the repo folder.

- Tooling and this tracker: branch `test/mutation-review` in the worktree
  `../financial-wt-mutation-review`, removed after batch 0 merged (#1283).
- Each slice that changes tests or code: its own branch off `main` in its own worktree
  (`git worktree add ../financial-wt-mut-<slice> -b test/mutation-<slice> origin/main`), removed
  after its PR merges. Stryker runs happen in that slice's worktree.
- Stryker output (`TestResults/`, `StrykerOutput/`, `Financial.Web/reports/`) is ignored by git.
- A new worktree needs `npm ci` in `Financial.Web` before any web run (about 12 minutes on this
  machine). Always call the project's StrykerJS through `npx --no-install` or `npm run`: a bare
  `npx stryker` with no `node_modules` downloads an unrelated, deprecated `stryker` package.

## Progress

### Batch 0 — Tooling

| # | Task | Status | PR | Notes |
|---|---|---|---|---|
| 0.1 | `scripts/stryker.ps1` accepts any production project (Application, Infrastructure, Shared, Api), `-DisableBail` and `-Tests` | ✅ | [#1283](https://github.com/ggibellato/Financial/pull/1283) | Trial: `Expense.cs` with bail off, 152 mutants in 36 s. Local runs pass `--break-at 0` so a low score still prints the report |
| 0.2 | `scripts/mutation-test-value.ps1` reads a Stryker JSON report (.NET or JS) and prints survivors by file, idle tests and redundancy candidates | ✅ | [#1283](https://github.com/ggibellato/Financial/pull/1283) | `[Theory]` rows merged by name; timeout warning added |
| 0.3 | StrykerJS: check `disableBail` and a per-slice `--mutate` run work for `.tsx` files | ✅ | [#1283](https://github.com/ggibellato/Financial/pull/1283) | `STRYKER_TEST_INCLUDE`; needs `--concurrency 2` here |
| 0.4 | Measure run time per project and correct the slice sizes below | ✅ | [#1283](https://github.com/ggibellato/Financial/pull/1283) | See "Local baseline"; no slice needed splitting; 2.3 skipped; `Program.cs` added to 9.1 |
| 0.5 | Ignore `StrykerOutput/` under `Tests/` | ⏭️ | | Already ignored (`StrykerOutput/` in `.gitignore`) |

### Slices

| # | Project | Slice (`--mutate`) | Status | Score before | Score after | Tests added | Tests removed | Equivalent | PR | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| 1.1 | CashFlow.Domain | `Entities/**` | ⬜ | 90.3% | | | | | | Trial on `Expense.cs`: 14 undetected (round-up suggestion, L43–45) |
| 1.2 | CashFlow.Domain | `Rules/**`, `ValueObjects/**` | ⬜ | 91.1% | | | | | | |
| 2.1 | Investment.Domain | `Entities/**` | ⬜ | 82.0% | | | | | | |
| 2.2 | Investment.Domain | `Rules/**` | ✅ | 74.5% | 87.7% | 15 | 3 | ~35 | [#1284](https://github.com/ggibellato/Financial/pull/1284) | See "Slice notes"; 734 → 754 tests |
| 2.3 | Investment.Domain | `ValueObjects/**`, `Exceptions/**` | ⏭️ | — | | | | | | No mutants generated |
| 3.1 | CashFlow.Application | Accounts: `AccountAdminService`, `AccountGroupSummaryService`, `AccountQueryService`, `AccountReferenceReader`, `AccountTransactionReader`, `AccountTransferService`, `BalanceAdjustmentService` | ⬜ | 70.3% | | | | | | 85 compile errors |
| 3.2 | CashFlow.Application | Cards: `CardStatementCreation`, `CardStatementService`, `CreditCardAccounts`, `StatementDueDateService`, `PaymentsDueService` | ⬜ | 67.3% | | | | | | |
| 3.3 | CashFlow.Application | Calendar: `CalendarIntegrationService`, `CreditCardCalendarSyncService`, `CreditCardCalendarSyncStatusStore` | ✅ | 54.9% | 66.7% | 6 | 4 | 31 | [#1291](https://github.com/ggibellato/Financial/pull/1291) | See "Slice notes"; `BestEffortDisconnectRemoteAsync` returns a value nobody reads |
| 3.4 | CashFlow.Application | Movements: `ExpenseService`, `IncomeService`, `IncomeSourceService`, `IncomeSummaryService`, `IncomeGroupLookupBuilder`, `CategoryService`, `CategorySummaryService`, `HistoricAverageService`, `MensaisService` | ⬜ | 63.5% | | | | | | `MensaisService` 46%; `EnsureStatementsOpenForMove` unmeasured |
| 3.5 | CashFlow.Application | Reserve and settlement: `ReserveService`, `ReserveSplitRule`, `PotsService`, `TitheService`, `SettlementService`, `CurrencyBalanceMapper` | 🔄 | 63.2% | 76.2% | 7 | 15 | 24 | | See "Slice notes"; 15 tests that re-checked Domain pot, tithe and settlement rules or duplicated others removed |
| 3.6 | CashFlow.Application | Investment accounts: `InvestmentAccountService`, `InvestmentAnnualResultService`, `InvestmentSnapshotService` | ⬜ | 63.2% | | | | | | |
| 3.7 | CashFlow.Application | `Validation/**` | ⬜ | 96.4% | | | | | | Likely review-only |
| 4.1 | Investment.Application | Assets and transactions: `AssetAdminService`, `AssetMoveService`, `AssetMutationHelper`, `TransactionService`, `CreditService`, `DividendService`, `FxEntryCaptureHelper` | ✅ | 47.7% | 65.0% | 11 | 9 | 16 | [#1286](https://github.com/ggibellato/Financial/pull/1286) | See "Slice notes"; 48.0% when run with `-AlsoTestedBy Investment.Infrastructure` |
| 4.2 | Investment.Application | Corporate actions: `CorporateActionService`, `BrokerCurrencyParser` | ✅ | 49.6% | 70.3% | 7 | 1 | 11 | [#1287](https://github.com/ggibellato/Financial/pull/1287) | See "Slice notes"; `BrokerCurrencyParser` is only used by 4.3/4.4 services |
| 4.3 | Investment.Application | Summaries: `PortfolioService`, `PortfolioAssetSummary*`, `PortfolioDashboard*`, `SummaryService`, `ConvertedSummaryBuilder`, `ConvertedAssetFlows`, `AssetTotals`, `AssetAmountBases`, `AssetCashFlowBuilder` | ✅ | 56.8% | 73.7% | 15 | 31 | 30 | [#1289](https://github.com/ggibellato/Financial/pull/1289) | See "Slice notes"; 28 asset-summary tests that re-checked Domain results removed |
| 4.4 | Investment.Application | Breakdowns and returns: `AllocationBreakdown*`, `BrokerBreakdown*`, `PortfolioXirrBuilder`, `XirrCalculationService`, `HoldingValuationService`, `CurrencyConversionContext` | ✅ | 70.2% | 78.5% | 3 | 10 | 5 | [#1290](https://github.com/ggibellato/Financial/pull/1290) | See "Slice notes"; 10 tests that re-checked Domain XIRR and valuation results removed |
| 4.5 | Investment.Application | Prices, income, tax: `AssetPriceHistoryService`, `AssetPriceLookupService`, `UpcomingIncome*`, `TaxRuleService`, `TaxWorkbookService`, `DataQualityReportService` | ✅ | 26.7% (47.7% with the Infrastructure tests) | 77.5% | 8 | 16 | 11 | [#1285](https://github.com/ggibellato/Financial/pull/1285) | See "Slice notes"; run with `-AlsoTestedBy Investment.Infrastructure` |
| 4.6 | Investment.Application | `NavigationMapper`, `NavigationService`, `BrokerService`, `ReportingCurrencySettingService`, `Validation/**` | ✅ | 52.7% | 74.5% | 9 | 1 | 6 | [#1288](https://github.com/ggibellato/Financial/pull/1288) | See "Slice notes"; 55.3% when run with `-AlsoTestedBy Investment.Infrastructure` |
| 5.x | Financial.Web | `src/hooks` — slices of ~10 files, worst score first, taken from the nightly `stryker-js` report (no local run needed to triage) | ⬜ | 65.2% | | | | | | Slice list made when the batch starts |
| 6.1 | Financial.Web | `src/utils/**` | ⬜ | | | | | | | |
| 7.1 | Shared.Abstractions | all | ⬜ | 63.9% | | | | | | `ReflectionJsonTypeInfoHelpers` 10 of 10 undetected |
| 7.2 | Shared.Infrastructure | all | ⬜ | 74.3% | | | | | | `DebouncedJsonStorage` 19 of 55 undetected |
| 7.3 | CashFlow.Infrastructure | all | ⬜ | 80.2% | | | | | | Slowest per mutant (7.2 min) |
| 7.4 | Investment.Infrastructure | all | ⬜ | 81.1% | | | | | | DI file 11 of 19 undetected |
| 8.x | Financial.Web | `src/components`, `src/pages` — only files holding logic (forms, totals, validation, filtering); presentational files skipped | ⬜ | | | | | | | Trial on `CategoryTotalsGrid.tsx`: 57.1%, 6 text/label mutants survive |
| 9.1 | Financial.Api | `Program.cs`, `Middleware/**`, `Helpers/**` | ⬜ | 71.6% | | | | | | Middleware and helpers already 100%; `Program.cs` 23 of 64 undetected |
| 9.2 | Financial.Api | Controllers with branching only | ⬜ | 78.7% | | | | | | Thin controllers skipped; `CalendarIntegrationController` 8 of 18 undetected |

### Batch 10 — Keep it continuous

| # | Task | Status | PR | Notes |
|---|---|---|---|---|
| 10.1 | Add each reviewed project to the nightly `stryker-dotnet` matrix with its own break threshold | ⬜ | | |
| 10.2 | Widen the nightly StrykerJS `mutate` to the reviewed components | ⬜ | | |
| 10.3 | Raise the existing break thresholds once scores hold for a week (`docs/ci-affected-pipeline.md`) | ⬜ | | |
| 10.4 | Decide on an advisory PR job mutating changed files only (`--since origin/main`) | ⬜ | | |

## Slice notes

### 2.2 Investment.Domain rules (74.5% → 87.7%)

Gaps closed:

- `CreditsAnalysisCalculator` had no Domain test at all (31 uncovered mutants; only reached through
  the Application's `PortfolioAssetSummaryBuilder`): new `CreditsAnalysisCalculatorTests`.
- `CreditFrequencyAnalyzer`: every band limit (1.5, 3.5, 5.0 months), two months, a year end.
- `XirrCalculator`: an empty series, same-day flows summing to zero, amounts too large for the
  absolute NPV tolerance (the 200-iteration fallback had no test).
- `DividendAttributionCalculator`: a buy on the credit date, two lots at different costs, a
  holding written down to zero.
- `DisposalRecordCalculator`: a FIFO sale covered by the oldest lot, a SpecificId sale split
  across two lots.
- `DisposalRecordRegenerator`: a sale with no record yet (created with its classification), a
  seeded SpecificId sale.
- Backfills: an already-classified disposal is skipped on a second run; shares summed over two
  lots.

Removed or merged: two `DividendAttributionCalculatorTests` that repeated another test's guard
or case; the all-positive and all-negative XIRR tests merged into one theory.

The 57 still undetected, all reviewed:

- **Exception-message text** (about 12): not asserted by design (messages embed figures).
- **Unreachable code** (6) in `XirrCalculator`: the `ToDecimalOrNull` overflow clamp (L169–170) —
  the probe ladder keeps every rate between -1 and 1e10, far inside `decimal` — and the
  `growthFactor <= 0` guard (L147–148). Candidate for a separate clean-up PR.
- **Failure-isolation catches** (4) in the three backfills: no entity accepts the data that would
  make one record's calculation throw, so they are untested.
- **Equivalent** (the rest): the XIRR sign-change and date guards shortcut what the bracket search
  concludes anyway; negating the NPV keeps its roots; `<`/`<=` where equality cannot occur;
  hash-code and comparer changes that only affect collisions; `Month + 1` shifts every month
  alike; `DisposalRecordRegenerator`'s final `RefreshRealizedCapitalGain` repeats what
  `AppendBackfilledDisposalRecord` already did.

### 4.5 Investment.Application prices, income and tax (47.7% → 77.5%)

The 26.7% baseline left out the two price services' tests, which live in
`Financial.Investment.Infrastructure.Tests`; with them (`-AlsoTestedBy`) the slice started at 47.7%.

Gaps closed:

- **Spans.** No test in the slice checked a span's result, so deleting `MarkSuccess`, a whole
  `catch`, or the span name went unnoticed. Each operation now has one success and one failure
  assertion (`ShouldHaveSucceededSpan`, new beside `ShouldHaveFailedSpan`). For
  `AssetPriceLookupService` this matters most: a write that succeeded could have been reported as
  failed.
- **`AssetPriceLookupService`.** When the live fetch fails and an earlier price is reused, nothing
  is recorded for today. When there is no price at all, the result is Unknown and not manual, and
  only a warning is logged. The fetch request carries the holding's name only when the caller sent
  none. A recorded automatic price has no currency. A failed fetch with no asset context is
  rethrown.
- **`AssetPriceHistoryService`.** Two tests only checked that a result came back. They now check
  the stored entry (price, manual flag, currency, replacement).
- **Ordering.** `DataQualityReportService` sorts its seven finding lists by broker, portfolio and
  asset, and the web shows them in that order; one theory now covers all seven.
  `TaxWorkbookService` options are now checked in jurisdiction-then-year order.
- **Historic holdings.** A workbook entry and an option from a historic broker.
- `UpdateTaxRuleAsync(null)` throws `ArgumentNullException`.

Removed or merged:

- `UpcomingIncomeBuilderTests` (5) repeated `UpcomingIncomeServiceTests` case for case. The
  builder's one caller is the service.
- The three `TaxRuleServiceTests` constructor tests repeat `ConstructorGuardTests`, which sweeps
  every constructor in the assembly (testing guide, application services).
- Merged into theories: the monthly, quarterly and four-monthly payers; the three no-frequency
  cases; zero price and future date; ProviderValue and Manual holdings.
- Merged into one test: the no-history warning and result tests; the three live-write tests.

The 59 still undetected, all reviewed:

- **Log and exception wording** (48): "started"/"completed" information logs (44), warning text
  (3), and one exception message.
- **Equivalent** (11): `ConfigureAwait(false)` → `true` (6). Unreachable `?? string.Empty` after
  `DescribeWith` has already filled the name (2). `FirstOrDefault` → `First` for a classification's
  source, which always exists while the classification is active (2). `ResolveAsset`'s early
  return before a repository lookup that returns null anyway (1).

For later slices: per-class constructor-null tests that `ConstructorGuardTests` already covers
remain in `PortfolioServiceTests`, `CashFlowJsonRepositoryTests` and three
Shared.Infrastructure test classes.

### 4.1 Investment.Application assets and transactions (48.0% → 65.0%)

Run with `-AlsoTestedBy Investment.Infrastructure`: `TransactionService` and `CreditService` also
have tests there.

Gaps closed:

- **Updates keep the stored currency.** The two tests that said so stored BRL, which is also the
  enum's default, so dropping `existing?.Currency` went unnoticed in both services. They now store
  USD.
- **Credits by portfolio**: newest first, and a blank broker alone returns nothing (`||` → `&&`
  survived because the repository stub held no credits).
- **`AssetAdminService`**: an explicit class on create, an invalid ISIN and a blank current name on
  update, null requests.
- **`TransactionService`**: adding for an unknown broker returns null and writes nothing; the
  transaction-type effects list.
- **`DividendService`**: year totals newest year first.
- **Spans**: success and failure for each operation, and the entity id on update and delete.

Removed or merged:

- The four `AssetMoveServiceTests` constructor tests (covered by `ConstructorGuardTests`).
- Four "add a credit with field X" tests merged into one that sends every field; two update tests
  likewise; the Application-side negative-value test, which the Infrastructure test repeats with
  persistence.

The 116 still undetected, all reviewed:

- **Log and exception wording** (100): the "started"/"completed" information logs are 93 of them
  (their statements and their text), which is why this slice stays at 65%. Seven are exception
  messages, two of them on read-backs that cannot fail.
- **Equivalent** (16): `ConfigureAwait(false)` → `true` (15), and `AssetMutationHelper`'s early
  return before a lookup that returns null anyway.

### 4.2 Investment.Application corporate actions (49.6% → 70.3%)

Gaps closed:

- **Rollback of a failed merger or spin-off update.** The update retracts both linked records and
  re-records them, undoing each step if a later one fails. No test reached the undo path. An
  invalid new exchange ratio or allocation now checks that both records come back unchanged and
  nothing is written.
- **Two mergers into one target.** Updating or deleting the second merger must touch only its own
  record on the target. With one merger per target, the linked-record lookup could drop its
  correlation-id condition (`&&` → `||`) and still pass.
- **The "empty id or blank holding" guard** on merger and spin-off updates (`||` → `&&`).
- **A merger target created inline**: its ISIN, exchange, ticker, country, type code and class
  (entered or derived), and empty values when none are given.
- The carried cost basis after a merger update, and the save on both updates.
- **Spans**: success on every path, failure on add merger, add spin-off, the update rollbacks and
  delete, and the entity id on update and delete.

Removed: `AddSplitAsync_InvalidRatioFactor_ThrowsAndWritesNothing`, which killed no mutant. The
service has no ratio check of its own; `CorporateActionTests.CreateSplit_InvalidRatio_...` covers
the domain rule.

The 79 still undetected, all reviewed:

- **Log and exception wording** (65): 53 from the "started"/"completed" logs, 12 exception
  messages (one in `BrokerCurrencyParser`, whose callers belong to slices 4.3 and 4.4).
- **Equivalent** (11): `ConfigureAwait` (7); `FirstOrDefault` → `First` on the linked-record
  lookups, which throw the same exception type either way (3); a default type code that resolves
  to the same class (1).
- **Unreachable** (2): the undo step for the second new record failing after the first succeeded,
  when both are built from the same validated values.
- **Left untested** (1): the same lookup for two spin-offs into one new asset, which does not
  happen in practice.

### 4.6 Investment.Application navigation, brokers and validation (55.3% → 74.5%)

Run with `-AlsoTestedBy Investment.Infrastructure`: `NavigationService` also has tests there.

Gaps closed:

- **Which broker's cost basis method asset details show.** The lookup takes the active broker with
  the asset's broker name, then a historic one. No test had two brokers, so matching any broker,
  dropping either side of the fallback, or preferring historic over active all passed, and the
  historic fallback never ran. One test has a different active broker plus a historic broker with
  the same name; another prices a historic-only broker.
- **Tax jurisdictions** in asset details are now checked in order, not only as a set, with the
  classifications added out of order so the sort is what puts them right.
- **Navigation tree display names** ("Broker (BRL)", "Portfolio (1 assets)") and **disposal-record
  lots**, tested on `NavigationMapper` directly rather than through the service, so they do not
  depend on repository fixtures or on how the domain builds lots.
- **`BrokerService`**: success and failure spans for every operation, null requests, a blank
  current name on update.
- **`NavigationService`**: success spans on the early returns (blank or unknown asset) and on open
  lots; the tree test now has two brokers, checks one child each in name order, and uses
  `ShouldHaveSucceededSpan`, keeping the entity-type check. The older single-broker cost basis test
  was removed: the new active-broker test checks the same and more.

`IsinValidator`, `AssetContextValidator` and `RequiredValueValidator` have no tests of their own;
the service tests cover them.

Open question for a separate change: `GetAssetDetails` picks the broker for the cost basis method
from the active list first, then the historic one, whatever scope was asked for, while
`BrokerService` uses the domain's `Investments.GetBroker(name, preferHistoric)`. A historic asset
whose broker name is also an active broker would show the active broker's method. The tests here
pin only the active-scope case.

Also for later: every service method repeats the same span block (start, `MarkSuccess`,
`MarkFailed` and rethrow), so each operation needs its own success and failure test to kill the
same mutants. One wrapper in `Shared.Abstractions` that owns that block would be tested once, and
service tests would only check the span name. A production change, not part of a test slice.

The 48 still undetected, all reviewed:

- **Log and exception wording** (42): 40 from the "started"/"completed" logs, two exception
  messages (`InvestmentScopeParser`, `RequiredValueValidator`).
- **Equivalent** (6): `ConfigureAwait` (4); the historic broker lookup cannot miss once the asset
  was found under that broker name (1); `AssetContextValidator`'s `||` → `&&` between broker and
  portfolio, because a blank name reaches a repository lookup that returns null anyway (1).

### 4.3 Investment.Application summaries and dashboard (56.8% → 73.7%)

Gaps closed:

- **Converted figures.** With a constant rate the converted returns must equal the native ones, so
  comparing them (with a withheld dividend in the holding) now catches a dropped transaction,
  credit, gross or net flow. Converted unrealised gain, converted market value summed across
  holdings, and converted invested as bought less sold are checked, as is the partial flag when
  some records or one broker currency fail to convert.
- **Edges**: a broker with no holdings reports a market value of 0, not unknown; a credit on
  1 January counts towards this year's income; an empty dashboard is not "currency unavailable";
  an unrecognised broker currency fails the span.
- **Guards and spans** on `SummaryService`, `PortfolioService` (null requests, blank current name)
  and `PortfolioAssetSummaryService`, including the blank-name guards seeded so they discriminate.
- **Cash flows** returned in date order by every builder.

Removed, after the simplify review's altitude check:

- 28 `PortfolioAssetSummaryServiceTests` that only re-checked `CreditsAnalysisCalculator`,
  `CreditFrequencyAnalyzer` and the asset's own totals (frequency bands, last-month and
  current-month credits, annual estimates, realised gain). `CreditFrequencyAnalyzerTests` and
  `CreditsAnalysisCalculatorTests` own those rules. One test now checks that the service passes
  the right basis and the clock's date to the calculator and maps every field; another keeps the
  fully-sold-holding yield case. The score rose with them gone.
- The three `PortfolioServiceTests` constructor tests (covered by `ConstructorGuardTests`).

The two methods Stryker cannot measure (Safe Mode) were read: `SumActiveScope` is covered by the
dashboard market value, invested, unpriced and unvalued-count tests; `ConvertedAssetFlows`'
transaction loop by `ConvertedSummaryBuilderTests.BuildAsync_SomeRecordsFailToConvert_...`.

The 81 still undetected, all reviewed:

- **Log and exception wording** (51).
- **Equivalent** (30): `ConfigureAwait` (24); the early return when no holding is priced (it
  would only convert a zero); `AttemptCount >= 0` (an unavailable context has always attempted a
  lookup); the historic valuation in the dashboard (a historic holding never has a terminal
  value); `holdingCount >= 0` (no holdings, nothing to compute); `as IReadOnlyList ?? ToList`; the
  price-only flow sort (transactions are stored in date order).

### 4.4 Investment.Application breakdowns and returns (70.2% → 78.5%)

Gaps closed:

- **Spans** on `BrokerBreakdownService` (success on every path, and a failed span when the
  repository throws) and on `AllocationBreakdownService`.
- **Allocation flags**: a converted breakdown is neither partial nor unavailable when every rate
  is found, and when nothing is priced (no conversion is attempted, so no context is unavailable).
- **Gross and net XIRR wiring**: `PortfolioXirrBuilder.Calculate` with a withheld dividend gives a
  lower net rate than gross, which pins the gross series to the credit values and the net series
  to the net credit amounts. Nothing checked this before.
- **Clock wiring in `HoldingValuationService`**: a price older than the previous weekday is
  stale against the clock's date, and a withheld dividend gives a lower net-of-tax total return
  than gross (it only checked they differed). Stryker has no mutant for passing the wrong date,
  so this test does not move the score.

Removed, after the simplify review's altitude check (`XirrCalculatorTests`,
`HoldingValuationCalculator` and the asset's own tests own these rules):

- Three `XirrCalculationServiceTests` that re-solved XIRR (10% growth, no flows, a loss). The two
  kept pin the service's own choices: the terminal value dated on the given day, and the clock
  default.
- `PortfolioXirrBuilderTests.Calculate_ReturnsNullWhenNoHoldingContributesAnInflow` (a Domain rule).
- Six `HoldingValuationServiceTests`: the provider-value method and the "price recorded before
  today" cases, the null-asset throw (the failed-span test covers it), the separate no-market-value
  net return test (merged into the unavailable test), and two historic tests merged into
  `GetValuation_Historic_IgnoresAnyRecordedPrice`, which now also checks a closed holding still
  gets a total return. Assertions on values the Domain computes (unrealised gain, cost of units,
  market status) were trimmed.

The 26 still undetected, all reviewed:

- **Log and exception wording** (21), including the `BrokerCurrencyParser` message.
- **Equivalent** (5): `ConfigureAwait` (4); `AttemptCount >= 0` (an unavailable context has
  always attempted a lookup).

Investment.Application is now done slice by slice (4.1 to 4.6).

### 3.3 CashFlow.Application calendar (54.9% → 66.7%)

Gaps closed:

- **Spans** on every early return of `CalendarIntegrationService` (status, disconnect, access
  token, authorisation URL) and of the card sync (not connected, no access token, revoked while
  refreshing), with the card id on the sync span and a failed span when the sync throws.
- **Pending OAuth state**: still accepted at exactly 10 minutes, rejected one second later, and
  rejected the second time it is used.
- **Token refresh**: refreshes when the token expires exactly at the 1-minute skew, keeps the
  stored refresh token when the provider issues none, and persists a rotated one.
- **Card sync**: an unknown card in `TriggerSyncIfInvoiceSynced` does nothing; an invoice later in
  the due-date month still syncs (the month, not the day, is compared); a sync with no connection
  never asks for an access token; the event description is pinned in full, with an amount over
  1,000 so the `N2` format is checked.

Removed or merged, after the simplify review's altitude check:

- `ResyncAsync_CreditFromAnEarlierMonth_LowersTheAmountInTheEventTitle` only re-checked
  `CardStatementService` credit netting (`CardStatementServiceTests` owns it), and its
  "no charges posted" check looked for text the code no longer writes.
- `GetSyncStatuses_ReflectsWhatHasBeenTracked` is contained in the locked-file test.
- The "second sync does not reconcile" test merged into the "second sync updates the same event"
  test; the two invoice-month tests became one theory; the expired-token and skew tests became one
  theory. The title check on the recreated event (CardStatementService arithmetic) was dropped.

`RecreateCalendarAndResyncAllLockedAsync` (Safe Mode, unmeasured) was read: the three self-heal
tests cover recreating the calendar, re-syncing every qualifying card and keeping the old mapping
when recreation fails.

The 82 still undetected, all reviewed:

- **Log and exception wording** (50).
- **Equivalent** (31): `ConfigureAwait` (25); `BestEffortDisconnectRemoteAsync`'s result (4: its
  only caller discards the return value, so the method could return nothing); the catch on a
  locked connection file (Stryker's removal returns `default`, the same null); the null-card guard
  in `TriggerSyncIfInvoiceSynced` (the stub repository returns no due date for a null card).
- **Unreachable** (1): the early return in `RemoveEventMapping` (it is only called when the
  mapping exists).

### 3.5 CashFlow.Application reserve and settlement (63.2% → 76.2%)

Gaps closed:

- **Spans** with the entity id on every `PotsService` operation (deposit, withdrawal, their
  updates, get, delete, replace pots, get pots), `ReserveService.DeleteMovement`, and every
  `SettlementService` write; `TitheService` success spans.
- **Null requests** on every pots and settlement write method throw `ArgumentNullException`
  rather than a null reference later.
- **Tithe**: an out-of-range month throws for the month (not the date) before the start month is
  saved, and months 1 and 12 are accepted; years 1 and 9999 are accepted; the first call saves this
  month as the carry-forward start once; two tithe payments in a month are added together;
  updating one month's decision leaves another month of the same year alone.

Removed or merged, after the simplify review's altitude check (`Financial.CashFlow.Domain.Tests`
owns these rules):

- Nine `PotsServiceTests` that only re-checked `PotDeposit`, `PotWithdrawal` and the split
  allocator (thirds split, inactive pot left out, no active pots, pots not totalling 100, amount
  not above zero for deposits and withdrawals, a bank withdrawal from a pots account without a
  category), plus a deposit get and a deposit delete that repeated the withdrawal ones.
- Four `TitheServiceTests`: the counts-as-tithe-true case (same input as the subtraction test), the
  non-tithe category with counts-as-tithe false (cannot catch what the non-tithe test catches), the
  bank-less income test (a subset of another), and the empty-month test merged with its twin.
- The reserve update span test merged into the update test; the settlement currency theory's
  "account doesn't use BRL" row (a `SettlementTransaction` rule).

The 84 still undetected, all reviewed:

- **Log and exception wording** (60).
- **Equivalent** (24): `ConfigureAwait` (19); `PotsService`'s `EnsureCanReplacePots` and the
  withdrawal's description check (the Domain repeats both, so removing the service's call throws
  the same error); three `TitheService` comparisons (`first > anchorMonth`, `previousBalance > 0`,
  `carryAmount > 0`) that give the same result at the boundary.

## Log

One line per working session: date, slice, outcome.

| Date | Slice | Outcome |
|---|---|---|
| 2026-11-14 | — | Plan written; baseline recorded from nightly run 43584910403 |
| 2026-11-14 | 0 | Tooling built and tried on `Expense.cs` and `CategoryTotalsGrid.tsx`; every .NET project measured with bail off (local baseline); merged in #1283 |
| 2026-11-14 | 2.2 | Rules 74.5% → 87.7%; 15 tests added, 3 removed or merged, 2 strengthened; unreachable XIRR clamp found |
| 2026-11-14 | 4.5 | Prices, income, tax 47.7% → 77.5% (26.7% without the Infrastructure tests); span assertions added, 16 duplicated tests removed or merged; `-AlsoTestedBy` added to `stryker.ps1` |
| 2026-11-15 | 4.1 | Assets and transactions 48.0% → 65.0%; currency-preservation tests fixed, spans and gaps added, 9 duplicated tests removed or merged; most of the rest is log wording |
| 2026-11-15 | 4.2 | Corporate actions 49.6% → 70.3%; update rollback, two-mergers-one-target and inline-target identity tested; 1 idle test removed |
| 2026-11-15 | 4.6 | Navigation, brokers, validation 55.3% → 74.5%; cost-basis broker lookup, display names, lot mapping and spans tested |
| 2026-11-15 | 4.3 | Summaries and dashboard 56.8% → 73.7%; converted figures and edges tested; 28 asset-summary tests that re-checked Domain results removed |
| 2026-11-15 | 4.4 | Breakdowns and returns 70.2% → 78.5%; spans, allocation flags and gross/net XIRR wiring tested; 10 tests that re-checked Domain results removed |
| 2026-11-15 | 3.3 | Calendar 54.9% → 66.7%; spans, OAuth state expiry and reuse, refresh skew and rotation, invoice-month boundary tested; 4 tests removed or merged |
| 2026-11-15 | 3.5 | Reserve and settlement 63.2% → 76.2%; spans, null requests and tithe boundaries tested; 15 tests that re-checked Domain rules or duplicated others removed |
