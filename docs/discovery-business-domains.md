# Business/Domain Analysis — Current State

Read-only business/domain discovery pass over the existing (brownfield) codebase, produced as the third step of introducing Spec-Driven Development (SDD). Identifies the major business domains/capabilities represented in the code, with business rules classified by evidence strength, followed by a clarifications log resolving the open questions raised during the pass with the product owner (the user).

Classifications used throughout:

- **CONFIRMED REQUIREMENT** — explicitly documented in a PRD/`context.md`/spreadsheet-origin doc, or has a test explicitly asserting the rule by name/intent — or, after this pass, explicitly confirmed by the product owner (see Clarifications Log).
- **OBSERVED BEHAVIOR** — implemented in code, no doc/test explicitly frames it as a requirement.
- **INFERRED** — interpretation of implementation intent, not directly stated.
- **UNKNOWN** — cannot be established from the repo alone.

No files were modified as part of the analysis itself.

---

## INVESTMENT BOUNDED CONTEXT

### 1. Broker / Portfolio / Asset Hierarchy

**Purpose:** Containment model for holdings — `Broker` (e.g. Trading212, XPI) → `Portfolio`s → `Asset`s.

**Main entities:** `Investments` (aggregate root, holds independent `ActiveBrokers`/`HistoricBrokers` collections), `Broker` (Name, Currency, Portfolios), `Portfolio` (Name, Assets; constructor `internal` — only `Broker` can create one), `Asset` (Name, ISIN, Exchange, Ticker, Country, LocalTypeCode, Class, plus owned Transactions/Credits/PriceHistory).

**Value objects:** `CountryCode`, `GlobalAssetClass` (classification enums), `AssetPriceSnapshot` (Date, Price, IsManual).

**Relationships:** Strict `Investments 1—* Broker 1—* Portfolio 1—* Asset`. No cross-bounded-context references (verified empirically — zero `using Financial.CashFlow.*` in Investment.Domain/.Application).

**Business rules:**
- Asset classification derives from `(CountryCode, LocalTypeCode)` — **CONFIRMED REQUIREMENT** (`context.md`, explicit test `AssetTests.Create_FiveArgOverload_ResolvesAssetClassFromCountryAndLocalTypeCode`).
- Active/Historic are two independent top-level collections; an asset never appears in both — **CONFIRMED REQUIREMENT** (P10 §4, explicit metric).
- Position state (`Long`/`Flat`/`Short`) is derived purely from `Quantity` sign, replacing a prior binary `Active` flag — **CONFIRMED REQUIREMENT** (P10 §2, explicit).
- `Broker.AddPortfolio` is get-or-create by name — **OBSERVED BEHAVIOR**.
- Only manually-entered (`IsManual=true`) price snapshots can be removed — **CONFIRMED REQUIREMENT (business decision)**. See Clarification #9.

**Application services:** `NavigationService`/`Mapper`, `PortfolioAssetSummaryService`/`Builder`, `BrokerBreakdownService`/`Builder`, `AssetMutationHelper`, `AssetCashFlowBuilder`.

**APIs:** `AssetsController` (`/assets`), `NavigationController`, `SummaryController`.

**WPF:** `MainNavigationViewModelBase` → `MainNavigationViewModel`/`MainNavigationViewModelHistoric` (parallel Active/Historic ViewModels), `AssetDetailsViewModel`.

**Web:** `InvestmentTree.tsx`, `ActiveInvestmentsPage.tsx`, `HistoricInvestmentsPage.tsx`, `SelectedNodeContext.tsx`.

**Tests:** `BrokerTests`, `PortfolioTests`, `AssetTests`, `InvestmentsTests` (Domain, pure unit); `NavigationServiceTests`, `PortfolioAssetSummaryServiceTests` (Application, stub-based).

**External integrations:** None for this capability itself.

---

### 2. Transaction Recording (Buy/Sell)

**Purpose:** Records buy/sell events; derives running quantity, average cost, realized gain.

**Main entities:** `Transaction` (Id, Date, Type, Quantity, UnitPrice, Fees; `TotalPrice` computed; three factories — `Create`, `CreateWithId`, `CreateFromTotal`), `Transactions` (owned collection maintaining running `Quantity`/`AveragePrice`/`RealizedCapitalGain`/`AverageSellPrice`).

**Business rules:**
- **Weighted-average cost method** for `AveragePrice` on every Buy — **CONFIRMED REQUIREMENT**, explicitly asserted/commented in `TransactionsTests.Add_Buy_UpdatesAveragePriceAndQuantity`.
- Selling does not change `AveragePrice` — **CONFIRMED REQUIREMENT** (`Add_Sell_DecreasesQuantityAndKeepsAveragePrice`).
- `RealizedCapitalGain` accumulates `TotalPrice − (Quantity × AveragePrice-at-sale)` per sell — **CONFIRMED REQUIREMENT**.
- `AverageSellPrice` = weighted average of sell totals, `null` with no sales — **CONFIRMED REQUIREMENT**.
- Short-selling (negative Quantity) is allowed with no guard — **CONFIRMED REQUIREMENT as an intentional supported state, accepted as-is for now** (P10 explicitly frames Short as a recognized position type). See Clarification #8.
- `Update`/`RemoveById` replay all transactions in **list insertion order, not date order**, to rebuild derived state, specifically **so `AveragePrice` can be recalculated** — **CONFIRMED REQUIREMENT (implementation decision, purpose confirmed)**. See Clarification #7.
- No negative-value guards on Quantity/UnitPrice/Fees at Domain/Application layer — **OBSERVED BEHAVIOR**; client-side validation unverified.

