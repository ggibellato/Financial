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

## How one slice is worked

1. **Run** Stryker on the slice only (`--mutate`), against the project's own test project.
   Analysis runs add `-DisableBail` so the report lists every test that kills each mutant.
2. **Report** with `scripts/mutation-test-value.ps1` (batch 0): survivors and uncovered mutants
   by file; tests with no kill in scope; tests whose kill set is covered by another test.
3. **Triage** each survivor as one of:
   - *missing/weak test* — add or strengthen the test;
   - *equivalent mutant* — record the count in this file, change nothing (no `// Stryker disable`
     comments);
   - *dead code* — remove it in its own PR, never mixed with test changes.
4. **Prune** low-value and duplicated tests (merge into a `[Theory]` / `it.each`, or delete).
5. **Confirm** by re-running the same slice; record the score before and after.
6. **Ship** one PR per slice, within the PR size target. The PR updates this file's row.

Slices are sized for one sitting: at most ~30 minutes of mutation run time. A slice that runs
longer is split before it is worked.

## Branches and folders

The review runs alongside other work (a bug fix on `main` in the repo folder), so it never
uses the repo folder.

- Tooling and this tracker: branch `test/mutation-review` in the worktree
  `../financial-wt-mutation-review`, kept until batch 0 merges.
- Each slice that changes tests or code: its own branch off `main` in its own worktree
  (`git worktree add ../financial-wt-mut-<slice> -b test/mutation-<slice> origin/main`), removed
  after its PR merges. Stryker runs happen in that slice's worktree.
- Stryker output (`StrykerOutput/`, `Financial.Web/reports/`) is never committed.

## Progress

### Batch 0 — Tooling

| # | Task | Status | PR | Notes |
|---|---|---|---|---|
| 0.1 | `scripts/stryker.ps1` accepts any production project (Application, Infrastructure, Shared, Api) and a `-DisableBail` switch | ⬜ | | |
| 0.2 | `scripts/mutation-test-value.ps1` reads a Stryker JSON report (.NET or JS) and prints survivors by file, zero-kill tests and redundant tests | ⬜ | | |
| 0.3 | StrykerJS: check `disableBail` and a per-slice `--mutate` run work for `.tsx` files | ⬜ | | |
| 0.4 | Measure run time per project and correct the slice sizes below | ⬜ | | |
| 0.5 | Ignore `StrykerOutput/` under `Tests/` (`Financial.Web/reports/` is already ignored; the .NET output is not) | ⬜ | | |

### Slices

