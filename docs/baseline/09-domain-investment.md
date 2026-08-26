# Domain: Investment Bounded Context

See legend in [README.md](README.md). Covers business capabilities implemented in `Financial.Investment.Domain`/`.Application`/`.Infrastructure`. No cross-references to the CashFlow context exist anywhere in this code (verified).

## Broker / Portfolio / Asset Hierarchy

**Purpose:** Containment model for holdings — `Broker` (e.g. Trading212, XPI) → `Portfolio`s → `Asset`s.

**Entities:** `Investments` (aggregate root; independent `ActiveBrokers`/`HistoricBrokers` collections), `Broker` (Name, Currency, Portfolios), `Portfolio` (Name, Assets; constructor `internal` — only `Broker` creates one), `Asset` (Name, ISIN, Exchange, Ticker, Country, LocalTypeCode, Class, owned Transactions/Credits/PriceHistory).

**Value objects:** `CountryCode`, `GlobalAssetClass`; `AssetPriceSnapshot` (Date, Price, IsManual).

**Relationships:** strict `Investments 1—* Broker 1—* Portfolio 1—* Asset`.

**Rules:**
- Asset classification derives from `(CountryCode, LocalTypeCode)` — **CONFIRMED** (`context.md`; test `AssetTests.Create_FiveArgOverload_ResolvesAssetClassFromCountryAndLocalTypeCode`).
- Active/Historic are two independent top-level collections; an asset never appears in both — **CONFIRMED** (P10 §4).
- Position state (`Long`/`Flat`/`Short`) is derived purely from `Quantity` sign, replacing a prior binary `Active` flag — **CONFIRMED** (P10 §2).
- `Broker.AddPortfolio` is get-or-create by name — **OBSERVED**.
- Only manually-entered (`IsManual=true`) price snapshots can be removed — **CONFIRMED (business decision)**.

**Application services:** `NavigationService`/`Mapper`, `PortfolioAssetSummaryService`/`Builder`, `BrokerBreakdownService`/`Builder`, `AssetMutationHelper`, `AssetCashFlowBuilder`.

**API:** `AssetsController` (`/assets`), `NavigationController`, `SummaryController`.

**WPF:** `MainNavigationViewModelBase` → `MainNavigationViewModel`/`MainNavigationViewModelHistoric` (parallel Active/Historic ViewModels), `AssetDetailsViewModel`.

**Web:** `InvestmentTree.tsx`, `ActiveInvestmentsPage.tsx`, `HistoricInvestmentsPage.tsx`, `SelectedNodeContext.tsx`.

**Tests:** `BrokerTests`, `PortfolioTests`, `AssetTests`, `InvestmentsTests` (Domain); `NavigationServiceTests`, `PortfolioAssetSummaryServiceTests` (Application).

---

## Transaction Recording (Buy/Sell)

**Purpose:** Records buy/sell events; derives running quantity, average cost, realized gain.

**Entities:** `Transaction` (Id, Date, Type, Quantity, UnitPrice, Fees; `TotalPrice` computed; factories `Create`/`CreateWithId`/`CreateFromTotal`), `Transactions` (owned collection maintaining running `Quantity`/`AveragePrice`/`RealizedCapitalGain`/`AverageSellPrice`).

**Rules:**
- **Weighted-average cost method** for `AveragePrice` on every Buy — **CONFIRMED** (explicit test intent, `TransactionsTests.Add_Buy_UpdatesAveragePriceAndQuantity`).
- Selling does not change `AveragePrice` — **CONFIRMED**.
- `RealizedCapitalGain` accumulates `TotalPrice − (Quantity × AveragePrice-at-sale)` per sell — **CONFIRMED**.
- `AverageSellPrice` = weighted average of sell totals, `null` with no sales — **CONFIRMED**.
- Short-selling (negative `Quantity`) is allowed with no guard, an intentional supported state (P10 frames Short as a recognized position type) — **CONFIRMED, accepted as-is for now** (product-owner decision, no near-term change planned).
- `Update`/`RemoveById` fully rebuild derived state by replaying all remaining transactions **in list insertion order, not date order** — **CONFIRMED purpose: this exists specifically so `AveragePrice` can be recalculated.** Order-of-insertion replay is a deliberate implementation choice, not accidental.
- No negative-value guards on Quantity/UnitPrice/Fees at Domain/Application layer — **OBSERVED**; client-side (WPF/Web form) validation not verified.

**Application services:** `TransactionService` (`ITransactionService`/`ITransactionQueryService`), routed through `AssetMutationHelper`.