**Application services:** `TransactionService` (`ITransactionService`/`ITransactionQueryService`), routed through shared `AssetMutationHelper`.

**APIs:** `TransactionsController` (`/transactions`).

**WPF:** `TransactionDialog.xaml`, `TransactionDialogViewModel`.

**Web:** `TransactionsTab.tsx`, `useTransactions.ts`.

**Tests:** `TransactionTests`, `TransactionsTests` (Domain, thorough); `TransactionServiceMutationTests`, `TransactionServiceQueryTests` (Application).

**External integrations:** None.

---

### 3. Credits (Dividends & Rent — manual entry)

**Purpose:** Records non-trade income (dividends, rent) against an Asset.

**Main entity:** `Credit` (Id, Date, Type [`Dividend`/`Rent`], Value) — owned flat list on `Asset`.

**Relationships:** `Asset.RealizedGainLoss = Transactions.RealizedCapitalGain + Credits.Sum(Value)` — an explicit code comment states this composition is deliberate ("Transactions itself has no knowledge of Credits").

**Business rules:** `CreditType` limited to Dividend/Rent — **OBSERVED BEHAVIOR**; Update/Delete require non-empty Guid — **OBSERVED BEHAVIOR**.

**Application services:** `CreditService` (via `AssetMutationHelper`), `CreditTypeParser`.

**APIs:** `CreditsController` (`/credits`), full CRUD + by-broker/by-portfolio.

**WPF:** `CreditDialog.xaml`, extensive charting support (`CreditsChartBuilder`, `CreditsViewState`, `CreditsFilterOptionViewModel`, `CreditsMonthTypeTotals`).

**Web:** `CreditsTab.tsx`, `useCredits.ts`.

**Tests:** `CreditTests`, `CreditServiceTests`, `CreditTypeParserTests`.

**External integrations:** None for manual entry.

---

### 4. Asset Price Fetching & Fallback Strategy

**Purpose:** Resolve current market price per asset, dispatched by `GlobalAssetClass`.

**Dispatch rule — CONFIRMED REQUIREMENT** (matches P08/P09 exactly):
- `GlobalAssetClass.Bond` → `BondAssetPriceFetcher` → `StatusInvestFinanceService`, by asset **Name**.
- `Cryptocurrency` → `CryptocurrencyAssetPriceFetcher` → `IFinanceService.GetAssetValue(brokerCurrency, Ticker)`.
- Everything else → `StandardAssetPriceFetcher` → `IFinanceService.GetAssetValue(Exchange, Ticker)`.

