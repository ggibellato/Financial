# Test-guidance compliance audit — 2026-11-20

Scope: every test on `main` at `c59fe09c`: 17 .NET test projects (about 4,450 test methods), the Vitest suite (about 3,700 `it` blocks) and the 8 Playwright specs. Each test was checked against `testing-guide-Financial` (SKILL.md §1, §2, §5 and the artifact guides) and `docs/rules/implementation.md` §Tests. The review used eight read-only reviewers, one per slice. Every row was confirmed in the code, and the high-impact rows were re-checked by hand.

Totals: 188 finding rows across the eight slices. Many rows group several tests.

## 1. Real defects found (fix first)

| # | Where | Defect |
|---|---|---|
| 1 | `Financial.Investment.Application/Services/AssetPriceLookupService.cs:183-209` | `LogError(ex, …{BrokerName}/{PortfolioName}/{AssetName})` and a warning that logs the names. This breaks the "never log a value / exception" rule, and `AssetPriceLookupServiceTests.cs:578, :680` pin the bad message. |
| 2 | `Financial.Web/src/hooks/useAnnualSummary.ts:19` + `AnnualSummaryPage.test.tsx:397, :422, :463` | Time bomb: `INITIAL_YEAR = new Date().getFullYear()` with no pinned clock. The tests change the year to `'2027'`, so from 2027-01-01 that is no change and 3 tests fail. |
| 3 | `Tests/Financial.Architecture.Tests/CategoryTraitCoverageTests.cs:177` `MatchingBrace` | It counts braces inside string literals, so a `[GeneratedRegex(@"\{…\}")]` ends the class early. `EmptyBodyEndpointsTests` has no `Category` trait and the guard misses it. |
| 4 | `Financial.Shared.Infrastructure/…/DebouncedJsonStorage.cs:123` | `Task.Delay(_flushTimeout)` ignores the injected `TimeProvider`. That forces about 8 tests onto real time with `Stopwatch` assertions. |
| 5 | `FxRateLoader` / `FxRateJsonStore` | They swallow `FileNotFoundException`/`JsonException` and report through `Trace.TraceWarning`, not `ILogger`. `ExampleDataFileTests.cs:25, :36` is therefore vacuous: it passes with the example file missing or corrupt. |
| 6 | Frankfurter provider call budget | `CancellationTokenSource(_callBudget)` takes no `TimeProvider`, so its test uses a real 200 ms budget and a `Stopwatch`. |
| 7 | Web AC tags | 23 tests in `DashboardPage.test.tsx` (15) and `IntegrationsPage.test.tsx` (8) use `ID: …` instead of `[AC ID] …`, and `AccountsPage.test.tsx:519` uses a non-grammar id (`P61-cross-01`). The nightly ac-traceability report counts all of them as untested. |
| 8 | Api acceptance files | About 70 untagged tests are interleaved in `Acceptance/`. Three whole files have no tag: ExchangeRateCrossFeatureConsistency, FxRateProviderComposition, ServerComputedValuation. ServerComputedValuation is also a mirror test. |

## 2. Wall-clock and fixed-delay tests (legacy; CI only scans added lines)

- **Real 10 s debounce waits, polling `DateTime.UtcNow` + `Task.Delay(50)`, plus `Stopwatch` asserts:** `CashFlowRepositoryFactoryTests.cs:199, :243, :284` and `InvestmentRepositoryFactoryTests.cs:134, :180, :218`. Delete these; keep the Pending-status test.
- **`Task.WhenAny(x, Task.Delay(300/2000))`:** `CashFlowJsonRepositoryTests.cs:120, :147` and `InvestmentJsonRepositoryTests.cs:166, :194, :210`. Use a deterministic check after `WriteEntered` and `WaitAsync` instead.
- **Polling with `DateTime.UtcNow` + `Task.Delay`:** `EndToEndTraceTests.cs:111` and `CreditCardDueDateEventSyncAcceptanceTests.cs:45`. Wait on a signal instead, and move `SignallingCalendarSyncStatusStore` to TestUtilities.
- **About 8 tests on real time:** `DebouncedJsonStorageTests.cs:42, 80, 300, 317, 363, 375, 411`, which also wait in `WaitForAsync`. Use `ObservableFakeClock` instead, after defect 4.
- **About 12 s of real retry delay:** `TransientRetryPolicyTests.cs:28, 53, 72, 91`. Make it one Theory with a recording delay.
- **`TimeProvider.System` passed to the code under test:** `TransactionServiceMutationTests`, `TransactionServiceQueryTests`, `CreditServiceTests`, `FxEntryCaptureHelperTests`, `NavigationServiceTests`. Use `TestClock.At()` and assert `RetrievedAt`.
- **Unpinned `new Date()`:** `useMensais.test.ts:8`, `useInvestmentSnapshots.test.ts:8`, `useAnnualSummary.test.ts:7`, and `historic-average.spec.ts:3` (E2E seed year).