**API:** `TransactionsController` (`/transactions`).

**WPF:** `TransactionDialog.xaml`, `TransactionDialogViewModel`.

**Web:** `TransactionsTab.tsx`, `useTransactions.ts`.

**Tests:** `TransactionTests`, `TransactionsTests` (Domain, thorough); `TransactionServiceMutationTests`, `TransactionServiceQueryTests` (Application).

---

## Credits (Dividends & Rent — manual entry)

**Purpose:** Records non-trade income (dividends, rent) against an Asset.

**Entity:** `Credit` (Id, Date, Type [`Dividend`/`Rent`], Value) — owned flat list on `Asset`.

**Relationship:** `Asset.RealizedGainLoss = Transactions.RealizedCapitalGain + Credits.Sum(Value)` — deliberate composition (explicit code comment: Transactions has no knowledge of Credits).

**Rules:** `CreditType` limited to Dividend/Rent/JCP — **OBSERVED**. Update/Delete require a non-empty Guid — **OBSERVED**.

**Application services:** `CreditService` (via `AssetMutationHelper`), `CreditTypeParser`.

**API:** `CreditsController` (`/credits`) — full CRUD + by-broker/by-portfolio.

**WPF:** `CreditDialog.xaml`, plus extensive charting support (`CreditsChartBuilder`, `CreditsViewState`, `CreditsFilterOptionViewModel`, `CreditsMonthTypeTotals`).

**Web:** `CreditsTab.tsx`, `useCredits.ts`.

**Tests:** `CreditTests`, `CreditServiceTests`, `CreditTypeParserTests`.

---

## Asset Price Fetching & Fallback Strategy

**Purpose:** Resolve current market price per asset, dispatched by `GlobalAssetClass`. Full detail in [08-integrations.md](08-integrations.md).

**Dispatch — CONFIRMED** (matches P08/P09): Bond → `BondAssetPriceFetcher` → Status Invest by Name. Cryptocurrency → `CryptocurrencyAssetPriceFetcher` → `IFinanceService.GetAssetValue(brokerCurrency, Ticker)`. Everything else → `StandardAssetPriceFetcher` → `IFinanceService.GetAssetValue(Exchange, Ticker)`.

**Fallback — CONFIRMED:** Google Finance primary, Yahoo Finance fallback, exchange-based requests only. **Reason confirmed: Google has no cryptocurrency price data**, hence crypto is excluded from this chain entirely.

Preconditions (Name for Bond, BrokerName for Crypto, Exchange for Standard) are hard-thrown `ArgumentException`s — **OBSERVED**.

**API:** `AssetPricesController` (`/prices`), `AssetPriceFetchController` (`/asset-price-fetch`).

**WPF:** `AssetPriceView.xaml`, `PriceDialogViewModel`, `AssetPriceFetchViewModel`.

**Web:** `PriceHistoryTab.tsx`, `usePriceHistory.ts`.

**Tests:** `AssetPriceServiceTests`, `BondAssetPriceFetcherTests`, `CryptocurrencyAssetPriceFetcherTests`, `FallbackFinanceServiceTests`, `StandardAssetPriceFetcherTests`, `GoogleFinanceServiceTests`, `YahooFinanceServiceTests`, `StatusInvestFinanceServiceTests`, plus scraping/retry-policy tests (some may be live-verification tests against real external sites — **UNKNOWN** whether CI-run or manual-only).

---

## Dividend Tracking & Valuation

**Purpose:** Track historical dividend/rent payments per ticker; compute a "fair buy price" from trailing yield.

**Value object:** `DividendValue` (Date, Value, DividendType).

**Rules:**
- 5-year lookback, current partial year excluded (not prorated) — **CONFIRMED** (P01: "Average Dividend: last 5 years").
- `RequiredYield = 6%`, the denominator of `PriceMaxBuy = averageDividend / RequiredYield` — **CONFIRMED (business decision)**: a rule the product owner learned and applies deliberately, not a placeholder.
- Divide-by-zero guarded, returns 0 — **OBSERVED**.

**Known defect:** `DividendDataSourceAdapter` accepts an `(exchange, ticker)` signature but only forwards `ticker` to the underlying lookup. **Confirmed error** — the method should not accept an `exchange` parameter at all. Not fixed in this documentation pass.

**Application services:** `DividendService`, composing `IDividendDataSource` + `IAssetSnapshotSource` + `DividendValuationRules`.

**API:** `DividendsController` (`/dividends`).

**WPF:** `DividendCheckView.xaml`, `DividendCheckViewModel`.