**Fallback chain:** `FallbackFinanceService(primary: GoogleFinanceService, fallback: YahooFinanceService)`. Yahoo is only retried for exchange-based (non-crypto) requests. **Reason confirmed (Clarification #1): Google does not have cryptocurrency price data** — crypto pricing is architecturally excluded from this Google→Yahoo exchange-based fallback chain for that reason, now **CONFIRMED REQUIREMENT**.

Preconditions (Name for Bond, BrokerName for Crypto, Exchange for Standard) are hard-thrown `ArgumentException`s — **OBSERVED BEHAVIOR**.

**APIs:** `AssetPricesController` (`/prices`), `AssetPriceFetchController` (`/asset-price-fetch`).

**WPF:** `AssetPriceView.xaml`, `PriceDialogViewModel`, `AssetPriceFetchViewModel`.

**Web:** `PriceHistoryTab.tsx`, `usePriceHistory.ts`.

**Tests:** Extensive — `AssetPriceServiceTests`, `BondAssetPriceFetcherTests`, `CryptocurrencyAssetPriceFetcherTests`, `FallbackFinanceServiceTests`, `StandardAssetPriceFetcherTests`, `GoogleFinanceServiceTests`, `YahooFinanceServiceTests`, `StatusInvestFinanceServiceTests`, plus scraping/retry-policy tests, some of which appear to be live-verification tests against real external sites (uncertain if CI-run or manual-only).

**External integrations:** Google Finance (HTML scrape), Yahoo Finance (public unauthenticated endpoint), Status Invest (HTML scrape — Tesouro Direto's own site was evaluated and rejected because it serves a Cloudflare JS challenge unreachable by the scraper — **CONFIRMED REQUIREMENT**, explicit in P09 §2).

---

### 5. Dividend Tracking & Valuation

**Purpose:** Track historical dividend/rent payments per ticker; compute a "fair buy price" from trailing yield.

**Value objects:** `DividendValue` (Date, Value, DividendType).

**Business rules:**
- 5-year lookback, current partial year excluded (not prorated) — **CONFIRMED REQUIREMENT** (matches P01's "Average Dividend: last 5 years" AC).
- `RequiredYield = 6%` hardcoded denominator for `PriceMaxBuy = averageDividend / RequiredYield` — **CONFIRMED REQUIREMENT (business decision — a rule the product owner learned and applies deliberately)**. See Clarification #2.

**Bug identified (Clarification #4):** `DividendDataSourceAdapter` accepts an `(exchange, ticker)` signature but silently forwards only `ticker` — the product owner confirms this is an **error**: the method **should not accept an `exchange` parameter at all**. The parameter is unnecessary and its presence (unused, silently dropped) is a defect to fix, not an intentional exchange-agnostic design. Flagged for follow-up (not fixed in this read-only pass).

- Divide-by-zero guarded, returns 0 — **OBSERVED BEHAVIOR**.

**Application services:** `DividendService`, composing `IDividendDataSource` + `IAssetSnapshotSource` + `DividendValuationRules`.

**APIs:** `DividendsController` (`/dividends`).

**WPF:** `DividendCheckView.xaml`, `DividendCheckViewModel`.

**Web:** `DividendCheckPage.tsx`.

**Tests:** `DividendServiceTests`, `DividendValuationRulesTests`.

---

### 6. Performance Analytics (XIRR, Profit)

**Purpose:** Money-weighted return and simple cost-basis profit.

**Business rules (standard financial formulas — OBSERVED BEHAVIOR, not project-specific policy):**
- `XirrCalculator`: Newton-Raphson, 100 max iterations, `1e-7` tolerance, 10% initial guess, 365-day year; returns `null` with <2 cash flows or if derivative hits zero (no bisection fallback).
- `ProfitCalculator.HasCostBasis` requires `averagePrice > 0 && quantity > 0`; `CalculateResultFraction` returns `0` with no cost basis while `CalculateProfitPercent` returns `null` — inconsistent null-vs-zero convention within the same class. **Confirmed as an implementation decision (Clarification #6)** — accepted as-is, not a defect requiring correction.

**Frontend `xirr.ts` duplication — investigated during clarification (Clarification #5):** `Financial.Web/src/utils/xirr.ts` is **not** dead/forgotten code. It is actively imported by `Financial.Web/src/components/PortfolioSummaryTab.tsx` and used to compute XIRR **per table row, client-side**, from cash-flow arrays already present in the API response (`item.cashFlows` + a synthetic terminal-value entry) — avoiding a separate `/xirr` API round-trip per row in a potentially large table. This is a genuine, live, second independent implementation of the same algorithm as the backend `XirrCalculator` (Domain), not a leftover. The divergence risk remains real: if the two implementations' edge-case handling (e.g. the `<2 cash flows → null` guard, iteration/tolerance parameters) drift apart, the Portfolio Summary table and any backend-driven XIRR display could disagree. No action taken in this pass; flagged for whoever next touches XIRR logic on either side.

**Application services:** `XirrCalculationService`, `ProfitCalculationService`, `AssetCashFlowBuilder`.

**APIs:** `XirrController` (`/xirr`).

**Tests:** `XirrCalculatorTests`, `ProfitCalculatorTests`, `XirrCalculationServiceTests`.

---

### 7. Portfolio/Broker/Asset Summary & Credits-Adjusted Return

**Purpose:** Aggregate value/cost basis and dividend/rent-adjusted profit at every hierarchy level; broker composition breakdown.

**Business rule — CONFIRMED REQUIREMENT** (P02, explicit): `TotalCredits` = sum of all Dividend+Rent Credit values on an asset; the "% Profit w/ Credits" metric was added specifically because plain "% Profit" was identified as understating return for income-producing assets.

**Business rule:** `CreditFrequencyAnalyzer.DetectFrequencyPerYear` infers payment cadence (Monthly/Quarterly/Four-monthly/irregular) from average month-gap, using thresholds ≤1.5/≤3.5/≤5.0 months — **CONFIRMED REQUIREMENT (business decision)**. See Clarification #3.

**Application services:** `SummaryService`, `PortfolioAssetSummaryService`/`Builder`, `BrokerBreakdownService`/`Builder`, `CreditService` (full CRUD).

**APIs:** `SummaryController`, `CreditsController`.

**WPF/Web:** as listed under Credits above, plus `AssetSummaryTab.tsx`, `PortfolioSummaryTab.tsx`, `BrokerBreakdownCharts.tsx`.

**Tests:** `CreditServiceTests`, `PortfolioAssetSummaryServiceTests`, `SummaryServiceTests`, `CreditFrequencyAnalyzerTests`.

---

### 8. Cryptocurrency Handling

**Purpose:** First-class `GlobalAssetClass` for crypto, priced by broker-currency rather than exchange/ticker.

**Business rule — CONFIRMED REQUIREMENT** (`context.md`, matches code exactly): crypto is held in a dedicated broker (Coinbase); no `LocalTypeCode` mapping row exists for it — the class is set directly on the asset instead of derived. See §4 for why crypto pricing bypasses the Google→Yahoo fallback chain (Google has no crypto price data).

**Tests:** `CryptocurrencyAssetPriceFetcherTests`, `GoogleFinanceCryptocurrencyUrlTests`.

---

### 9. Country/Local-Type → GlobalAssetClass Mapping

**Purpose:** Normalize per-country local asset-type strings into one shared taxonomy.

**Business rule — CONFIRMED REQUIREMENT** (matches `context.md`'s documented examples one-for-one).

Unmapped/blank `LocalTypeCode` resolves to `Unknown` (fallback, not exception), case-insensitive match — **OBSERVED BEHAVIOR**.

---

## CASHFLOW BOUNDED CONTEXT

### 10. Bank Entity & Balance Management

**Purpose:** Track real-world bank accounts and compute a running monthly balance from linked incomes/expenses/transfers/adjustments.

**Main entity:** `Bank` (Id, Name, RoundUpEnabled, OpeningBalance, OpeningBalanceDate).

**Business rules:**
- Name required, non-blank; opening balance ≥ 0 — **CONFIRMED REQUIREMENT**.
- Balance = `OpeningBalance + income − (expense + roundUp) + transferIn − transferOut + adjustments`, windowed to `[OpeningBalanceDate, asOfDate]` — **CONFIRMED REQUIREMENT** for round-up subtraction (P13 AC); overall formula **OBSERVED BEHAVIOR**.
- Exactly 3 banks exist by design (Barclays, Trading212, Chase), seeded once, no in-app bank-creation screen — **CONFIRMED REQUIREMENT** (P13 §6, explicit "Out of Scope").

**Application service:** `BankService`. **APIs:** `BanksController`. **WPF:** `MonthlyViewModel.BuildBankTotals`, `BanksGridView.xaml`. **Web:** `useBankOperations.ts`, `BanksGrid.tsx`. **Tests:** `BankTests` + Application/API tests. **External integrations:** None.

---

### 11. Bank Round-Up

Unrelated to the earlier-known "Value−RoundUp backwards" bug in Investment-domain broker balances — this is an independent CashFlow `Bank`/`Expense` model that happens to reuse the same real-world institution names.

**Business rules — verified consistent across Domain, WPF, and Web (no sign/direction bug currently present):**
- Suggested round-up = `ceil(Value) − Value` — **CONFIRMED REQUIREMENT** (P13 AC).
- Only settable on a positive-value, bank-paid expense where the bank has `RoundUpEnabled=true` — **CONFIRMED REQUIREMENT** (P13 §6 F02).
- Range `£0.00`–`£0.99` inclusive — **CONFIRMED REQUIREMENT**.
- "Sticky" — never auto-recalculated on value edit — **CONFIRMED REQUIREMENT** (P13 §6 F02).
- Bank's round-up total shown separately; balance subtracts both `Value` and `RoundUpAmount` — **CONFIRMED REQUIREMENT** (P13 §9, all ACs complete).

**External integrations:** None — explicitly no live bank-API sync (P13 §7 Out of Scope).

---

### 12. Bank Transfers

**Purpose:** Record money moved between the user's own tracked banks.

**Main entity:** `Transfer` (Id, Date, SourceBank, DestinationBank, Amount, Note).

**Business rules:** Source ≠ destination, both required, amount > 0 — domain-validated.

**Notable gap:** No overdraft/negative-balance check on transfers, unlike Reserve withdrawals. **Confirmed acceptable for now (Clarification #11)** — the product owner is prioritizing reaching an MVP to retire the spreadsheet; this asymmetry is not a defect to fix at this stage.

**Application service:** `TransferService`. **APIs:** `TransfersController`. **WPF:** `BankOperationRow.cs`. **Web:** `TransferForm.tsx`, `useTransferForm.ts`.

---

### 13. Balance Adjustment / Reconciliation

**Purpose:** Reconcile a bank's computed balance against a real-world value; system computes and stores the `Delta`.

**Main entity:** `BalanceAdjustment` (Id, Date, Bank, TargetBalance, Delta, Note).

**Business rules:** `TargetBalance ≥ 0` — **CONFIRMED REQUIREMENT**. `Delta` computed server-side, can be negative — **OBSERVED BEHAVIOR**.

**Application service:** `BalanceAdjustmentService`. **API:** nested under `BanksController`. **WPF:** `BalanceAdjustmentFormView.xaml`. **Web:** `BalanceAdjustmentForm.tsx`.

---

### 14. Credit Card Entity, Card Statements & Expense Settlement

**Purpose:** Track a small fixed set of credit cards and monthly statements that aggregate a card's charges into a payable total, with an explicit paid/unpaid settlement transition that converts charges into bank-linked expenses — directly reflecting the spreadsheet's original rule that a credit-card charge should not reduce the bank balance until the statement is paid.

**Main entities:** `CreditCard` (Id, Name, IsActive, NextInvoiceDueDate — no Bank reference, intentional per P13 §7), `CardStatement` (Id, CreditCard ref, Year, Month, IsPaid).

**Business rules:**
- An expense has exactly one of `PaymentSourceBank`/`CreditCard`, never both/neither — **CONFIRMED REQUIREMENT** (P12, enforced on every Create/Update).
- An unsettled card charge doesn't count toward any bank balance until settled — **CONFIRMED REQUIREMENT** (P12 Executive Summary).
- Marking a statement paid requires choosing which bank paid it; that bank + today's date cascades onto every matched charge — **CONFIRMED REQUIREMENT** (P12's "auditable transition").
- "Unmark paid" fully reverses the cascade — **CONFIRMED REQUIREMENT** (P12 explicitly closes this gap).
- Matching statement↔charges is by `CreditCard.Id` + `InvoiceDate.Year/Month`, not a stored FK — **CONFIRMED REQUIREMENT** ("derive, don't store" pattern, explicit in P12).
- `InvoiceDate` fixed at creation, never changes across Settle/Unsettle — **CONFIRMED REQUIREMENT** (explicit doc comment).

**Bug identified (Clarification #12):** Re-invoking mark-paid on an already-paid statement (or unmark-paid on an already-unpaid one) currently silently no-ops, returning current state with no feedback. The product owner confirms **this is a mistake — it should surface feedback to the user**. Flagged for follow-up (not fixed in this read-only pass).

`OverdraftConfirmationRequiredException` belongs exclusively to Reserve-bucket withdrawals (§21), not this capability.

**Application services:** `CardStatementService` (the one Application-layer class anywhere in the backend using `ILogger`). **APIs:** `CardStatementsController`, `CreditCardsController`. **WPF:** `CreditCardsGridView`, `CreditCardExpensesView.xaml`. **Web:** `CardsGrid.tsx`, `useCreditCards.ts`. **Tests:** `CardStatementTests`, `CardStatementServiceTests`, `CardStatementsEndpointsTests`.

---

### 15. Credit Card Invoice Dates (P25)

**UNKNOWN / not deeply investigated.** Mechanism confirmed to exist; specific P25 business rules were not read in this pass.

---

### 16. Expense Tracking

**Purpose:** Records any household spend, tagged to a Category and either a Bank or CreditCard, with derived payment-state/reporting-date logic. Originates from the legacy `Despesas.xlsx` monthly rows.

**Main entity:** `Expense` (Id, Date, Description, Value [can be negative — reimbursement], Category [required], PaymentSourceBank [nullable], CreditCard [nullable], ChargeDate/InvoiceDate [nullable, card-only], RoundUpAmount [nullable], CountsAsTithe [bool, default true]).

**Derived, not stored:** `PaymentStatus`, `RoundUpSuggestion`, `IsInvestment` (delegates to `Category.IsInvestment`), `ReportingDate`.

**Business rules:**
- Payment shape (exactly one of Bank/CreditCard) — **CONFIRMED REQUIREMENT** (see §14).
- Settled expenses' bank/card fields immutable except through unsettle — **CONFIRMED REQUIREMENT** (P12).
- New/updated expenses may only reference an *active* Category — **CONFIRMED REQUIREMENT** (P30-F02).
- `CountsAsTithe` defaults true, independently toggleable per-expense to exclude an offering from the tithe-paid total — **CONFIRMED REQUIREMENT** (P33-F02).

**Application service:** `ExpenseService`. **APIs:** `ExpensesController`. **WPF:** `ExpenseFormView.xaml`. **Web:** `ExpensesSection.tsx`, `ExpenseForm.tsx`, `useExpenseForm.ts`. **Tests:** full-stack coverage confirmed.

---

### 17. Income & Income Sources

**Purpose:** Records income and its origin; feeds bank balances and tithe/annual-summary.

**Main entities:** `Income` (Id, Date, IncomeSource ref, GrossValue [nullable], NetValue, Bank [nullable since P33-F01], Description [nullable]), `IncomeSource` (Id, Name, IsActive, Group).

**Business rules:**
- Bank optional; bank-less income excluded from bank balances but still contributes to tithe base — **CONFIRMED REQUIREMENT** (P33-F01).
- `NetValue ≥ 0` — **CONFIRMED REQUIREMENT**.
- Submitted IncomeSourceId must resolve to a seeded source; inactive sources still accepted — **CONFIRMED REQUIREMENT** (P26-F02).
- `IncomeSource` has no CRUD — seeded once — **CONFIRMED REQUIREMENT** (P26).

**Reference storage pattern confirmed (Clarification #21):** P26's original text described `IncomeSource` as a plain string, not used as a foreign key. That design was **superseded** — the product owner confirms **all entity references in this codebase should work the same way: GUID stored in the JSON, full object reference when loaded in memory** — matching the `Category`/`Bank` reference-converter pattern. P26's PRD text describing a bare-string design is stale.

**Application services:** `IncomeService` (full CRUD), `IncomeSourceService` (read-only). **APIs:** `IncomesController`, `IncomeSourcesController` (GET only). **WPF:** `IncomeSectionView`. **Web:** `IncomeSection.tsx`, `IncomeForm.tsx`. **Tests:** full coverage confirmed.

---

### 18. Recurring Bills ("Mensais")

**Purpose:** Tracks recurring monthly bills (UK + Brazil), mirroring the spreadsheet's "Mensais" tab.

**Main entity:** `RecurringBill` (Id, DueDay [1–31], Description, Value, Area, Note, NitNumber [nullable, Brazil-only], MinimumWageValue [nullable], Status: Unset/Scheduled/Paid).

**Business rules:**
- `NitNumber`/`MinimumWageValue` populated only by the spreadsheet importer — **CONFIRMED REQUIREMENT**.
- Three-state Status model matches the spreadsheet's flag column — **CONFIRMED**.

**Usage clarified (Clarification #22):** `ResetAllToUnsetAsync` is a documentation gap, not a defect — the product owner confirms it's **intended to be run once a month, at the beginning of the month, to reset all bills to unset** before that month's payment tracking begins. Should be documented as such going forward.

**Application service:** `MensaisService`. **API:** `MensaisController`. **WPF:** `AddBillFormView.xaml`, `BillTableView.xaml`. **Web:** `useMensais.ts`.

---

### 19. Categories

**Purpose:** Classifies expenses into 14 fixed household budget categories, with two flags (`IsInvestment`, `IsTithe`) driving downstream logic.

**Main entity:** `Category` (Id, Name, Active, IsInvestment, IsTithe).

**Business rules:**
- Exactly 14 categories seeded once, matching the spreadsheet verbatim — **CONFIRMED REQUIREMENT** (P30-F01).
- No create/update/delete at any layer — **CONFIRMED REQUIREMENT** (P30).
- An inactive category stays valid for historical references but rejected for new expenses — **CONFIRMED REQUIREMENT** (P30-F02).

**"Viagem" naming — resolved (Clarification #20):** The product owner confirms the Category `Viagem` and the (separately documented) Reserve-context "Viagem" label **are the same thing** — the old spreadsheet name carried into the new app unchanged; the spreadsheet still uses "Viagem" today. Not a bug or naming collision requiring correction — accepted as-is.

**Application service:** `CategoryService` (read-only). **API:** `CategoriesController` (GET only). **Tests:** `CategoryTests`, `CategoriesEndpointsTests`.

**External integration (import-time only):** spreadsheet importer resolves imported labels against seeded names, including a documented typo-tolerance mapping (`"Casas"`→`"Casa"`, P30-F06).

---

### 20. Tithe Calculation

**Purpose:** Computes 10% tithe on net income and how much has already been paid via Dizimo-categorized expenses, computed on demand, never persisted.

**Ownership — CONFIRMED:** Tithe is unambiguously CashFlow-owned.

**Business rules:**
- `CalculatedTithe` = 10% of the sum of **every** Income's NetValue for the month, regardless of source/bank — **CONFIRMED REQUIREMENT** (P33-F01).
- `TitheBalance = CalculatedTithe − Σ(Expense.Value where Category.IsTithe && Expense.CountsAsTithe)` — **CONFIRMED REQUIREMENT** (P33-F02).

**Clarified distinction (Clarification #19):** The apparent tension between "10% of all household net income" (current, tested implementation) and the spreadsheet's older documented rule of "10% of my wife's wages after tax" is **not a contradiction** — they describe two different concerns:
1. **The overall tithe calculation** is, and always has been, 10% of *all* net income across every source — confirmed correct as implemented.
2. **A separate, distinct rule** governs how the wife's income specifically gets *split into reserve buckets*: that split should be calculated on her income **net of tithe** (i.e., wife's-income-after-tithe is what gets divided among reserve buckets), not on her gross/pre-tithe net income.

This second rule is about Reserve Bucket income-splitting (§21), not Tithe calculation itself. **Open follow-up:** it was not verified in this pass whether the current `ReserveService.PostIncomeSplitAsync` logic actually applies a tithe deduction specifically when splitting the wife's income source, or whether it splits the full net income amount for any income source uniformly. This should be checked against the Reserve capability's code before assuming it's correctly implemented.

**Application service:** `TitheService`. **API:** `TitheController`. **Tests:** `TitheServiceTests`, `TitheEndpointsTests`.

---

### 21. Reserve Buckets

**Purpose:** Split posted income across named percentage-based savings buckets, tracking a running per-bucket balance via signed movements.

**Main entities:** `ReserveBucket` (Id, Name, IsActive, SplitPercentage), `ReserveMovement` (Id, Bucket ref, Amount [signed], Date, Description).

**Business rules:**
- Each active bucket's split share computed via `Round(total × SplitPercentage/100, 2, AwayFromZero)` — **CONFIRMED REQUIREMENT** (P28 + code).
- Buckets are seeded reference data, no CRUD API — **CONFIRMED REQUIREMENT** (P28).
- A withdrawal exceeding a bucket's balance throws `OverdraftConfirmationRequiredException` (409) unless explicitly confirmed — **OBSERVED BEHAVIOR** (confirmed home of this exception).
- Deleting one movement from an income split deletes the entire split group — **OBSERVED BEHAVIOR**, explicit code comment confirms intentional.

**Missing feature identified (Clarification #17):** P28 documents a non-blocking warning when active bucket percentages don't sum to 99.99–100.01%, but this pass could not locate its implementation in `ReserveService`. The product owner confirms **this warning should exist**: it should be **generated server-side and presented to the user in the UI**. This is a confirmed gap between the documented objective and current implementation — flagged for follow-up (not built yet, not fixed in this pass).

**Data-file gap confirmed (Clarification #13):** `data-cashflow.example.json` has no `ReserveBuckets` top-level key. The product owner confirms **it should have one — reserve buckets are part of `data-cashflow.json`**. This is a confirmed defect in the tracked example/template file (a fresh copy would have zero active buckets, and every income-split would fail), not an intentional omission or an alternate storage location. Flagged for follow-up.

**`context.md`'s fixed 4-bucket description — accepted as-is (Clarification #14):** The product owner confirms the discrepancy between `context.md`'s fixed list (Investimento, HouseTreats, Ariana, Gleison) and P28's configurable-entity design is fine for now, since those bucket names come directly from the spreadsheet, where they are the same fixed set.

**Application services:** `ReserveService`, `ReserveBucketService` (read-only). **APIs:** `POST /reserve/income-split`, `POST /reserve/withdrawals`, `GET /reserve/balances`, `GET /reserve/movements`, `GET /reserve-buckets`. **WPF:** `EditReserveMovementFormView.xaml`, `ReservaViewModel`. **Web:** `ReservaPage.tsx`, `useReserva.ts`. **Tests:** `ReserveBucketTests`, `ReserveMovementTests`, `ReserveServiceTests`, `ReserveBucketServiceTests`, `ReserveBucketNameResolverTests`, endpoint tests.

---

### 22. Controle Mãe (Household Loan Ledger with Mother)

**Purpose:** Track an informal, dual-currency personal loan/ledger between the user and their mother.

**Main entity:** `MaeLedgerEntry` (Id, Date, Description, Note, SourceCurrency, BrlValue?, GbpValue?).

**Value objects:** `Currency` enum (BRL/GBP).

**Business rules:**
- On creation, the non-source currency is auto-converted via a historical FX rate for that date; a failed lookup leaves the converted value null — **OBSERVED BEHAVIOR**.
- Only BRL/GBP supported — **OBSERVED BEHAVIOR**.
- `UpdateEntryValuesAsync` allows directly overwriting both currency values post-creation, bypassing FX conversion — **OBSERVED BEHAVIOR**.

**Sign convention now defined (Clarification #15):** Previously genuinely undefined in the repository. The product owner confirms: **a negative value means a debt from the user (Gleison) to his mother; a positive value means the opposite (a debt from his mother to the user)**. This is now a **CONFIRMED REQUIREMENT** and should be captured in any future spec touching this capability.

**Application service:** `ControleMaeService` (depends on `IExchangeRateProvider`). **APIs:** `ControleMaeController`. **WPF:** `ControleMaeView.xaml`. **Web:** `ControleMaePage.tsx`, `useControleMae.ts`. **Tests:** `MaeLedgerEntryTests`, `ControleMaeServiceTests`, `ControleMaeEndpointsTests`, `ControleMaeViewModelTests`.

**External integrations:** `FrankfurterExchangeRateProvider` (historical FX lookup).

---

### 23. CashFlow "Quick-Access" Investment Snapshots

**Purpose confirmed and clarified (Clarification #10):** Manually tracked monthly point-in-time values for named "quick access" investment accounts. The product owner confirms these are **a genuinely different type of investment from the Investment bounded context** — CashFlow's tracked accounts are **quick-access funds, like emergency funds**, that are deliberately **not counted among the long-term, not-quickly-accessible holdings** tracked by the Investment bounded context. This is a real, intentional domain distinction, not an accidental naming collision — though the shared vocabulary ("Investment Account"/"Investment Snapshot" used by both contexts) remains a documentation-clarity risk worth disambiguating in any future spec.

**Main entities:** `InvestmentAccount` (Id, Name, IsActive, IsLiability, Aliases[]), `InvestmentSnapshot` (Id, Account ref, Year, Month, Value).

**Business rules:**
- `GetSnapshotsForMonthAsync` auto-creates a zero-value snapshot for every in-scope account missing one for that month, as a side effect of a read-shaped call — **CONFIRMED REQUIREMENT (intentional)**. See Clarification #18.
- Account scope for past years is derived purely from having ≥1 persisted snapshot that year, independent of current active/disabled status — **CONFIRMED REQUIREMENT** (explicit code comment tied to import behavior).
- `IsLiability` accounts contribute negated to net position in Annual Summary — **OBSERVED BEHAVIOR**.

**Application services:** `InvestmentSnapshotService`, `InvestmentAccountService`. **APIs:** `GET/PUT /investment-snapshots/{year}/{month}`, `InvestmentAccountsController`. **WPF:** `EditSnapshotValueFormView.xaml`, `InvestmentSnapshotsViewModel`. **Web:** `InvestmentSnapshotsPage.tsx`, `useInvestmentSnapshots.ts`. **Tests:** full coverage confirmed. **External integrations:** None — purely manual entry.

---

### 24. Monthly Aggregation

**Purpose:** Single-month household cash-flow view.

**Finding:** There is no `Monthly` Domain entity, no `MonthlyService`, no `/monthly` API endpoint. "Monthly" is a **UI-layer-only composition** — `useMonthly.ts` (Web) separately calls Expenses/Incomes/Banks/CardStatements/Categories/Tithe endpoints and assembles one page-level state; WPF's `MonthlyViewModel` composes the equivalent sub-views in-process directly against the same Application services. It inherits the business rules of whatever it composes; it has none of its own.

---

### 25. Annual Summary (CashFlow)

**Purpose:** Server-computed yearly view of category totals, income summary, investment net-position, and historical multi-year averages. Confirmed CashFlow-only — no equivalent Investment-context route exists.

**Value object:** `MonthlySeries` (12-element decimal array with `Sum()`/`Average()`/`DiffsFrom()`/`Add()`) — the one genuine Value Object identified across this whole CashFlow slice.

**Business rules:**
- Server-side computation (not client-side) — **CONFIRMED REQUIREMENT** (P19).
- `Resultado = salaryAfterTaxes − totalDespesas + investimentoCategoryValue` — the Investimento category is added back because it's already counted once inside total expenses, and money moved into an investment account isn't consumption — **CONFIRMED REQUIREMENT**, explicit in code comment.
- Two parallel series per category/year (`Display` includes the current partial month; `ForAverage` excludes it) — **OBSERVED BEHAVIOR**, deliberate design to avoid skewing averages.
- Investment net-position averages/sums are never rounded, explicitly to stay byte-identical to the pre-refactor investment-diffs output — **OBSERVED BEHAVIOR, documented rationale**.

**2017 special case resolved (Clarification #16):** `NumberOfMonthsForAverage` treats 2017 as an 11-month year. The product owner confirms this is a **known historical data artifact, not a bug**: **they moved to the UK that month, and all UK/GBP-denominated expense tracking starts from February 2017** — so 2017 genuinely only has 11 months of tracked data. Now **CONFIRMED REQUIREMENT** and should be documented as such in code/spec going forward.

**Application service:** `AnnualSummaryService`. **APIs:** `GET /annual-summary/{year}/category-totals`, etc. **WPF:** `AnnualSummaryView.xaml`. **Web:** `AnnualSummaryPage.tsx`, `useAnnualSummary.ts`. **Tests:** `AnnualSummaryServiceTests`, `AnnualSummaryEndpointsTests`, `AnnualSummaryViewModelTests`.

---

### 26. Async Persistence (P31) — cross-cutting infrastructure, not a business capability

Implemented once in `Financial.Shared.Infrastructure`, wired symmetrically into both bounded contexts. Fully covered in `docs/discovery-architecture.md` — noted here only to confirm it is not a business domain in its own right.

---

## Clarifications Log

Answers provided directly by the product owner, resolving the open questions raised during this discovery pass. Where an answer changes a rule's classification, the relevant domain section above has been updated accordingly.

| # | Question (summary) | Resolution |
|---|---|---|
| 1 | Why does the price-fetch fallback chain exclude crypto from the Google→Yahoo retry? | **Google does not have cryptocurrency price data.** Now CONFIRMED REQUIREMENT. |
| 2 | Is the 6% required-yield constant for "price max buy" intentional? | **Business decision** — a rule the product owner learned and applies deliberately. |
| 3 | Are the 1.5/3.5/5.0-month credit-frequency thresholds intentional? | **Business decision.** |
| 4 | Is silently dropping the `exchange` param in `DividendDataSourceAdapter` intentional? | **Error** — the method should not accept an `exchange` parameter at all. Confirmed defect; not fixed in this pass. |
| 5 | Is the frontend `xirr.ts` dead code that should have been removed? | **No — actively used and needed.** Investigated: imported by `PortfolioSummaryTab.tsx`, computes per-row XIRR client-side from already-fetched cash-flow data. Not dead code. Divergence risk from the backend implementation remains a live concern. |
| 6 | Is `ProfitCalculator`'s null-vs-zero inconsistency intentional? | **Implementation decision** — accepted as-is. |
| 7 | Why does transaction replay use insertion order, not date order? | **To make `AveragePrice` recalculable** — confirmed intentional purpose of the rebuild mechanism. |
| 8 | Is unconditional short-selling (no guard) intended? | **Fine as-is for now.** |
| 9 | Why can only manually-entered price snapshots be removed? | **Business decision.** |
| 10 | Is "Investment Account"/"Investment Snapshot" the same concept in both contexts? | **No — genuinely different.** CashFlow's are quick-access funds (e.g. emergency funds), explicitly separate from the Investment context's long-term, not-quickly-accessible holdings. |
| 11 | Should Transfers/Balance Adjustments get overdraft protection like Reserve withdrawals? | **Fine as-is for now** — priority is reaching an MVP to retire the spreadsheets. |
| 12 | Is silent no-op on re-mark/re-unmark card statements intended? | **Mistake — it should surface feedback to the user.** Confirmed defect; not fixed in this pass. |
| 13 | Should `data-cashflow.example.json` include a `ReserveBuckets` key? | **Yes — it should, and is part of `data-cashflow.json`.** Confirmed defect in the tracked example file; not fixed in this pass. |
| 14 | Should `context.md`'s fixed 4-bucket list be updated to describe buckets as configurable? | **Fine for now** — those names come directly from the spreadsheet, where they're the same fixed set. |
| 15 | What does a positive/negative Controle Mãe value mean? | **Previously genuinely undefined. Now defined:** negative = debt from the user to his mother; positive = the opposite (debt from mother to user). |
| 16 | Why does 2017 use 11 months instead of 12 in Annual Summary averages? | **Known historical artifact, not a bug** — the product owner moved to the UK that month; GBP expense tracking starts Feb 2017. |
| 17 | Is the P28 percentage-sum warning implemented anywhere? | **No — it's missing.** Confirmed it should be generated server-side and presented to the user in the UI. Confirmed gap; not built in this pass. |
| 18 | Is the write-side-effect on `GetSnapshotsForMonthAsync` (a GET call) intentional? | **Intentional.** |
| 19 | Does "10% of net income" contradict the spreadsheet's "10% of wife's wages" rule? | **Not a contradiction — two different rules.** Overall tithe is always 10% of all household net income (confirmed correct as implemented). Separately, the wife's income specifically should be split into reserve buckets net of tithe — **not verified whether this second rule is actually implemented in `ReserveService`**; flagged as a follow-up to check. |
| 20 | Do "Viagem" (Category) and "Viagem" (Reserve-context label) mean the same thing? | **Yes — same thing.** Old spreadsheet name carried into the new app; the spreadsheet still uses "Viagem" today. |
| 21 | Was P26's "IncomeSource as plain string" design superseded by a GUID-reference pattern? | **Yes, superseded.** Confirmed standard for this codebase: all entity references store a GUID in JSON, resolve to a full object reference in memory (same as Category/Bank). |
| 22 | What triggers `RecurringBill.ResetAllToUnsetAsync`? | **Missing documentation, not a defect.** Intended to run once a month, at the start of the month, to reset all bills to unset before that month's tracking begins. |

## Follow-ups identified during clarification (not actioned in this read-only pass)

These are confirmed defects, missing features, or open verification items surfaced by the product owner's answers above — listed here for prioritization, not fixed as part of this discovery pass:

1. **`DividendDataSourceAdapter`** should drop its unused `exchange` parameter (Clarification #4).
2. **Card statement mark-paid/unmark-paid** should surface user feedback instead of silently no-opping when re-invoked on a statement already in that state (Clarification #12).
3. **`data-cashflow.example.json`** is missing its `ReserveBuckets` seed data (Clarification #13) — a fresh copy of the template currently cannot perform an income split.
4. **Reserve bucket percentage-sum warning** (P28 objective) is not implemented server-side or surfaced in either UI (Clarification #17).
5. **Wife's-income-net-of-tithe bucket-split rule** (Clarification #19) — needs verification against `ReserveService.PostIncomeSplitAsync` to confirm whether this rule is actually applied today, or whether it's an unimplemented requirement.
6. **`RecurringBill.ResetAllToUnsetAsync`'s monthly-reset purpose** should be documented in code (Clarification #22).
7. **Frontend/backend XIRR duplication** (`xirr.ts` vs. `XirrCalculator`) remains a live divergence risk — confirmed both are actively used, not resolved (Clarification #5).