| # | Project | Slice (`--mutate`) | Status | Score before | Score after | Tests added | Tests removed | Equivalent | PR | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| 1.1 | CashFlow.Domain | `Entities/**` | ⬜ | | | | | | | |
| 1.2 | CashFlow.Domain | `Rules/**`, `ValueObjects/**` | ⬜ | | | | | | | |
| 2.1 | Investment.Domain | `Entities/**` | ⬜ | | | | | | | |
| 2.2 | Investment.Domain | `Rules/**` | ⬜ | | | | | | | |
| 2.3 | Investment.Domain | `ValueObjects/**`, `Exceptions/**` | ⬜ | | | | | | | |
| 3.1 | CashFlow.Application | Accounts: `AccountAdminService`, `AccountGroupSummaryService`, `AccountQueryService`, `AccountReferenceReader`, `AccountTransactionReader`, `AccountTransferService`, `BalanceAdjustmentService` | ⬜ | | | | | | | |
| 3.2 | CashFlow.Application | Cards: `CardStatementCreation`, `CardStatementService`, `CreditCardAccounts`, `StatementDueDateService`, `PaymentsDueService` | ⬜ | | | | | | | |
| 3.3 | CashFlow.Application | Calendar: `CalendarIntegrationService`, `CreditCardCalendarSyncService`, `CreditCardCalendarSyncStatusStore` | ⬜ | | | | | | | |
| 3.4 | CashFlow.Application | Movements: `ExpenseService`, `IncomeService`, `IncomeSourceService`, `IncomeSummaryService`, `IncomeGroupLookupBuilder`, `CategoryService`, `CategorySummaryService`, `HistoricAverageService`, `MensaisService` | ⬜ | | | | | | | |
| 3.5 | CashFlow.Application | Reserve and settlement: `ReserveService`, `ReserveSplitRule`, `PotsService`, `TitheService`, `SettlementService`, `CurrencyBalanceMapper` | ⬜ | | | | | | | |
| 3.6 | CashFlow.Application | Investment accounts: `InvestmentAccountService`, `InvestmentAnnualResultService`, `InvestmentSnapshotService` | ⬜ | | | | | | | |
| 3.7 | CashFlow.Application | `Validation/**` | ⬜ | | | | | | | |
| 4.1 | Investment.Application | Assets and transactions: `AssetAdminService`, `AssetMoveService`, `AssetMutationHelper`, `TransactionService`, `CreditService`, `DividendService`, `FxEntryCaptureHelper` | ⬜ | | | | | | | |
| 4.2 | Investment.Application | Corporate actions: `CorporateActionService`, `BrokerCurrencyParser` | ⬜ | | | | | | | |
| 4.3 | Investment.Application | Summaries: `PortfolioService`, `PortfolioAssetSummary*`, `PortfolioDashboard*`, `SummaryService`, `ConvertedSummaryBuilder`, `ConvertedAssetFlows`, `AssetTotals`, `AssetAmountBases`, `AssetCashFlowBuilder` | ⬜ | | | | | | | |
| 4.4 | Investment.Application | Breakdowns and returns: `AllocationBreakdown*`, `BrokerBreakdown*`, `PortfolioXirrBuilder`, `XirrCalculationService`, `HoldingValuationService`, `CurrencyConversionContext` | ⬜ | | | | | | | |
| 4.5 | Investment.Application | Prices, income, tax: `AssetPriceHistoryService`, `AssetPriceLookupService`, `UpcomingIncome*`, `TaxRuleService`, `TaxWorkbookService`, `DataQualityReportService` | ⬜ | | | | | | | |
| 4.6 | Investment.Application | `NavigationMapper`, `NavigationService`, `BrokerService`, `ReportingCurrencySettingService`, `Validation/**` | ⬜ | | | | | | | |
| 5.x | Financial.Web | `src/hooks` — slices of ~10 files, worst score first, taken from the nightly `stryker-js` report (no local run needed to triage) | ⬜ | 65.2% | | | | | | Slice list added after 0.4 |
| 6.1 | Financial.Web | `src/utils/**` | ⬜ | | | | | | | |
| 7.1 | Shared.Abstractions | all | ⬜ | | | | | | | |
| 7.2 | Shared.Infrastructure | all | ⬜ | | | | | | | Integration tests (temp files): slower per mutant |
| 7.3 | CashFlow.Infrastructure | all | ⬜ | | | | | | | |
| 7.4 | Investment.Infrastructure | `Services/**` first, then the rest | ⬜ | | | | | | | |
| 8.x | Financial.Web | `src/components`, `src/pages` — only files holding logic (forms, totals, validation, filtering); presentational files skipped | ⬜ | | | | | | | Slice list added after 0.4 |
| 9.1 | Financial.Api | `Middleware/**`, `Helpers/**` | ⬜ | | | | | | | Host tests: restrict the test filter per slice |
| 9.2 | Financial.Api | Controllers with branching only | ⬜ | | | | | | | Thin controllers skipped |

### Batch 10 — Keep it continuous

| # | Task | Status | PR | Notes |
|---|---|---|---|---|
| 10.1 | Add each reviewed project to the nightly `stryker-dotnet` matrix with its own break threshold | ⬜ | | |
| 10.2 | Widen the nightly StrykerJS `mutate` to the reviewed components | ⬜ | | |
| 10.3 | Raise the existing break thresholds once scores hold for a week (`docs/ci-affected-pipeline.md`) | ⬜ | | |
| 10.4 | Decide on an advisory PR job mutating changed files only (`--since origin/main`) | ⬜ | | |

## Log

One line per working session: date, slice, outcome.

| Date | Slice | Outcome |
|---|---|---|
| 2026-11-14 | — | Plan written; baseline recorded from nightly run 43584910403 |