**Web:** `DividendCheckPage.tsx`.

**Tests:** `DividendServiceTests`, `DividendValuationRulesTests`.

---

## Performance Analytics (XIRR, Profit)

**Purpose:** Money-weighted return and simple cost-basis profit.

**Rules (standard financial formulas — OBSERVED, not project-specific policy):**
- `XirrCalculator`: Newton-Raphson, 100 max iterations, `1e-7` tolerance, 10% initial guess, 365-day year; returns `null` with fewer than 2 cash flows or if the derivative hits zero (no bisection fallback).
- `ProfitCalculator.HasCostBasis` requires `averagePrice > 0 && quantity > 0`; `CalculateResultFraction` returns `0` with no cost basis, `CalculateProfitPercent` returns `null` for zero cost basis — inconsistent null-vs-zero convention within the same class. **Confirmed as an accepted implementation decision**, not a defect.

**Known duplication — confirmed live, not dead code:** `Financial.Web/src/utils/xirr.ts` independently reimplements XIRR client-side. It is actively imported by `PortfolioSummaryTab.tsx`, computing per-table-row XIRR from cash-flow data already present in the API response (avoids a per-row `/xirr` round-trip). This is a genuine second implementation of the same algorithm as the backend `XirrCalculator`; if their edge-case handling (e.g. the `<2 cash flows` guard) diverges, the UI and API could disagree. No action taken; flagged for whoever next touches XIRR logic on either side.

**Application services:** `XirrCalculationService`, `ProfitCalculationService`, `AssetCashFlowBuilder`.

**API:** `XirrController` (`/xirr`).

**Tests:** `XirrCalculatorTests`, `ProfitCalculatorTests`, `XirrCalculationServiceTests`.

---

## Portfolio/Broker/Asset Summary & Credits-Adjusted Return

**Purpose:** Aggregate value/cost basis and dividend/rent-adjusted profit at every hierarchy level; broker composition breakdown.

**Rule — CONFIRMED** (P02): `TotalCredits` = sum of all Dividend+Rent Credit values on an asset. The "% Profit w/ Credits" metric exists specifically because plain "% Profit" was identified as understating return for income-producing assets.

**Rule — CONFIRMED (business decision):** `CreditFrequencyAnalyzer.DetectFrequencyPerYear` infers cadence (Monthly/Quarterly/Four-monthly/irregular) from average month-gap between recorded credits, using thresholds ≤1.5/≤3.5/≤5.0 months.

**Application services:** `SummaryService`, `PortfolioAssetSummaryService`/`Builder`, `BrokerBreakdownService`/`Builder`, `CreditService`.

**API:** `SummaryController`, `CreditsController`.

**WPF/Web:** as listed under Credits above, plus `AssetSummaryTab.tsx`, `PortfolioSummaryTab.tsx`, `BrokerBreakdownCharts.tsx`.

**Tests:** `CreditServiceTests`, `PortfolioAssetSummaryServiceTests`, `SummaryServiceTests`, `CreditFrequencyAnalyzerTests`.

---

## Cryptocurrency Handling

**Purpose:** First-class `GlobalAssetClass` for crypto, priced by broker-currency rather than exchange/ticker.

**Rule — CONFIRMED** (`context.md`): crypto is held in a dedicated broker (Coinbase); no `LocalTypeCode` mapping row exists for it — the class is set directly on the asset rather than derived from the mapping table. See "Asset Price Fetching" above for why Google/Yahoo fallback doesn't apply to crypto.

**Tests:** `CryptocurrencyAssetPriceFetcherTests`, `GoogleFinanceCryptocurrencyUrlTests`.

---

## Country/Local-Type → GlobalAssetClass Mapping

**Purpose:** Normalize per-country local asset-type strings into one shared taxonomy for cross-jurisdiction reporting.

**Rule — CONFIRMED** (matches `context.md`'s documented examples one-for-one): e.g. BR Acoes→Equity, BR FII→RealEstate, BR TesouroDireto→Bond, UK ConventionalGilt→Bond, BR CreditoImobiliario→PrivateCredit.

Unmapped/blank `LocalTypeCode` resolves to `Unknown` (fallback, not exception), case-insensitive match — **OBSERVED**.

## Naming note (see also [10-domain-cashflow.md](10-domain-cashflow.md))

"Investment Account"/"Investment Snapshot" in the API and UI does **not** refer to this bounded context — those routes and pages belong to CashFlow's separate "quick-access investments" concept (e.g. emergency funds), explicitly distinct from the long-term holdings modeled here.