## 3. Structural violations

- **Owned code faked above Unit:**
  - Api `AssetPriceEndpointsTests` (its local stub copies production validation, so `:60, :70, :121` assert the stub), `DividendEndpointsTests` (also not derived from `ApiEndpointTests`), `SyncStatusEndpointsTests` and `DividendsControllerLoggingTests` (a unit-tested controller; merge into the host tests).
  - Infra `AssetPriceLookupServiceTests` (31 tests, Integration-tagged, faking owned types). Move the branches to Application Unit tests and keep about 3 disk tests.
  - Web: `TaxPage.test.tsx` mocks `useTaxWorkbook`; `CorporateActionsTab.test.tsx` mocks `useCorporateActions`; `useHoldingNavigation.test.ts` mocks react-router and an owned util; `useIncomeTab.test.ts` mocks `useSuccessToast`; `useTaxWorkbook.test.ts` mocks `taxWorkbookCsv` with a copy of the filename logic (a mirror).
- **Local copies of shared doubles:**
  - `StubRemoteFileClient(+Factory)` / `RecordingRemoteFileClient` in 4 projects.
  - `BlockingJsonStorage` ×3, and 4 "fail next write" storages.
  - `FakeFinanceService` ×2, plus `SyncStatusProviderStub`.
  - Web: inline `new Promise(() => {})` in about 22 places (use `deferred`/`never`); local `optionNames` ×4; inline fake timers instead of `pinDate` ×2; the recharts pie mock copied in 3 files.
- **Silent-default doubles:** `FakeCreditCardCalendarSyncService.GetSyncStatuses`, `FakeSyncStatusStorage`, and `StubDataSource.GetSpreadSheetAsync` return defaults; they should throw.
- **No FluentProvider:** 17 page test files and 27 component test files use plain RTL `render`. The fix is a one-line import swap to `renderWithFluent` in each.
- **Wrong trait (Integration but pure in-memory, should be Unit):**
  - `CashFlowDataConverterTests`, `TransferDestinationAmountPersistenceTests`.
  - 10 Investment fetcher/adapter/fallback classes.
  - `FxRateJsonStoreTests`, `FxRateLoaderTests` (these should also gain one real temp-file test).
- **Log assertions by message/entity name:** `AssetPriceLookupServiceTests.cs:578, :680` (defect 1).
- **Isolation leaks:**
  - `useTaxWorkbook.test.ts:47` uses `clearAllMocks` with queued `*Once` values.
  - `CorporateActionsTab.test.tsx:382` never restores `scrollIntoView`.
  - Storage-failure tests restore `localStorage` without `finally`.
  - `static readonly StubExchangeRateProvider` in 3 Investment test files.
- **Live checks:** `StatusInvestVerificationTests.cs` uses `Skip.If` on 403 (the guide says never `Skip`), and `GoogleFinanceVerificationTests.cs:84` cannot fail.
- **Persistence not proven from disk:** `AssetPriceHistoryServiceTests.cs` (5 tests) asserts only the returned DTO.
- **Temp-file boilerplate:** about 41 tests hand-roll it instead of using `PersistedInvestmentFile`.

## 4. Missing failure coverage the guide requires

