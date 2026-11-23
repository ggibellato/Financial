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
6. **Review** with the `simplify` skill on the diff, apply its fixes, commit, then run it again
   on the committed branch until a pass finds nothing material. Re-run the slice if tests were
   removed or moved, to confirm no kills were lost.
7. **Ship** one PR per slice, within the PR size target. The slice PR adds its notes file
   (`docs/mutation-review/<slice>.md`) and does not touch this file, so the next slice can start
   before it merges without conflicts. Every few slices one docs-only PR updates the table rows,
   PR numbers and log lines here.

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
  `../financial-wt-mutation-review`, removed after batch 0 merged (#1283). The tracker and every
  slice's notes live in `docs/mutation-review/`.
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
| 1.1 | CashFlow.Domain | `Entities/**` | ✅ | 90.3% | 94.5% | 18 | 0 | 10 | [#1307](https://github.com/ggibellato/Financial/pull/1307) | [Notes](1.1.md); round-up suggestion, inactive card/pots checks, capability defaults on setup change |
| 1.2 | CashFlow.Domain | `Rules/**`, `ValueObjects/**` | ✅ | 91.1% | 97.0% | 12 | 0 | 7 | [#1309](https://github.com/ggibellato/Financial/pull/1309) | [Notes](1.2.md); found the negative pot share bug, fixed in [#1310](https://github.com/ggibellato/Financial/pull/1310) |
| 2.1 | Investment.Domain | `Entities/**` | ✅ | 82.0% | 88.1% | 28 | 0 | 8 | [#1306](https://github.com/ggibellato/Financial/pull/1306) | [Notes](2.1.md); disposal regeneration boundaries, corporate action revise/rollback; most survivors are wording |
| 2.2 | Investment.Domain | `Rules/**` | ✅ | 74.5% | 87.7% | 15 | 3 | ~35 | [#1284](https://github.com/ggibellato/Financial/pull/1284) | [Notes](2.2.md); 734 → 754 tests |
| 2.3 | Investment.Domain | `ValueObjects/**`, `Exceptions/**` | ⏭️ | — | | | | | | No mutants generated |
| 3.1 | CashFlow.Application | Accounts: `AccountAdminService`, `AccountGroupSummaryService`, `AccountQueryService`, `AccountReferenceReader`, `AccountTransactionReader`, `AccountTransferService`, `BalanceAdjustmentService` | ✅ | 70.3% | 80.5% | 9 | 29 | 21 | [#1298](https://github.com/ggibellato/Financial/pull/1298) | [Notes](3.1.md); 29 Domain re-checks and duplicates removed; adjustment add span uses the account id |
| 3.2 | CashFlow.Application | Cards: `CardStatementCreation`, `CardStatementService`, `CreditCardAccounts`, `StatementDueDateService`, `PaymentsDueService` | ✅ | 67.3% | 78.4% | 3 | 6 | 5 | [#1297](https://github.com/ggibellato/Financial/pull/1297) | [Notes](3.2.md); the second simplify pass kept the only credit-covers-the-month refusal test |
| 3.3 | CashFlow.Application | Calendar: `CalendarIntegrationService`, `CreditCardCalendarSyncService`, `CreditCardCalendarSyncStatusStore` | ✅ | 54.9% | 66.7% | 6 | 4 | 31 | [#1291](https://github.com/ggibellato/Financial/pull/1291) | [Notes](3.3.md); `BestEffortDisconnectRemoteAsync` returns a value nobody reads |
| 3.4 | CashFlow.Application | Movements: `ExpenseService`, `IncomeService`, `IncomeSourceService`, `IncomeSummaryService`, `IncomeGroupLookupBuilder`, `CategoryService`, `CategorySummaryService`, `HistoricAverageService`, `MensaisService` | ✅ | 63.5% | 76.9% | 11 | 57 | 25 | [#1294](https://github.com/ggibellato/Financial/pull/1294) | [Notes](3.4.md); tests were 4× the code, 57 Domain re-checks and duplicates removed |
| 3.5 | CashFlow.Application | Reserve and settlement: `ReserveService`, `ReserveSplitRule`, `PotsService`, `TitheService`, `SettlementService`, `CurrencyBalanceMapper` | ✅ | 63.2% | 76.2% | 7 | 14 | 24 | [#1292](https://github.com/ggibellato/Financial/pull/1292) | [Notes](3.5.md); 14 tests that re-checked Domain pot and tithe rules or duplicated others removed |
| 3.6 | CashFlow.Application | Investment accounts: `InvestmentAccountService`, `InvestmentAnnualResultService`, `InvestmentSnapshotService` | ✅ | 63.2% | 75.3% | 5 | 4 | 11 | [#1293](https://github.com/ggibellato/Financial/pull/1293) | [Notes](3.6.md); an inactive account's existing snapshot stays hidden (found by the second simplify pass) |
| 3.7 | CashFlow.Application | `Validation/**` | ✅ | 96.4% | 98.2% | 1 | 0 | 1 | [#1333](https://github.com/ggibellato/Financial/pull/1333) | [Notes](3.7.md); withdrawing exactly a pot's balance; the account name check takes a nullable name it never receives |
| 4.1 | Investment.Application | Assets and transactions: `AssetAdminService`, `AssetMoveService`, `AssetMutationHelper`, `TransactionService`, `CreditService`, `DividendService`, `FxEntryCaptureHelper` | ✅ | 47.7% | 65.0% | 11 | 9 | 16 | [#1286](https://github.com/ggibellato/Financial/pull/1286) | [Notes](4.1.md); 48.0% when run with `-AlsoTestedBy Investment.Infrastructure` |
| 4.2 | Investment.Application | Corporate actions: `CorporateActionService`, `BrokerCurrencyParser` | ✅ | 49.6% | 70.3% | 7 | 1 | 11 | [#1287](https://github.com/ggibellato/Financial/pull/1287) | [Notes](4.2.md); `BrokerCurrencyParser` is only used by 4.3/4.4 services |
| 4.3 | Investment.Application | Summaries: `PortfolioService`, `PortfolioAssetSummary*`, `PortfolioDashboard*`, `SummaryService`, `ConvertedSummaryBuilder`, `ConvertedAssetFlows`, `AssetTotals`, `AssetAmountBases`, `AssetCashFlowBuilder` | ✅ | 56.8% | 73.7% | 15 | 31 | 30 | [#1289](https://github.com/ggibellato/Financial/pull/1289) | [Notes](4.3.md); 28 asset-summary tests that re-checked Domain results removed |
| 4.4 | Investment.Application | Breakdowns and returns: `AllocationBreakdown*`, `BrokerBreakdown*`, `PortfolioXirrBuilder`, `XirrCalculationService`, `HoldingValuationService`, `CurrencyConversionContext` | ✅ | 70.2% | 78.5% | 3 | 10 | 5 | [#1290](https://github.com/ggibellato/Financial/pull/1290) | [Notes](4.4.md); 10 tests that re-checked Domain XIRR and valuation results removed |
| 4.5 | Investment.Application | Prices, income, tax: `AssetPriceHistoryService`, `AssetPriceLookupService`, `UpcomingIncome*`, `TaxRuleService`, `TaxWorkbookService`, `DataQualityReportService` | ✅ | 26.7% (47.7% with the Infrastructure tests) | 77.5% | 8 | 16 | 11 | [#1285](https://github.com/ggibellato/Financial/pull/1285) | [Notes](4.5.md); run with `-AlsoTestedBy Investment.Infrastructure` |
| 4.6 | Investment.Application | `NavigationMapper`, `NavigationService`, `BrokerService`, `ReportingCurrencySettingService`, `Validation/**` | ✅ | 52.7% | 74.5% | 9 | 1 | 6 | [#1288](https://github.com/ggibellato/Financial/pull/1288) | [Notes](4.6.md); 55.3% when run with `-AlsoTestedBy Investment.Infrastructure` |
| 5.1 | Financial.Web | 11 hooks the nightly report scored worst (pot forms, manage pots, row edit route, …) | ✅ | 80.3% | 96.1% | 37 | 0 | 3 | [#1311](https://github.com/ggibellato/Financial/pull/1311) | [Notes](5.1.md); measured with every web test: the nightly hook score leaves out component tests |
| 5.2 | Financial.Web | `useCorporateActions`, `useAccountActionForms` | ✅ | 75.5% | 89.5% | 25 | 0 | 14 | [#1312](https://github.com/ggibellato/Financial/pull/1312) | [Notes](5.2.md); split ratio `/` vs `*`; account action forms deferred to 5.3 |
| 5.3 | Financial.Web | `useAccountActionForms` | ✅ | 87.5% | 94.7% | 20 | 0 | 13 | [#1313](https://github.com/ggibellato/Financial/pull/1313) | [Notes](5.3.md); which forms close each other, focus; last kills hand-checked |
| 5.4 | Financial.Web | `useTransactions`, `useExpenseForm`, `useCredits` | ✅ | 76.5% | 93.2% | 44 | 0 | 23 | [#1315](https://github.com/ggibellato/Financial/pull/1315) | [Notes](5.4.md); found a create form opening with the edited expense's round-up, fixed in [#1316](https://github.com/ggibellato/Financial/pull/1316) |
| 5.5 | Financial.Web | `useIncomeForm`, `useMensais`, `useAccountTransferForm` | ✅ | 81.0% | 95.4% | 41 | 0 | 19 | [#1318](https://github.com/ggibellato/Financial/pull/1318) | [Notes](5.5.md) |
| 5.6 | Financial.Web | `useRowFocusRecovery`, `useSuggestedValues`, `usePayRecurringBill`, `usePayCardStatementForm` | ✅ | 72.5% | 90.7% | 33 | 0 | 17 | [#1319](https://github.com/ggibellato/Financial/pull/1319) | [Notes](5.6.md); found the Suggest Values panel reopening after a close while loading, fixed in [#1320](https://github.com/ggibellato/Financial/pull/1320) |
| 5.7 | Financial.Web | `useAccountScreen`, `useCardStatements`, `useMoveChargeForm`, `useCardCreditRefundForm` | ✅ | 80.5% | 93.0% | 18 | 0 | 22 | [#1321](https://github.com/ggibellato/Financial/pull/1321) | [Notes](5.7.md); found the previous account's tabs shown while another account loads, fixed in [#1322](https://github.com/ggibellato/Financial/pull/1322) |
| 5.8 | Financial.Web | `useInvestmentSnapshots`, `useAssetSummary`, `usePortfolioAssetSummary`, `useDisposals`, `usePriceHistory` | ✅ | 76.9% | 88.1% | 12 | 0 | 26 | [#1323](https://github.com/ggibellato/Financial/pull/1323) | [Notes](5.8.md); found the price history window dropping first-day prices at a negative UTC offset (since fixed) |
| 5.9 | Financial.Web | `useSettlementEntryForm`, `useIncomeTab`, `useBalanceAdjustmentForm`, `useStatementDueDateForm`, `useTithe` | ✅ | 88.0% | 97.5% | 19 | 0 | 5 | [#1325](https://github.com/ggibellato/Financial/pull/1325) | [Notes](5.9.md) |
| 5.10 | Financial.Web | Shared helpers: `useCrudResource`, `useAsyncResource`, `useSortableRows`, `useColumnFilters`, `useMediaQuery`, `useConfirmedDelete`, `useDuplicateNameField` | ✅ | 72.8% | 84.4% | 22 | 2 | 33 | [#1326](https://github.com/ggibellato/Financial/pull/1326) | [Notes](5.10.md); most survivors are stable dependency lists; three sort mutants are equivalent only on V8 |
| 5.11 | Financial.Web | `useCalendarConnection`, `useCalendarSyncStatuses`, `useReportingCurrency`, `useSyncStatus` | ✅ | 60.4% | 76.9% | 7 | 0 | 15 | [#1327](https://github.com/ggibellato/Financial/pull/1327) | [Notes](5.11.md); the reporting currency `enabled ?? true` fallback is dead code |
| 5.12 | Financial.Web | Admin list loaders: `useBrokers`, `useCategories`, `useIncomeSources`, `useInvestmentAccounts`, `useRecurringBills`, `usePortfolios` | ✅ | 18.4% | 18.4% | 0 | 0 | 19 | [#1328](https://github.com/ggibellato/Financial/pull/1328) | [Notes](5.12.md); notes only: thin wrappers over `useCrudResource`, the rest is wording |
| 5.13 | Financial.Web | `useTaxWorkbook`, `useAdminAccounts`, `useAssets`, `useAnnualSummary`, `usePotTransactionEditForm`, `usePaymentsDue` | ✅ | 73.0% | 87.7% | 13 | 0 | 15 | [#1348](https://github.com/ggibellato/Financial/pull/1348) | [Notes](5.13.md); load-cancel flags caught under StrictMode's double effect |
| 5.14 | Financial.Web | `useAccountFormOptions`, `inlineFormSlice`, `useOverviewAddress`, `useHoldingNavigation`, `useAccountsSummary`, `useAssetSearchOptions`, `useOpenLots` | ✅ | 90.0% | 96.7% | 4 | 0 | 3 | [#1349](https://github.com/ggibellato/Financial/pull/1349) | [Notes](5.14.md) |
| 5.15 | Financial.Web | Summaries, breakdowns, dashboard, month selection, dialogs, toasts: eleven small hooks | ✅ | 87.7% | 93.0% | 3 | 0 | 4 | [#1350](https://github.com/ggibellato/Financial/pull/1350) | [Notes](5.15.md); success toast asserted through its checkmark icon |
| 5.16 | Financial.Web | The smallest hooks and helpers: `useFocusReloadedContent`, `useMonthTransactions`, `useAccountTemplateGroups` and seven more | ✅ | 81.6% | 97.4% | 3 | 0 | 1 | [#1351](https://github.com/ggibellato/Financial/pull/1351) | [Notes](5.16.md); last hooks slice |
| 5.x | Financial.Web | remaining `src/hooks`: slices of a few files, worst score first; measure each with the tests that reach it (`STRYKER_TEST_INCLUDE`), not the nightly score | ✅ | | | | | | | Done in 5.13-5.16; every file in `src/hooks` reviewed |
| 6.1 | Financial.Web | `src/utils/**` | ✅ | 80.1% | 95.4% | 51 | 0 | 21 | [#1332](https://github.com/ggibellato/Financial/pull/1332) | [Notes](6.1.md); formatters and funding lines; `signClass` shows zero in green; `holdingNavigation` node checks can never fail |
| 7.1 | Shared.Abstractions | all | ✅ | 63.9% | 86.7% | 9 | 1 | 8 | [#1296](https://github.com/ggibellato/Financial/pull/1296) | [Notes](7.1.md); telemetry and reflection helpers had no tests in their own project |
| 7.2 | Shared.Infrastructure | all | ✅ | 73.7% | 83.6% | 11 | 2 | 4 | [#1301](https://github.com/ggibellato/Financial/pull/1301) | [Notes](7.2.md); debounced save cycle flush and failure paths; three "equivalent" mutants were real gaps |
| 7.3 | CashFlow.Infrastructure | all | ✅ | 80.6% | 92.7% | 9 | 1 | 4 | [#1304](https://github.com/ggibellato/Financial/pull/1304) | [Notes](7.3.md); reference wire names and required flags, credentials path, dead converter code |
| 7.4 | Investment.Infrastructure | all | ✅ | 81.1% | 93.2% | 7 | 0 | 4 | [#1305](https://github.com/ggibellato/Financial/pull/1305) | [Notes](7.4.md); two tests passed vacuously because BRL is the Currency default |
| 8.x | Financial.Web | `src/components`, `src/pages` — only files holding logic (forms, totals, validation, filtering); presentational files skipped | ✅ | | | | | | | Trial on `CategoryTotalsGrid.tsx`: 57.1%, 6 text/label mutants survive; ranked by logic held, done in eight slices (8.1–8.8, see [8.1](8.1.md)) |
| 8.1 | Financial.Web | `AccountFormDialog`, `InvestmentTree` | ✅ | 76.2% | 90.2% | 23 | 1 | 42 | [#1334](https://github.com/ggibellato/Financial/pull/1334) | [Notes](8.1.md); `queryByRole('dialog')` can pass while a dialog stays open; the opening balance number message can never show |
| 8.2 | Financial.Web | `TransactionsTab`, `CreditsTab` | ✅ | 70.1% | 86.6% | 17 | 0 | 4 | [#1335](https://github.com/ggibellato/Financial/pull/1335) | [Notes](8.2.md); credit chart colours and stacking; most survivors are chart styling |
| 8.3 | Financial.Web | `PortfolioHoldingsTab`, `AssetSummaryTab`, `PriceHistoryTab` | ✅ | 71.1% | 97.9% | 34 | 6 | 0 | [#1337](https://github.com/ggibellato/Financial/pull/1337) | [Notes](8.3.md); led to the "Current price" label ([#1338](https://github.com/ggibellato/Financial/pull/1338)) and the asset summary clean-up ([#1339](https://github.com/ggibellato/Financial/pull/1339)) |
| 8.4 | Financial.Web | `AccountScreenPage`, `CardStatementsTab`, `AccountTransactionsGrid` | ✅ | 81.9% | 93.8% | 26 | 0 | 15 | [#1341](https://github.com/ggibellato/Financial/pull/1341) | [Notes](8.4.md); the unmark dialog lost its message while it closed (since fixed) |
| 8.5 | Financial.Web | `ExpenseForm`, `IncomeForm`, `TransferForm`, `PotDepositForm`, `PotWithdrawalForm` | ✅ | 86.5% | 97.9% | 22 | 0 | 7 | [#1342](https://github.com/ggibellato/Financial/pull/1342) | [Notes](8.5.md); the overdraft dialog lost its message while it closed, like 8.4's (since fixed) |
| 8.6 | Financial.Web | `MensaisPage`, `AnnualSummaryPage`, `MonthTransactionsGrid`, `IncomeTab` | ✅ | 80.0% | 97.5% | 22 | 0 | 6 | [#1343](https://github.com/ggibellato/Financial/pull/1343) | [Notes](8.6.md); deleting the Mensais bill being edited left its edit form open with a blank Bill (since fixed) |
| 8.7 | Financial.Web | `CorporateActionsTab`, `CorporateActionForm`, `MoveAssetDialog`, `DisposalsTab` | ✅ | 68.9% | 94.8% | 36 | 0 | 19 | [#1344](https://github.com/ggibellato/Financial/pull/1344) | [Notes](8.7.md) |
| 8.8 | Financial.Web | `Sidebar`, `TickerCombobox`, `CurrentValuesPage`, `DividendCheckPage`, `InvestmentAccountFormDialog`, `AccountHeader`, `AggregatedSummaryTab` | ✅ | 72.6% | 97.3% | 45 | 0 | 13 | [#1346](https://github.com/ggibellato/Financial/pull/1346) | [Notes](8.8.md); a failed price fetch's message was never shown; ArrowUp on the closed ticker picker activated an option (both since fixed) |
| 9.1 | Financial.Api | `Program.cs`, `Middleware/**`, `Helpers/**` | ✅ | 71.6% | 90.1% | 7 | 0 | 4 | [#1300](https://github.com/ggibellato/Financial/pull/1300) | [Notes](9.1.md); `Program.cs` wiring (CORS, versioning, options binding) asserted; `UseDeveloperExceptionPage` redundant |
| 9.2 | Financial.Api | All controllers | ✅ | 78.7% | 95.6% | 1 | 0 | 1 | [#1302](https://github.com/ggibellato/Financial/pull/1302) | [Notes](9.2.md); empty-body test now requires the guard to reject before any service; blank route-value guards are dead code |

### Batch 10 — Keep it continuous

| # | Task | Status | PR | Notes |
|---|---|---|---|---|
| 10.1 | Add each reviewed project to the nightly `stryker-dotnet` matrix with its own break threshold | ✅ | [#1353](https://github.com/ggibellato/Financial/pull/1353), [#1355](https://github.com/ggibellato/Financial/pull/1355) | Seven projects added, each breaking 5 points below its score measured on 2026-11-19 |
| 10.2 | Widen the nightly StrykerJS `mutate` to the reviewed components | ✅ | [#1356](https://github.com/ggibellato/Financial/pull/1356) | Separate `Web.Components` entry over the 30 reviewed files, 95.0%, break 90; Web.Logic unchanged |
| 10.3 | Raise the existing break thresholds once scores hold for a week (`docs/ci-affected-pipeline.md`) | ✅ | | Raised to the lowest nightly score of the week minus 5: CashFlow.Domain 72 → 88, Investment.Domain 72 → 82, Web.Logic 55.9 → 77. The entries added on 2026-11-19 already break 5 below their measured score and have not risen |
| 10.4 | Decide on an advisory PR job mutating changed files only (`--since origin/main`) | ✅ | | Decided 2026-11-19: no PR job. The nightly mutates every reviewed project and component and opens the `nightly-failure` issue when one drops below its break, so a regression shows within a day without adding minutes to every PR |

## Slice notes

Each slice keeps its notes (gaps closed, tests removed and why, the undetected mutants by
class, production notes) in its own file in this folder:

- [2.2 Investment.Domain rules (74.5% → 87.7%)](2.2.md)
- [3.1 CashFlow.Application accounts (70.3% → 80.5%)](3.1.md)
- [3.2 CashFlow.Application cards (67.3% → 78.4%)](3.2.md)
- [3.3 CashFlow.Application calendar (54.9% → 66.7%)](3.3.md)
- [3.4 CashFlow.Application movements (63.5% → 76.9%)](3.4.md)
- [3.5 CashFlow.Application reserve and settlement (63.2% → 76.2%)](3.5.md)
- [3.6 CashFlow.Application investment accounts (63.2% → 75.3%)](3.6.md)
- [4.1 Investment.Application assets and transactions (48.0% → 65.0%)](4.1.md)
- [4.2 Investment.Application corporate actions (49.6% → 70.3%)](4.2.md)
- [4.3 Investment.Application summaries and dashboard (56.8% → 73.7%)](4.3.md)
- [4.4 Investment.Application breakdowns and returns (70.2% → 78.5%)](4.4.md)
- [4.5 Investment.Application prices, income and tax (47.7% → 77.5%)](4.5.md)
- [4.6 Investment.Application navigation, brokers and validation (55.3% → 74.5%)](4.6.md)
- [7.1 Shared.Abstractions (63.9% → 86.7%)](7.1.md)
- [7.2 Shared.Infrastructure (73.7% → 83.6%)](7.2.md)
- [9.1 Api Program, Middleware and Helpers (71.6% → 90.1%)](9.1.md)
- [9.2 Api controllers (78.7% → 95.6%)](9.2.md)
- [7.3 CashFlow.Infrastructure (80.6% → 92.7%)](7.3.md)
- [7.4 Investment.Infrastructure (81.1% → 93.2%)](7.4.md)
- [2.1 Investment.Domain entities (82.0% → 88.1%)](2.1.md)
- [1.1 CashFlow.Domain entities (90.3% → 94.5%)](1.1.md)
- [1.2 CashFlow.Domain rules and value objects (91.1% → 97.0%)](1.2.md)
- [5.1 Web hooks: the eleven worst by the nightly report (80.3% → 96.1%)](5.1.md)
- [5.2 Web hooks: corporate actions and account action forms (75.5% → 89.5%)](5.2.md)
- [5.3 Web hooks: account action form orchestration (87.5% → 94.7%)](5.3.md)
- [5.4 Web hooks: transactions, expense form and credits (76.5% → 93.2%)](5.4.md)
- [5.5 Web hooks: income form, Mensais and account transfers (81.0% → 95.4%)](5.5.md)
- [5.6 Web hooks: row focus recovery, suggested values, bill and statement payment (72.5% → 90.7%)](5.6.md)
- [5.7 Web hooks: account screen, card statements, moving a charge, card credit refund (80.5% → 93.0%)](5.7.md)
- [5.8 Web hooks: investment snapshots, asset and portfolio summaries, disposals, price history (76.9% → 88.1%)](5.8.md)
- [5.9 Web hooks: settlement entry, income tab, balance adjustment, statement due date, tithe (88.0% → 97.5%)](5.9.md)
- [5.10 Web hooks: shared helpers (72.8% → 84.4%)](5.10.md)
- [5.11 Web hooks: calendar connection, calendar sync statuses, reporting currency, sync status (60.4% → 76.9%)](5.11.md)
- [5.12 Web hooks: small admin list loaders (18.4%, no gaps)](5.12.md)
- [6.1 Web utils (80.1% → 95.4%)](6.1.md)
- [3.7 CashFlow.Application validation (96.4% → 98.2%)](3.7.md)
- [8.1 Web components: account form dialog, investment tree (76.2% → 90.2%)](8.1.md)
- [8.2 Web components: transactions tab, credits tab (70.1% → 86.6%)](8.2.md)
- [8.3 Web components: portfolio holdings, asset summary, price history (71.1% → 97.9%)](8.3.md)
- [8.4 Web components: account screen, card statements, account transactions grid (81.9% → 93.8%)](8.4.md)
- [8.5 Web components: account forms (86.5% → 97.9%)](8.5.md)
- [8.6 Web pages: Mensais, annual summary, month transactions grid, income tab (80.0% → 97.5%)](8.6.md)
- [8.7 Web components: corporate actions, move asset dialog, disposals (68.9% → 94.8%)](8.7.md)
- [8.8 Web components and pages: navigation, ticker picker, prices, dividends, account header (72.6% → 97.3%)](8.8.md)
- [5.13 Web hooks: tax workbook, admin accounts, assets, annual summary, pot transaction edit, payments due (73.0% → 87.7%)](5.13.md)
- [5.14 Web hooks: account form options, inline form slice, overview address, holding navigation, accounts summary, asset search, open lots (90.0% → 96.7%)](5.14.md)
- [5.15 Web hooks: summaries, breakdowns, dashboard, month selection, dialogs, toasts (87.7% → 93.0%)](5.15.md)
- [5.16 Web hooks: the smallest hooks and helpers (81.6% → 97.4%)](5.16.md)

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
| 2026-11-15 | 3.5 | Reserve and settlement 63.2% → 76.2%; spans, null requests and tithe boundaries tested; 14 tests that re-checked Domain rules or duplicated others removed |
| 2026-11-15 | 3.6 | Investment accounts 63.2% → 75.3%; spans, null requests, the prior-December filter and the snapshot grid's filter on existing snapshots tested; 4 tests removed |
| 2026-11-15 | 3.4 | Movements 63.5% → 76.9%; spans, null requests, the current-month cutoff and the historic tax difference tested; 57 tests that re-checked Domain rules or duplicated others removed |
| 2026-11-15 | — | Tracker and slice notes moved to `docs/mutation-review/`, one file per slice; slice PRs no longer edit this file |
| 2026-11-15 | 7.1 | Shared.Abstractions 63.9% → 86.7%; direct tests for the telemetry, reflection JSON and enum helpers and the null-argument guards |
| 2026-11-15 | 3.2 | Cards 67.3% → 78.4%; spans, null request, payments-due time zone, first-of-month and ordering tested; 6 ledger re-checks removed |
| 2026-11-15 | 3.1 | Accounts 70.3% → 80.5%; spans, null requests, delete-in-use branches, group totals and month-end tested; 29 tests removed |
| 2026-11-15 | 9.1 | Api wiring 71.6% → 90.1%; CORS, API versions, options binding, static files and Swagger UI asserted |
| 2026-11-15 | 7.2 | Shared.Infrastructure 73.7% → 83.6%; debounced storage flush, generation and failure paths; missing-folder guard |
| 2026-11-15 | 9.2 | Api controllers 78.7% → 95.6%; empty-body guards pinned, dividend exchange, enum casing, calendar pages, tax year |
| 2026-11-15 | 7.3 | CashFlow.Infrastructure 80.6% → 92.7%; reference wire names and required flags, credentials file path, calendar settings binding |
| 2026-11-15 | 7.4 | Investment.Infrastructure 81.1% → 93.2%; two vacuous currency tests fixed (BRL is the default), Yahoo suffixes, fetcher requests |
| 2026-11-15 | 2.1 | Investment.Domain entities 82.0% → 88.1%; disposal regeneration boundaries, corporate action revise and rollback |
| 2026-11-15 | 1.1 | CashFlow.Domain entities 90.3% → 94.5%; round-up suggestion, inactive card and pots checks, setup-change defaults |
| 2026-11-15 | 1.2 | CashFlow.Domain rules 91.1% → 97.0%; found a negative pot share in `PotSplitAllocator`, fixed in #1310 |
| 2026-11-16 | 5.1 | Worst eleven web hooks 80.3% → 96.1%; the nightly hook score leaves out component tests, so it understates coverage |
| 2026-11-16 | 5.2 | `useCorporateActions` 62.8% → 91.7%; split ratio, blank optional fields, validation edges |
| 2026-11-16 | 5.3 | `useAccountActionForms` 87.5% → 94.7%; which forms close each other, focus handling |
| 2026-11-16 | 5.4 | Transactions, expense form and credits hooks 76.5% → 93.2%; found a create form opening with the edited expense's round-up, fixed in #1316 |
| 2026-11-17 | 5.5 | Income form, Mensais and account transfer hooks 81.0% → 95.4% |
| 2026-11-17 | 5.6 | Row focus recovery, suggested values and payment hooks 72.5% → 90.7%; found the Suggest Values panel reopening after a close while loading, fixed in #1320 |
| 2026-11-17 | 5.7 | Account screen, card statements, move charge and card credit refund hooks 80.5% → 93.0%; found stale tabs on an account switch, fixed in #1322 |
| 2026-11-17 | 5.8 | Snapshot, asset and portfolio summary, disposal and price history hooks 76.9% → 88.1%; found the price history window dropping first-day prices at a negative UTC offset |
| 2026-11-17 | 5.9 | Settlement entry, income tab, balance adjustment, due date and tithe hooks 88.0% → 97.5% |
| 2026-11-18 | 5.10 | Shared helper hooks 72.8% → 84.4%; `useConfirmedDelete` gets its own test file |
| 2026-11-18 | 5.11 | Calendar, reporting currency and sync status hooks 60.4% → 76.9% |
| 2026-11-18 | 5.12 | Admin list loader hooks reviewed, no gaps: thin wrappers over `useCrudResource` |
| 2026-11-18 | 6.1 | Web utils 80.1% → 95.4%; formatters, funding lines and field errors; module-level constants misreported as survivors, checked by replay |
| 2026-11-18 | 3.7 | CashFlow validation 96.4% → 98.2%; one boundary test, one equivalent |
| 2026-11-18 | 8.1 | Account form dialog and investment tree 76.2% → 90.2%; first components slice, 8.x planned as eight slices |
| 2026-11-18 | 8.2 | Transactions and credits tabs 70.1% → 86.6% |
| 2026-11-18 | 8.3 | Portfolio holdings, asset summary and price history tabs 71.1% → 97.9%; asset summary label and clean-up PRs followed |
| 2026-11-19 | 8.4 | Account screen, card statements and account transactions grid 81.9% → 93.8%; the unmark dialog blanks its message while closing |
| 2026-11-19 | 8.5 | Account forms 86.5% → 97.9%; field errors asserted through `aria-invalid`; the overdraft dialog blanks its message while closing |
| 2026-11-19 | 8.6 | Mensais, annual summary, month transactions grid and income tab 80.0% → 97.5%; deleting the bill being edited leaves its form open |
| 2026-11-19 | 8.7 | Corporate actions, move asset dialog and disposals 68.9% → 94.8%; no product issue |
| 2026-11-19 | 8.8 | Sidebar, ticker picker, current values, dividend check, investment account dialog, account header and aggregated summary 72.6% → 97.3%; last 8.x slice, the 8.x row is done |
| 2026-11-19 | 5.13 | Tax workbook, admin accounts, assets, annual summary, pot transaction edit and payments due hooks 73.0% → 87.7%; load-cancel flags tested under StrictMode |
| 2026-11-19 | 5.14 | Account form options, inline form slice, overview address, holding navigation, accounts summary, asset search and open lots hooks 90.0% → 96.7% |
| 2026-11-19 | 5.15 | Summary, breakdown, dashboard, month selection, dialog and toast hooks 87.7% → 93.0% |
| 2026-11-19 | 5.16 | Smallest hooks and helpers 81.6% → 97.4%; last hooks slice, the 5.x row is done |
| 2026-11-19 | 10.1 | Nightly `stryker-dotnet` matrix covers every reviewed .NET project, each with its own break threshold |
| 2026-11-19 | 10.2 | Nightly StrykerJS adds a `Web.Components` entry for the 30 reviewed component and page files, 95.0% |
| 2026-11-19 | 10.4 | No advisory PR mutation job; the nightly covers it |
| 2026-11-19 | 8.4–8.8 | Fixed the product issues the slices noticed: confirm dialogs keep their message while closing, Mensais closes the edit of a deleted bill, Current values lists failed fetches, ArrowUp opens the ticker picker |
| 2026-11-19 | 5.8 | Fixed the defect it found: the price history and credits period filters read a date as a local calendar date, so a first-day price stays in the window at a negative UTC offset |
| 2026-11-23 | 10.3 | Break thresholds raised: CashFlow.Domain 88, Investment.Domain 82, Web.Logic 77; batch 10 is done |
