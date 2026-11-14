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
  `Financial.Investment.Infrastructure.Tests`, which a per-project run does not use. Decide in
  slice 4.5: move the tests, or mutate with both test projects.
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
     (reports under `TestResults/stryker/<target>/<timestamp>/`).
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
| 2.2 | Investment.Domain | `Rules/**` | 🔄 | 74.5% | 87.7% | 15 | 3 | ~35 | | See "Slice notes"; 734 → 754 tests |
| 2.3 | Investment.Domain | `ValueObjects/**`, `Exceptions/**` | ⏭️ | — | | | | | | No mutants generated |
| 3.1 | CashFlow.Application | Accounts: `AccountAdminService`, `AccountGroupSummaryService`, `AccountQueryService`, `AccountReferenceReader`, `AccountTransactionReader`, `AccountTransferService`, `BalanceAdjustmentService` | ⬜ | 70.3% | | | | | | 85 compile errors |
| 3.2 | CashFlow.Application | Cards: `CardStatementCreation`, `CardStatementService`, `CreditCardAccounts`, `StatementDueDateService`, `PaymentsDueService` | ⬜ | 67.3% | | | | | | |
| 3.3 | CashFlow.Application | Calendar: `CalendarIntegrationService`, `CreditCardCalendarSyncService`, `CreditCardCalendarSyncStatusStore` | ⬜ | 54.9% | | | | | | 87 compile errors; `RecreateCalendarAndResyncAllLockedAsync` unmeasured |
| 3.4 | CashFlow.Application | Movements: `ExpenseService`, `IncomeService`, `IncomeSourceService`, `IncomeSummaryService`, `IncomeGroupLookupBuilder`, `CategoryService`, `CategorySummaryService`, `HistoricAverageService`, `MensaisService` | ⬜ | 63.5% | | | | | | `MensaisService` 46%; `EnsureStatementsOpenForMove` unmeasured |
| 3.5 | CashFlow.Application | Reserve and settlement: `ReserveService`, `ReserveSplitRule`, `PotsService`, `TitheService`, `SettlementService`, `CurrencyBalanceMapper` | ⬜ | 63.2% | | | | | | `ReserveService` 38 compile errors |
| 3.6 | CashFlow.Application | Investment accounts: `InvestmentAccountService`, `InvestmentAnnualResultService`, `InvestmentSnapshotService` | ⬜ | 63.2% | | | | | | |
| 3.7 | CashFlow.Application | `Validation/**` | ⬜ | 96.4% | | | | | | Likely review-only |
| 4.1 | Investment.Application | Assets and transactions: `AssetAdminService`, `AssetMoveService`, `AssetMutationHelper`, `TransactionService`, `CreditService`, `DividendService`, `FxEntryCaptureHelper` | ⬜ | 47.7% | | | | | | |
| 4.2 | Investment.Application | Corporate actions: `CorporateActionService`, `BrokerCurrencyParser` | ⬜ | 49.6% | | | | | | `CorporateActionService` 133 of 260 undetected |
| 4.3 | Investment.Application | Summaries: `PortfolioService`, `PortfolioAssetSummary*`, `PortfolioDashboard*`, `SummaryService`, `ConvertedSummaryBuilder`, `ConvertedAssetFlows`, `AssetTotals`, `AssetAmountBases`, `AssetCashFlowBuilder` | ⬜ | 56.8% | | | | | | |
| 4.4 | Investment.Application | Breakdowns and returns: `AllocationBreakdown*`, `BrokerBreakdown*`, `PortfolioXirrBuilder`, `XirrCalculationService`, `HoldingValuationService`, `CurrencyConversionContext` | ⬜ | 69.6% | | | | | | |
| 4.5 | Investment.Application | Prices, income, tax: `AssetPriceHistoryService`, `AssetPriceLookupService`, `UpcomingIncome*`, `TaxRuleService`, `TaxWorkbookService`, `DataQualityReportService` | ⬜ | 26.7% | | | | | | The two price services are tested from `Investment.Infrastructure.Tests` |
| 4.6 | Investment.Application | `NavigationMapper`, `NavigationService`, `BrokerService`, `ReportingCurrencySettingService`, `Validation/**` | ⬜ | 52.7% | | | | | | |
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

## Log

One line per working session: date, slice, outcome.

| Date | Slice | Outcome |
|---|---|---|
| 2026-11-14 | — | Plan written; baseline recorded from nightly run 43584910403 |
| 2026-11-14 | 0 | Tooling built and tried on `Expense.cs` and `CategoryTotalsGrid.tsx`; every .NET project measured with bail off (local baseline); merged in #1283 |
| 2026-11-14 | 2.2 | Rules 74.5% → 87.7%; 15 tests added, 3 removed or merged, 2 strengthened; unreachable XIRR clamp found |