- **Failure tests asserting only `Throw<T>()` without `ShouldHaveFailedSpan<T>`:** about 130 in CashFlow.Application and about 40 in Investment.Application (AssetAdmin, AssetMove, Portfolio, Broker, CorporateAction, TransactionMutation). Every operation still has at least one span check, so this is completeness, not a hole.
- **`RecordedException.NotBeNull()` instead of a typed check:** `RemoteJsonStorageTests.cs:91, :108` and `DebouncedJsonStorageTests.cs:375`.
- **`useAsyncResource`:** no test of `retry()` or of a rejected initial load. Add these before trimming consumer copies.
- **Admin pages missing loading and create-refused (409) tests:** Categories, Assets, Portfolios, RecurringBills, IncomeSources, InvestmentAccounts, InvestmentSnapshots. Active/HistoricInvestments have no tree-load error test.
- **Validators with no test of their own:** `DescriptionValidator`, `PotWithdrawalValidator`, `IncomeGroupParser`, `InvestmentAccountSourceParser` and `EntityLookupExtensions`. The 200/201 boundary is asserted through 4 callers instead.
- **DI default-provider tests:** in both Infrastructure projects they assert only `NotBeNull`. CashFlow also has no GoogleDrive branch test.
- **Scraper parsers:** no short-row, empty-table or unparseable-price tests (`DadosMercado`, `GoogleFinance`).
- **Domain projects:** no `ConstructorGuardTests`, so `TransferPairRulesTests.cs:178` hand-writes one.

## 5. Low-value tests to delete or merge (by area)

### Domain
- **Mirror tests** (expected value comes from the code under test):
  - `CorporateActionReplayTests.cs:105`
  - 5 overload-equality tests (`OpenPositionCostCalculator:38`, `HoldingValuationCalculator:33, :158`, `OpenLotTracker:183`, `SaleCoverageRule:199`)
  - `AverageCostReplayTests.cs:45`
- **Compiler-generated or plumbing behaviour:**
  - Record equality ×4 (`RoundUpSettingTests`, `OpeningBalanceTests`)
  - `List<T>` plumbing in `TransactionsTests.cs:177, :203`
  - Setter echoes ×6 (`CashFlowDataTests`, `TitheCarryForwardTests`, `InvestmentsTests`, `BrokerTests`)
  - `ItemCollectionTests` count tests ×3
- **Duplicates:**
  - `CapabilityGuardTests` ×4 (duplicate `AccountTests`/`ExpenseTests`)
  - `AccountTests.cs:1108`
  - `AccountActionsTests.cs:217`
- **Variant repetition on one constructor path** (note checks, WithId, `CreateWithId` copies):
  - `CorporateActionTests` ×8
  - `TransactionTests` ×3
  - `CreditTests` ×11 (merge into one `Create_SetsProperties`)
- **Getter echoes:** `PotTransactionTests`, `PotTests`, `IncomeTests`, `CorporateActionTests` correlation id.
- **The same rule through every caller:** split/merger numbers at 3 levels; tax-year BRL variants; `HoldingValuationCalculator` groups; `AccountTemplatesTests` → 2 Theories.

### CashFlow Application / Infrastructure
- **Repository and factory tests:**
  - 10 one-line repository getter tests (`CashFlowJsonRepositoryTests.cs:450-621`).
  - Factory branch tests are repeated in 3 factories. Move one copy to a Shared `RemoteStorageFactoryTests`.
- **Domain rules re-asserted in service tests:**
  - Round-up suggestion ×3 (`ExpenseServiceTests.cs:558, 578, 588`); move `:568` down to Domain.
  - Pots withdraw ×3.
  - Settlement inactive / no-participants ×4.
  - Blank-name theories.
- **Wrapper and guard tests:**
  - `AreaParserTests`/`BillStatusParserTests` (wrappers over `EnumParser`).
  - Internal method null guards ×4.
  - Validate-before-lookup order ×2.
  - Rollback duplicate `CashFlowJsonRepositoryTests.cs:208`.

### Investment Application / Infrastructure
- **Whole classes or mirror tests:**
  - Infrastructure `NavigationServiceTests` (22 shape-only/mirror tests; delete the class).
  - `PortfolioAssetSummaryServiceTests.cs:279` (mirror).
  - `XirrCalculationServiceTests.cs:26` (mirror).
- **Domain rules re-asserted:**
  - Application `NavigationServiceTests` ×6.
  - `TransactionServiceMutationTests.cs:494`.
  - Infra `CreditServiceTests.cs:132, :143`.
- **Wiring-only and over-tested paths:**
  - `*_DefaultsToActiveScope` wiring-only tests ×14.
  - Enum-parser wrappers ×8.
  - `AssetPriceLookupService` persistence-failure ×5 → 1.
  - `Serialize_ProducesValidJson`.

### Api / Architecture
- **Endpoint tests duplicating Application or AC tests:**
  - `TitheEndpointsTests` ×6 (copies of `TitheServiceTests`).
  - `TitheViewAcceptanceTests.cs:118, :127`.
  - `CorporateActionsEndpointsTests` ×4 (copies of AC tests).
  - `ExpenseEndpointsTests` ×5 plus 6 → 1 negatives.
  - `IncomesEndpointsTests` ×5 and `StatementDueDateAcceptanceTests` ×3.
- **Weak or echo assertions:**
  - `*_ReturnsOk` with `NotBeNull`/`>= 0` ×7.
  - Watchlist / AssetPriceFetch options echo ×4.
  - Seed read-backs ×7.
  - Filter tests that cannot fail ×2.
- **Framework and plumbing:**
  - Retired-route 404s ×6 (framework routing; the snapshot covers it).
  - Null-body per endpoint ×3 (fold into the `EmptyBodyEndpointsTests` sweep).
  - `GetHealth_AnswersRepeatedProbesWithOk`.
  - DI-singleton test.
- **Architecture:** 2 `Domain_Should_Not_Reference_Infrastructure` facts duplicate `DomainPurityTests`.

### Shared / Integrations / Tools
- **Duplicates and constants:**
  - `UsdBasedExchangeRateProviderTests.cs:195, :209`.
  - Observability ×2.
  - `FromVersion_Is3/Is4`.
  - `ImportReportTests.cs:40` (echo, with a misused `ContainSingle`).
- **Re-tests of an owned rule:**
  - Reflection test `FxRate…:80` and its inspector.
  - `GoogleRetryPolicyTests.cs:84` (backoff owned by `RetryPolicy`).
  - Fee-floor ×2; Bitcoin portfolio ×2.
- **Over-tested or vacuous:**
  - `IncomeMigrator` count echoes ×4 → 1, and `:113` pins a known silent-import bug.
  - Method null guards ×6.
  - Unsupported-provider duplicate.
  - FX serializer round-trips ×3 → 1, and `:45` asserts nothing.
  - `ResumoValidationReader` aliases → 1 Theory.
  - Frankfurter malformed/non-2xx → `Lookup_*` theories.

### Web hooks
- **Per-wrapper copies of shared reducers** (`useAsyncResource`/`useCrudResource`):
  - About 20 loading/retry/fallback copies in thin data hooks.
  - About 15 "constant fallback message" tests.
  - About 14 CRUD retry/propagate copies.
- **Duplicates and setter echoes:**
  - `useCorporateActions` duplicates and validation tables.
  - `useCredits` duplicates, setter echoes and default ×3.
  - `useDialogSubmit` duplicate.
  - `useFieldError` → 1 table.
- **Shared formatter rule re-asserted:** `NETWORK_SAVE_ERROR` per form ×7.
- **Computed expectations:** `useTransactions` month keys are computed with `formatMonthKey`; use literals.

### Web components
- **Duplicate or vacuous tests:**
  - Mislabelled duplicate `AssetSummaryTab.test.tsx:276, :382`.
  - 5 dash tests that cannot fail (`PortfolioHoldingsTab:704-772`).
- **Rules re-asserted through every caller:**
  - Inactive-bank AC ×9 (`selectableBanks` owns it).
  - Duplicate-name per dialog ×6 → `formDialogContract`.
  - Period filter per tab ×8.
  - Tooltip and percentage mirror tests.
- **Implementation details asserted in jsdom:**
  - Recharts internals, SVG and mock props (about 13).
  - Exact `classList` (about 7) and layout/CSS checks (about 4).
  - Gain/loss colour classes per field → one per component.
- **Already covered elsewhere:**
  - `TotalsGrid` sort cycle.
  - Save-in-flight copies ×3.
  - Formatter output and "bold" class in tabs.
  - IncomingGrid AC duplicate.
  - CorporateActionsTab focus pairs.
  - Render-only tests (`SplitPanel`, `LoadingState`, `StatusBadge`, `AggregatedSummaryTab`).
  - `SortableColumnHeader.test.tsx:67`.
- **Fix rather than delete:**
  - Backend-error tests `TransferForm:127` / `BalanceAdjustmentForm:111` do not check the message is under the field.
  - `SortableColumnHeader.test.tsx:83` presses no key.
  - The `overdraftMessage` tests are in the wrong folder.

### Web pages / api / utils / E2E
- **Duplicates:**
  - `ReportingCurrencyPage` ×3, which duplicate the AC test.
  - `HistoricInvestmentsPage` ×2.
  - `AccountScreenPage` hook-rule copies ×2 and discard-guard per form, about 15 → 5.
  - `AppearancePage` ×3.
  - Sort-cycle and `aria-sort` copies in 4 pages.
- **`financialApiClient`:**
  - Per-method ApiError ×5.
  - `toEqual(responseBody)` repeated in 93 places.
  - Currency-query duplicates.
- **Utils and inert tests:**
  - Storage-failure copies ×6 (`safeStorage` owns them).
  - `currency.test.ts` (constant equals itself).
  - `App.test.tsx:91` never exercises the OS-preference branch.
  - `MensaisPage.test.tsx:416` checks only an input echo.
- **E2E:**
  - Delete `app-loads.spec.ts`; `investment-asset` contains it.
  - Polling fix in `bankless-income-tithe.spec.ts:30`.

## 6. Decisions for the user

1. **Method-argument null-guard tests** (about 16 across projects). Either delete them like constructor guards, or keep them where the guide says "a test for every throw". The guide is ambiguous.
2. **`add-expense.spec.ts:28, :43`**: the 500 is faked with `page.route`, and `react-pages.md` lists both as sanctioned. Delete them and update the guide?
3. **CSS-only UI state** in TickerCombobox (active option), Sidebar (current page) and ColumnFilter (active filter). Adding `aria-activedescendant` / `aria-current` / `aria-pressed` is a product change; the tests would then assert those instead of classes.
4. **`PotTests.cs:133` `Update_NameWithSurroundingSpaces_StoresItAsGiven`**: `Create` trims and `Update` doesn't. Is that a bug locked in by a test?
5. **`IncomeMigratorTests.cs:113`** pins a known silent-import ordering bug as expected behaviour.

## 7. Guide fixes

- `react-hooks.md` contradicts itself: "What to test" asks every data hook for loading, error and retry, while "When to skip" says don't re-test the `useAsyncResource` reducer.
- `google-sdk-wrappers` names `FakeDriveHandler.cs`; the file is now `FakeGoogleApiHandler.cs`.
- `react-pages.md` / `e2e-environment.md` E2E list, depending on decision 2.
- `test-hygiene.sh` scans added lines only, so the legacy hits in §2 were never reported. After §2 is cleaned, a one-off full scan (or a scheduled one in nightly) would keep the suite clean.

## 8. Housekeeping

`Tests/Financial.App.E2ETests` and `Tests/Financial.Presentation.Tests` hold only untracked `bin/`/`obj/` output from the removed WPF projects. Delete the local folders; there is nothing to commit.

## 9. Proposed PR sequence

Within the 8-code-file / ~15-file target, except identical sweeps:

1. `fix:` **log redaction** in `AssetPriceLookupService` + its tests (defect 1).
2. `test:` **trait scanner** brace fix + `EmptyBodyEndpointsTests` trait + retags of the Integration→Unit classes (defect 3, §3 traits).
3. `fix:` **TimeProvider** for the `DebouncedJsonStorage` flush timeout and the Frankfurter budget; FX loader via `ILogger`; then the time-controlled test rewrites in Shared (defects 4–6, §2 Shared rows).
4. `test:` **wall-clock clean-up** in the repository factory and repository tests (both contexts), `TimeProvider.System` → `TestClock`, the web unpinned dates and the AnnualSummary 2027 bomb (defect 2).
5. `test:` **AC traceability**: web retitles + Api acceptance tags/moves (defects 7–8).
6. `test:` **shared doubles** consolidation + silent-default doubles.
7. `test:` **FluentProvider render swap** (an identical sweep, one PR).
8. `test:` **owned-module mocks** → `apiClient`/host seams (Api stubs, TaxPage, CorporateActionsTab, hook mocks).
9. **Low-value deletions**, one PR per area in §5: Domain, CashFlow, Investment, Api, Shared/Tools, Web hooks, Web components, Web pages/api.
10. `test:` **missing coverage** (§4), including Domain `ConstructorGuardTests` and the failed-span additions.

**Mutation thresholds (10.3):** the deletions in step 9 can lower mutation scores where a deleted test was the only killer of a mutant. Finish steps 1–9 before measuring the steady week. The threshold week then restarts after the last deletion PR merges.
