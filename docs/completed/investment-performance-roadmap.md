# Investment Performance & Tax Reporting — Feasibility and Roadmap

Program-level assessment of the Investment bounded context against the *Investment Performance
Research Brief*. Scope is **Investment only** — CashFlow is out of scope throughout.

Status: assessment. Written 2026-09-08 against `main` @ `94757937`.

**Revised 2026-09-10 against `main` @ `24bbf41d`.** Wave 0 has since been specified in full
(`specs/003-investment-calculation-core/`), and doing so meant checking this document's claims against
the code and against `data/data-investment.json` rather than against memory. Twenty were wrong. The
corrections are woven in below and marked **[corrected 2026-09-10]**; two new gaps (G13, G14) and one
new decision (D7) were found in the process. Nothing in the verdict changes — the programme is still
feasible and still ordered the same way — but three of §6's features and one of §7's decisions
described work that cannot be done as written.

One methodological warning carried out of that exercise: **several figures below are volatile.** Price
counts and staleness ages change hourly because the application is running and fetching. During the
day this revision was written the data file gained five prices. Treat every count here as
illustration, never as something to build a rule on.

**Revised 2026-09-13.** Waves 0, 1 and 2 have since shipped in full — `specs/003-investment-calculation-core/`
(P46, 12 PRs, merged 2026-09-11), `specs/004-transaction-income-vocabulary/` (P47, 2 PRs, merged
2026-09-12) and `specs/005-valuation-methods-provenance/` (P48, 5 PRs, merged 2026-09-12). §6 below
records what each wave actually delivered against its plan; several gaps in §3 that motivated Wave 0
and Wave 1 are now closed or narrowed and are marked inline.

**Revised 2026-09-14.** Wave 3 has since shipped in full —
`docs/prd/P49-prd-multi-currency-reporting-currency/` (P49, 16 PRs across F01–F05, merged 2026-09-13),
plus two same-day follow-ups outside the original feature table: a performance fix (#822, in-memory FX
rate cache + a reporting-currency on/off toggle, prompted by live use surfacing slow navigation on
brokers whose currency differs from the reporting currency) and a gap-closing cross-feature test (#823).
G2 is closed; D3 is resolved. Wave 4 (P50 · Disposals and cost basis) is next.

**Revised 2026-09-15 (Wave 4).** Wave 4 has since shipped in full —
`docs/prd/P50-prd-disposals-and-cost-basis/` (P50, 15 PRs across F01–F05, merged 2026-09-14/15). G3's
remaining FIFO/specific-identification limitation and G4 (no disposal record) are both closed; D2 is
confirmed. D7 (custody vs. domicile, previously deferred to P51) was also resolved during this
revision — see §7 — closing G13. Wave 5 (P51 · Tax reporting support) is next.

**Revised 2026-09-15 (Wave 5).** Wave 5 has since shipped in full —
`docs/prd/P51-prd-tax-reporting-support/` (P51, 12 PRs across F01–F05, merged 2026-09-15). G5 (no tax
domain at all) is closed; D1 (reporting support, never tax-due calculation) is confirmed as
implemented. Wave 6 (P52 · Portfolio dashboard and data quality) is next.

**Revised 2026-09-17 (Wave 6).** Wave 6 has since shipped in full —
`docs/prd/P52-prd-portfolio-dashboard-and-data-quality/` (P52, 12 PRs across F01–F06, merged
2026-09-15/17). G7 is closed — income YTD, allocation by class/currency/country/broker, gross-vs-net
portfolio return and data-quality warnings are all now on the dashboard, in both front ends. G11 is
further narrowed: the report-only unclassified-holding tool P46-F07 shipped now has a visible home,
surfaced as dashboard data-quality warnings with click-through to the affected holding. Wave 7 (P53 ·
Corporate actions) is next.

---

## 1. Verdict

**Feasible, with no architectural change required.** The brief's central calculation model —
every investment as a series of dated cash flows, solved for XIRR — is already the model this
codebase uses. The solver, the active/closed split, per-asset price history, and the
provider-fetcher abstraction all exist and are tested.

What the brief asks for beyond that is overwhelmingly **data-model widening**, not new
architecture. Roughly 70% of the work is widening three entities (`Transaction`, `Credit`,
`AssetPriceSnapshot`) and re-deriving everything downstream from them.

Three caveats bound the estimate:

1. **This is a migration programme, not a feature programme.** Every widening is a JSON schema
   change → OpenAPI snapshot → generated TS types → React → WPF. The repo's own Definition of
   Done (vertical slices, `main` always deployable) means each widening is its own PR with
   backward-compatible deserialisation and its own migration verification against a temp copy of
   `data-investment.json`.
2. **Two front ends roughly double presentation cost.** React is the UX source of truth; WPF
   follows at parity. Expect ~40% of total effort in UI.
3. **The tax module must be scoped as *tax reporting support*, not *tax calculation*.** See §5.

Data volume makes migration cheap: 160 assets, 899 transactions, 1,485 credits and — at the moment
of writing — 62 price snapshots. Every migration in this programme runs in under a second and is
trivially diffable.

---

## 2. What already exists (and is worth keeping)

| Brief requirement | Existing implementation | Assessment |
|---|---|---|
| Dated cash flows as the universal model | `AssetCashFlowBuilder` (buy/sell/credits → dated flows) | Correct shape, narrow vocabulary |
| XIRR | `Domain/Rules/XirrCalculator` — bracket + bisection over (-1, ∞), null rather than throw | **Production-grade. Do not touch.** |
| Closed holdings use historical facts | Active/Historic broker trees; historic terminal value is 0, never marked to market | **[corrected 2026-09-10]** A convention, *not* an enforced invariant — see G14 |
| Valuation snapshots | `AssetPriceSnapshot(Date, Price, IsManual)` + per-asset `PriceHistory`, write-failure visibility | ~40% of the brief's snapshot entity. **[corrected 2026-09-10]** "Manual precedence" overstates it — see G10 |
| Multiple valuation sources | `IAssetPriceFetcher` → Standard / Bond / Cryptocurrency; `FallbackFinanceService` chains Google → Yahoo → StatusInvest | Extensible to NAV / provider value |
| Instrument classification | `CountryCode` x `LocalTypeCode` → `GlobalAssetClass` (11 classes incl. RealEstate, Bond, Fund, Pension, PrivateCredit, Cryptocurrency) | Covers every class in the brief |
| Income separate from price movement | `Credit` (Dividend / Securities Lending Income / JCP / Coupon) is a distinct dated entity | **[corrected 2026-09-12]** Granularity fixed by P47 — see G12 |
| Income analytics | `CreditFrequencyAnalyzer`, estimated annual yield, last-month yield | Yield is on **cost**, not market value |
| Delivery machinery | 45 PRDs, 769 PRs, PRD → spec → feature workflow, OpenAPI snapshot contract tests, generated TS types, coverage gates, affected-only CI | Proven at exactly this scale |

The prior accuracy work (**P34**, all but two acceptance criteria green) already fixed XIRR solver
convergence, the credits double-count, the duplicate TypeScript XIRR implementation, price-history
write-failure visibility, manual-price precedence, and asset-class fetcher routing. The
foundations are sound; this programme builds on them rather than around them.

---

## 3. Gaps against the brief

Ordered by dependency — G1 is the root, most others are downstream of it.

### G1 — Transaction vocabulary is two values **[substantially addressed 2026-09-12]**

`Transaction.TransactionType` is `{ Buy, Sell }`. The brief specifies seventeen. Fees exist only
as a field on a buy/sell; tax withheld, return of capital, capital calls, transfers, redemption,
maturity, splits, and standalone valuation adjustments are **unrepresentable**. `Credit` carries
three income types and a single positive `Value` — no gross, no withheld, no net, no
return-of-capital. This is the root gap: net-of-tax return, tax reporting, and most instrument
rules are all blocked behind it.

P47 (#794/#795) widened `TransactionType` to eight values (adds `Fee`, `Redemption`, `TransferIn`,
`TransferOut`, `CapitalCall`, `ReturnOfCapital`) driven by a data-driven `TransactionTypeEffects`
table, added a gross/fees/withheld/net money block to both `Transaction` (`NetCash`) and `Credit`
(`NetAmount`), and produced a `TotalReturnNetOfTax` series alongside the existing gross one. Splits
and standalone valuation adjustments remain out of scope by design (Wave 7/Wave 2 respectively);
tax reporting itself is still Wave 5.

### G2 — No currency on transactions, no FX **[fixed 2026-09-13]**

Currency lives on `Broker` only (`Trading 212` / `Coinbase` / `FreeTrade` = GBP, `XPI` = BRL).
There is no per-transaction currency, no reporting currency, no dated FX rate, and therefore no
honest all-brokers total. An `IExchangeRateProvider` (Frankfurter) exists but sits in **CashFlow**
Infrastructure — bounded-context isolation forbids Investment referencing it, so it must be
promoted to a shared abstraction rather than reused in place.

P49 closed this in full: F01 promoted `IExchangeRateProvider` to `Financial.Shared.Abstractions`
with its Frankfurter implementation in its own `Integrations/Frankfurter` project, cut over
`ControleMaeService` (CashFlow) to the shared interface, and added the 10-calendar-day
nearest-earlier-date fallback for weekends/holidays. F02 added `Currency` (auto-filled from the
broker) and a dated `FxRateSnapshot` to `Transaction`/`Credit`, backfilled automatically on load
for pre-existing records. F03 added the persisted reporting-currency setting (defaults GBP) and
converted totals — each flow-based figure is the sum of its contributing records converted
individually at each one's own date, not the native total converted by a single spot rate — with
`Partial`/`ReportingCurrencyUnavailable` flags and native figures always preserved. F04/F05 gave
both front ends the Settings control, the converted totals alongside native ones, and a per-record
FX provenance affordance (rate, source, retrieved-at). A same-day follow-up (#822) added an
in-memory (session-lifetime, unpersisted) rate cache and a reporting-currency on/off toggle after
live use surfaced slow navigation on brokers whose currency differs from the reporting currency —
every unique transaction/credit date was re-fetched from Frankfurter on every click with no
caching; the toggle lets a user skip the conversion path entirely rather than pay that cost.

### G3 — Cost basis is hard-coded and order-dependent **[fixed 2026-09-11]**

`Transactions` computes weighted-average cost inline, and replays **in insertion order, not date
order** (`Transactions.Rebuild` walks `_items`; `TransactionService.AddTransactionAsync` appends).
Two consequences:

- *Defect:* entering a back-dated buy after a sell yields a different average price than entering
  the same set chronologically. Realised gain/loss inherits the error.
  **[corrected 2026-09-10]** Measured blast radius on live data: exactly **one** holding is stored out
  of date order, and re-sorting it by date changes nothing, because both swapped rows are purchases.
  Fixing date order alone is therefore pure prevention. What *does* move two holdings is the same-date
  tie-break the fix has to define — see G12's new sub-point.
- *Limitation:* FIFO and specific-identification are impossible; there is no per-jurisdiction
  method selection.

Fees are also folded into `AveragePrice` via `Transaction.TotalPrice`, which conflates acquisition
cost with allowable costs. Defensible for economic return; not separable for a tax basis.

**[fixed 2026-09-11]** P46-F01 (#781, User Story 1) made date-ordered replay the single owner of
position figures, with the same-date purchases-before-sales tie-break described under G12 below.
The FIFO/specific-identification limitation is unchanged — still Wave 4 (P50) — and fees are still
folded into `AveragePrice`, unchanged until Wave 1's money-block work (G1) separates them.

**[FIFO/specific-identification delivered 2026-09-15]** P50-F01 (#825) added a `CostBasisMethod`
strategy (AverageCost / FIFO / SpecificId) selectable per broker; P50-F02 (#826/#828) persists the
method, basis and units actually used on each disposal. Fees remain folded into `AveragePrice`,
still unchanged until G1's money-block work reaches this figure.

### G4 — No disposal record **[fixed 2026-09-15]**

Realised gain is a running scalar (`Transactions.RealizedCapitalGain`) recomputed by full replay.
Nothing persists *which* units were disposed, under which method, at what basis, in which tax
year. The brief requires that a later configuration change must not retroactively rewrite a filed
result — today, changing anything replays everything.

**[fixed 2026-09-15]** P50-F02 (#826/#828) added a persisted, immutable `DisposalRecord` per
sell/redemption — date, units, method, basis, proceeds, fees, gain/loss, tax year. P50-F03 (#829)
defined the recalculation policy the brief required: a later method change or backdated correction
supersedes an existing record rather than rewriting it, so a full audit chain survives.

### G5 — No tax domain at all **[closed 2026-09-15]**

Zero fields anywhere: no jurisdiction, tax year, event classification, withheld amount, exempt
amount, evidence reference, or calculation status. No net-of-tax return of any kind.

**[closed 2026-09-15]** P51-F01/F02 added a dated, admin-configurable `TaxRule` and a `TaxClassification`
created automatically for every `DisposalRecord` (P50) and every qualifying income credit (Dividend,
Coupon, JCP, Securities Lending Income — P47), each carrying jurisdiction, tax year, event category and
a four-value calculation status (final / estimated / incomplete / requires review). P51-F03 assembled
the per-jurisdiction, per-tax-year workbook with evidence references back to the source record. Net-of-tax
return was already closed separately by P47 (G1, `TotalReturnNetOfTax`). Tax due itself is still never
computed, per D1/§5.

### G6 — Valuation maths lives in the front ends **[fixed 2026-09-11]**

Market value, cost basis, unrealised gain/loss and result % are computed in
`Financial.Web/src/hooks/useAssetSummary.ts` **and** in
`Financial.App/ViewModels/Investment/PortfolioAssetSummaryRowViewModel` — neither in Domain.
P34-F03 already removed a duplicate TypeScript XIRR for exactly this reason; the rest of the
calculation set needs the same treatment or React and WPF will drift.

**[corrected 2026-09-10]** "Two implementations" undercounts by a factor of five. Market value alone is
derived in **five** React sites (`useAssetSummary.ts:228` and `:256`, `usePortfolioAssetSummary.ts:151`,
`PortfolioSummaryTab.tsx:36` and `:176`) and **two** WPF sites (`AssetDetailsViewModel.cs:141`,
`PortfolioAssetSummaryRowViewModel.cs:173`), with **three** further sites deriving cost basis — ten in
total. They already disagree on units: `useAssetSummary.ts` returns fractions, `PortfolioSummaryTab.tsx`
returns percentages.

**[fixed 2026-09-11]** P46-F02 (#785, User Story 4) added the single `HoldingValuation` domain rule
(market value, cost basis of open units, unrealised P/L, price-only return, total return); P46-F04/F05
(#786) deleted all ten sites above and switched both `Financial.Web` and `Financial.App` to consume it
— the WPF app resolves the Application service in-process via DI, not over HTTP, per the correction
under Wave 0 below.

### G7 — No portfolio-level return, no dashboard **[fixed 2026-09-17]**

XIRR is per-asset only. `AggregatedSummaryDTO` is four numbers (bought / sold / credits /
invested): no market value, no unrealised total, no income YTD, no allocation by currency, country
or provider, no gross-vs-net portfolio return, no data-quality warnings.

P46-F06 (#787/#788, User Story 5) added `MarketValue`, `HoldingCount`, `UnvaluedHoldingCount`,
`PriceOnlyReturn` and `TotalReturn` to `AggregatedSummaryDTO` at both portfolio and broker level, in
both front ends — the first aggregate return figures the brief asked for.

P52 (`docs/prd/P52-prd-portfolio-dashboard-and-data-quality/`, #853–#864) closed the rest: a single
dashboard aggregate (market value, invested, unrealised, realised, income YTD and lifetime, gross and
net XIRR — F01), allocation by class/currency/country/broker (F02), data-quality warnings with
click-through to the affected holding (F03) and upcoming income/coupon/maturity projections (F04), in
both React (F05) and WPF (F06). See Wave 6 under §6 for the full breakdown.

### G8 — Valuation method is implicit **[fixed 2026-09-12]**

Every asset is priced by ticker lookup. There is no `valuationMethod`
(market price / NAV / provider value / manual / bond quote), so **value-based funds and
property-platform investments (Inco) have no honest representation**. `Transaction` also rejects
`quantity <= 0` and `unitPrice <= 0`, so a plain "I contributed £5,000" cannot be recorded without
inventing units.

P48 (`specs/005-valuation-methods-provenance/`, #797–#801) added `Asset.ValuationMethod`
(market price / NAV / provider value / manual / bond quote) and `IncomePolicy`, drove fetcher
routing from method rather than asset class alone, and added a provider-valued holding path that
records a value directly with no quantity change — closing the gap for the two instrument types
named in §4 (value-based funds, Inco). See Wave 2 under §6 for the full breakdown.

### G9 — No corporate actions

Splits, consolidations, rights issues, mergers and spin-offs would silently corrupt quantity and
average price.

### G10 — Provenance is thin **[asset-price half fixed 2026-09-12]**

`AssetPriceSnapshot` has Date / Price / IsManual — no source, source reference, market status,
currency, or retrieved-at. `Transaction` has no source, source reference, estimated flag, or
created/updated timestamps. The brief requires all of these for reproducibility and for detecting
provider corrections.

P48 (#797/#798/#799) widened `AssetPriceSnapshot` with currency, source, source reference, market
status and retrieved-at, made `IsManual` derived from source, and made both front ends show
as-of/source/market-status plus a stale-data warning. The `Transaction` half of this gap (source,
source reference, estimated flag, created/updated timestamps) is untouched — still open, folded
into Wave 1's/Wave 3's later work rather than a wave of its own.

**[corrected 2026-09-10]** Three further facts about price history that the brief's snapshot work has
to accommodate, none of them in the original assessment:
- **A holding stores at most one snapshot per date.** `Asset.UpsertInto` replaces any entry with a
  matching date, so a manual and an automatic price for one date can never coexist. There is
  consequently *no read-time manual-precedence rule* — `GetMostRecentPrice` orders by date only. What
  exists is a write-time guard that protects a manual entry dated **today** from being overwritten by
  a fetch; on any earlier date the last write wins, and P32-F03's own spec documents a live fetch
  overwriting a stale manual entry as intended.
- **`GetPriceForDate` matches an exact date only** — there is no carry-forward. Since prices are only
  recorded on days a fetch runs, most holdings have no price bearing today's date, so any valuation
  built on exact matching would value almost nothing.
- **`Asset.PositionType` is persisted** despite being derived purely from quantity, and is written to
  every holding in the file. Four other derived figures are correctly excluded from serialization;
  this one is not, so it can and does disagree with the transactions beneath it.

### G11 — Data quality in the live file

**[corrected 2026-09-10]** The counts and, more importantly, the remedy.

**90 of 160** assets are `GlobalAssetClass.Unknown` — 87 historic *and 3 active*, which the original
count missed. 93 have an empty `LocalTypeCode`; the difference of three is Bitcoin, BOVA11 and IVVB11,
whose class is set directly on the asset instead of being derived, which is legitimate and documented
for Bitcoin (no crypto row exists in the mapping).

Two findings change what can be done about it:

- **The mapping table has no gaps.** Every asset carrying a `LocalTypeCode` resolves to a real class —
  zero failures. So nothing is unclassified because a mapping row is missing; they are unclassified
  because the code itself is absent. `CountryCode` is **not** a gap either: all 160 assets carry one.
- **There is no mechanical backfill.** Confirmed with the user: classifying an instrument is a
  judgement made one at a time, so the code cannot be inferred. This kills the "backfill tool" in
  §6 Wave 0 F07 and invalidates decision D5 — see both.

Any class-driven rule will therefore be wrong for those rows **indefinitely**, not "until backfilled",
and must define its own behaviour for an unclassified holding. Note that classification also decides
which fetcher a price comes from: `Unknown` is routed as exchange-listed, which is right for a share,
fund or ETF and **wrong for a bond or cryptocurrency**, so for those two an absent class silently
causes an absent price.

**[delivered 2026-09-11]** P46-F07 (#790/#791, User Story 7) shipped the report-only tool this section
anticipated: it names every `Unknown`/unclassified holding, G12's three impossible sales, and G14's
four mis-filed Historic holdings, and writes nothing back to the data file.

**[delivered 2026-09-17]** P52-F03 (#854, #859, #863) gave that report-only tool a visible home: the
same findings — unclassified holdings, missing price, missing basis, stale valuation, unknown tax
treatment, impossible cash-flow sequence — now surface as dashboard data-quality warnings, with
click-through navigation to the affected holding, in both React and WPF. Still nothing is written back
to the data file; this closes the "no visible home" half of the gap, not the underlying classification
work itself.

### G12 — Smaller correctness and labelling issues

- **Oversell is not rejected.** `Transactions.Add` never checks sell quantity against held
  quantity; a typo silently produces a negative quantity and a `PositionType.Short`.
  **[corrected 2026-09-10]** **Three holdings already breach this**, all Historic: two by under a
  hundredth of a unit from a fund redemption rounding, one (`ImpaxEnviro`) a lone sale of 1 unit with
  no purchase at all. Consequences for the fix: validating on the load path would stop the application
  starting, and a rule raised from inside the position replay would make those three impossible to
  *correct*, since replay runs on every edit. The latent `DivideByZeroException` is also worse than it
  looks — positions rebuild while the file is being read and nothing on that path catches it, so it
  would fail **startup**, not one holding.
  **[fixed 2026-09-11]** P46-F03 (#782, User Story 2) rejects a new sale that would take a holding
  negative, without touching the three pre-existing holdings already in that state on load — the
  load-path and replay-time concerns above are why the rule is enforced only at the point of a new
  sell.
- **The same-date tie-break is undefined, and defining it moves real figures.** **[new 2026-09-10]**
  A transaction carries a date but no time, so any date-ordered replay must decide how to order
  same-date rows. Two holdings store a sale ahead of a purchase on one date; ordering purchases first
  (the rule chosen) changes Bitcoin's average price from 62,713.15 to 62,709.05 and its realised gain
  from 0.25 to 0.17, and AGNC's realised gain from 9.98 to 9.66. Small, one-off and defensible as a
  correction — but it must be stated, not discovered.
  **[fixed 2026-09-11]** Purchases-before-sales on a tied date shipped as part of P46-F01 (#781); the
  two figure changes above are the actual, intended effect of that PR.
- **`TotalInvested = TotalBought - TotalSold`** (`AssetInvestedAmountSelector`) understates the
  cost basis of a partially-sold position. Buy 100 @ £10, sell 50 @ £20 → reported invested = £0
  while 50 units are still held at £500 cost. **[corrected 2026-09-10]** There are **five** independent
  derivations of this figure, not the two the selector suggests: the rows, the portfolio/broker total
  (`SummaryService` hardcodes the subtraction and never consults the selector), the allocation chart
  (which additionally filters `> 0`), and a footer in each front end that sums the rows. Both front
  ends therefore already print two different "total invested" figures on the same screen. All 15
  Historic portfolios disagree today; the 10 Active ones reconcile only because no active holding
  currently has quantity zero — the code paths diverge, the data just has not exposed it yet.
  **[fixed 2026-09-11]** P46-F03 (#783, User Story 3) gave the five derivations one meaning: invested
  is now cost basis of currently-held units, computed once and consumed everywhere, so a partially-sold
  position no longer reports £0 invested.
- **The invested figure cannot be redefined in isolation.** **[new 2026-09-10]** The same number
  serves three purposes at once: the invested amount, the basis for portfolio weight, and the
  denominator of the income yield percentages. Changing it for one purpose silently moves the other
  two — so market-based weight cannot be introduced without accidentally converting yield-on-cost into
  yield-on-market. The three must be separated before either change ships.
  **[fixed 2026-09-11]** The three were separated as planned: #783 fixed invested-amount; #784 made
  `PortfolioWeight` nullable as a compatibility boundary ahead of #789 repointing its basis to market
  value; yield percentages were deliberately left yield-on-cost (see below), now on a figure that no
  longer also carries the other two concerns.
- **`PortfolioWeight` is cost-based, not market-based** (`PortfolioAssetSummaryBuilder`
  `weightBasis`). The brief's allocation views require market value.
  **[fixed 2026-09-11]** P46-F03 (#789, User Story 6) repointed `AssetAmountBases.WeightBasis` in
  Active Investments to each holding's market value, with a disclosed shortfall for holdings with no
  price; Historic stays cost-based since historic terminal value is 0 (G14).
- **Yield percentages are yield-on-cost**, not market yield. Legitimate, but currently unlabelled.
  Still open — unchanged by Wave 0.
- **`Credit.Type.Rent`** is a naming leak for FII distributions. **[disproven & fixed 2026-09-12]**
  Checked against `data/data-investment.json` during P47's clarification session: every `Rent`
  credit sits on non-RealEstate holdings (BBAS3, BOVA11, GOLD11, IVVB11), while every RealEstate
  holding already uses `Dividend` — this is Brazilian securities-lending income (share loan fees),
  not a mislabeled FII distribution. Renamed to `SecuritiesLendingIncome`; `Coupon` (bond interest)
  added alongside it.
- **`Credit.Value > 0` only** — corrections and return-of-capital cannot be recorded.
  **[fixed 2026-09-12]** P47 relaxed this to "invalid only if `== 0`", so a negative `Value` records
  a correction to a previous payment; `Credit.Withheld`/`NetAmount` also added for the gross/net
  breakdown.
- **`CountryCode` is `{ Unknown, BR, US, UK }`** — an extensibility ceiling.

### G13 — `CountryCode` records custody, not domicile **[new 2026-09-10]** **[resolved 2026-09-15]**

Country is not a data-quality gap — all 160 assets carry one — but it does not mean what a tax module
would assume. It tracks the **broker's jurisdiction**: every holding at the Brazilian broker is BR and
every holding at the three UK brokers is UK save one. Between fifty and sixty recognisably
US-domiciled holdings — Apple, Tesla, PepsiCo, Johnson & Johnson, Procter & Gamble, Realty Income,
Walmart, IBM — are recorded as UK because they sit in UK accounts, and exactly one asset in the whole
file carries `US`. Two of them carry a US ISIN and a US exchange while still being recorded as UK.

The convention is also not enforced: AGNC Investment Corp. appears three times under one broker,
recorded as US once and UK twice.

This is harmless for classification, which resolves through `UK/Stock` as intended, and P48's
valuation routing is unaffected because the venue is what determines where a price is fetched. It is
**not** harmless for P51: withholding on a US-domiciled dividend is a US matter whichever account holds
it, so a tax profile derived from this field would be wrong for those fifty-odd holdings. See D7.

**[resolved 2026-09-15]** Confirmed with the user: tax is filed and paid in the broker's (custody)
jurisdiction regardless of where the underlying issuer is domiciled — a US-domiciled ETF held at a UK
broker is a UK filing matter, not a US one. D7 is therefore resolved as "custody only" — but not via
`CountryCode` itself, since this section already shows it's inconsistently set (AGNC recorded as US
once, UK twice, under the same broker). P51 instead derives jurisdiction from each disposal/income
event's own `Currency` (`BRL` → BR, any other currency → UK), the same signal P50-F02 already uses for
`DisposalRecord.TaxYear` — no new domicile field, no ISIN-prefix derivation, and `CountryCode` is not
consulted at all. This closes G13 as *not applicable* to tax-profile derivation, rather than fixed by
a data-model change.

### G14 — "Historic" is a filing convention, not an enforced state **[new 2026-09-10]**

§2 recorded "historic terminal value is 0, never marked to market" as an invariant already enforced. It
is a convention the application does not check. **Four of the 132 historic holdings still carry a
quantity** — one of them 28 units at just over 2,000 of cost, stored as `PositionType.Long` inside
Historic Investments; the other three carry the small negative quantities of G12's impossible sales.

Because Historic is never marked to market, a position that genuinely still holds units is reported as
worth nothing and contributes nothing to any total. The arithmetic is right for the scope it is filed
under; the filing is wrong. Every rule in the programme must therefore key off **which scope a holding
is filed under**, never off whether its quantity happens to be zero — otherwise the invested column
means two different things within one view.

---

## 4. Feasibility by instrument type

| Instrument | Representable today? | Blocking gap |
|---|---|---|
| Shares | Yes, minus corporate actions and withheld tax | G1, G9 |
| ETFs (distributing) | Yes | G1 (distribution policy not modelled) |
| ETFs (accumulating) | Partially — no cash flow to record | G8 |
| REITs / FIIs / RICs | Yes, income tracked; no per-payment tax classification | G1, G5 |
| Funds — unit/NAV based | Yes if a NAV can be fetched | G8 (NAV valuation method) |
| Funds — value based | **No** — units must be invented | G8 |
| Property platform (Inco) | **No** — no provider-value method, no unit-free contribution | G8 |
| Bonds — quoted | Yes; coupons recordable as credits | G1 (clean/dirty/accrued not separated) |
| Bonds — non-quoted | Partially — manual price only | G8, G10 |

Two instrument types the user already holds — value-based funds and Inco — are **not honestly
representable today**. That makes G8 the highest-value gap after G1.

---

## 5. Scoping recommendation: tax reporting support, not tax calculation

Computing tax due correctly means implementing, and maintaining annually:

- **UK:** Section 104 pooling, same-day and 30-day bed-and-breakfast matching, annual exempt
  amount, ISA wrapper exemption, dividend allowance and rate bands.
- **Brazil:** FII distribution exemption conditions, swing-trade vs day-trade rates, the monthly
  share-sale exemption threshold, DARF timing, and the dividend withholding change the brief
  cites as effective 2026.

That is a specialist product with its own release cadence, and getting it wrong in a personal
tool is worse than not having it. **Recommendation:** the system records classified, dated,
jurisdiction-tagged, auditable events and produces a per-tax-year workbook with a calculation
status (final / estimated / incomplete / requires review) — and explicitly does not compute tax
due. The brief itself already requires this posture ("must not be treated as tax advice"). This
single decision removes the largest schedule and correctness risk in the programme.

---

## 6. Roadmap

Eight waves, each one PRD, each feature one PR (max 8 non-test code files, per
`docs/rules/design.md`). Slice order within every feature stays the house standard: Domain →
Application → Infrastructure → API → React → WPF → tests. Next available PRD number is **P46**.

### Wave 0 — P46 · Investment calculation core **[delivered 2026-09-11]**

*Make what exists correct and server-owned before adding anything.* No schema change.

| # | Feature | Notes |
|---|---|---|
| F01 | Date-ordered position replay | Fixes the back-dated-transaction defect (G3). Domain-only. |
| F02 | `HoldingValuation` domain rule | One owner for market value, cost basis of open units, unrealised P/L, price-only return, total return (G6). |
| F03 | Reject oversell; correct `TotalInvested`; market-based `PortfolioWeight` | The G12 correctness set. |
| F04 | Server-computed asset valuation + **both** front ends switched over | **[corrected 2026-09-10]** Deletes the duplicated maths in `useAssetSummary.ts`, `PortfolioSummaryTab.tsx`, `usePortfolioAssetSummary.ts` *and* the WPF row/detail view models. |
| F05 | Portfolio-grid valuation + both front ends switched over | The second half of the same job — see the note below on why the two front ends move together. |
| F06 | Portfolio- and broker-level XIRR and market value | First aggregate return figures (G7 partial). |
| F07 | Data-quality **report** (no backfill) | **[corrected 2026-09-10]** There is no mechanical way to classify the 90 `Unknown` assets — it is manual, one instrument at a time. The tool reports and writes nothing; see D5. It also names G12's three impossible sales and G14's four mis-filed holdings. |

**Deliverable:** every figure on screen comes from one server-side implementation, and it is right.

**[corrected 2026-09-10] Two structural errors in this wave as originally written:**

1. **`Financial.App` is not an HTTP client.** F04/F05 described the desktop app switching to an
   endpoint. It has no `HttpClient` at all — it resolves the Application interfaces in-process via DI,
   exactly as it already does for `IProfitCalculationService` and `IXirrCalculationService`. The single
   owner is therefore a **Domain rule behind an Application service**, with an HTTP endpoint as an
   *additional* surface for the browser, not the mechanism by which the two front ends agree.
2. **The two front ends must switch in the same increment.** Switching one while the other still
   derives its own figures shows two different values for the same holding — a parity regression under
   the UI rules and the constitution, not an unfinished increment. This deviates from the documented
   Domain → Application → API → WPF → Web slice order, deliberately.

**PR count: ~13, not 7.** At the 8-non-test-file limit, F02+F04+F05 merge into one story needing four
PRs, and the invested-amount and weight work need two each. The wave is still one PRD and still
independently deployable; it is simply larger than the feature count suggests.

Shipped as `specs/003-investment-calculation-core` across 12 merged PRs, one per user story
(spec.md's 7 user stories, P1–P7) plus two bugfixes found along the way: #781 (US1 — date-ordered
replay, F01), #782 (US2 — reject oversell, part of F03), #783 (US3 — one meaning for `TotalInvested`,
part of F03), #784 (US6 part 1 — `PortfolioWeight` made nullable as a compatibility boundary, part of
F03), #785 (US4 — the `HoldingValuation` domain rule, F02), #786 (US4 — both front ends switched to
it, F04/F05), #787 (US5 — portfolio/broker `AggregatedSummaryDTO` fields, F06), #788 (US5 — both front
ends render the level totals, F06), #789 (US6 part 2 — `PortfolioWeight` repointed to market value
with shortfall disclosure, F03), #790 (US7 — the data-quality report, F07), #791 (fix — asset-admin
edit now re-derives `GlobalAssetClass` the same way create already does) and #792 (fix — a refused
transaction save no longer discards what was typed; found during the mandatory UI-review-checklist
pass, `docs/ui/review-checklist.md`).

### Wave 1 — P47 · Transaction and income event vocabulary **[delivered 2026-09-12]**

*The root gap. Everything downstream depends on it.*

| # | Feature |
|---|---|
| F01 | Widen `TransactionType` with an explicit position-effect and sign policy per type |
| F02 | Coherent money block on `Transaction`: gross / fees / tax withheld / net cash; relax unit validation for non-unit events |
| F03 | `Credit` → income event: gross, withheld, net, income kind; `Value` preserved as derived net for compatibility + migration tool |
| F04 | Cash-flow builder rebuilt over the new vocabulary → **Gross XIRR vs Net XIRR** as distinct series |
| F05 | React entry and display for the new event types |
| F06 | WPF parity |

**Deliverable:** net-of-tax return exists; every fee and withholding is an individually auditable
dated event.

Shipped as `specs/004-transaction-income-vocabulary` across two merged PRs, smaller than the
six-feature table implied because CI's `web` job (`openapiFreshness.test.ts`) enforces that generated
TypeScript types never drift from the OpenAPI snapshot — a backend-only PR cannot be green on its own
in this repo, so F01–F05 landed together: #794 (backend + `Financial.Web` — F01's 8-value
`TransactionType` behind a data-driven `TransactionTypeEffects` table, F02's gross/fees/withheld/net
money block, F03's widened `Credit` — including the `Rent`→`SecuritiesLendingIncome` rename and
negative-value corrections — F04's `TotalReturnNetOfTax` series, and F05's React parity; plus an
on-the-fly JSON-envelope migration replacing the one-shot console-tool pattern) and #795 (`Financial.App`
WPF parity, F06, completing User Story 4). The roadmap corrections this feature's clarification session
produced (the disproven `Credit.Type.Rent` naming-leak claim, the now-fixed `Credit.Value > 0`
limitation) are folded into §3 above rather than tracked separately.

### Wave 2 — P48 · Valuation methods and provenance **[delivered 2026-09-12]**

*Unblocks the two instrument types that cannot be represented today.*

| # | Feature |
|---|---|
| F01 | Widen `AssetPriceSnapshot`: currency, source, source reference, market status, retrieved-at (`IsManual` becomes derived from source) |
| F02 | `Asset.ValuationMethod` + `IncomePolicy`; drive fetcher routing from method, not class alone |
| F03 | Value-based holdings: contribution / withdrawal / provider valuation without inventing units — **funds without units, Inco** |
| F04 | React: as-of, source, market status, stale-data warning |
| F05 | WPF parity |

Shipped as `specs/005-valuation-methods-provenance` across five merged PRs: #797 (Foundational +
User Story 1 — F02's `ValuationMethod`/`IncomePolicy` plus F01/F03's widened snapshot and
value-based recording), #798 (User Story 2 — F01/F04/F05's provenance and market-status display in
both front ends), #799 (fix — automatic fetches now persist the real provider as `Source` instead
of `Unknown`), #800 (User Story 3 — F02's fetcher routing keyed off `ValuationMethod`), and #801
(polish/whole-feature verification — fixed a WPF/React terminology mismatch on the price-source label,
via a `PriceSourceToLabelConverter` mirroring `Financial.Web`'s `SOURCE_LABELS` map, plus the
quickstart.md walkthrough and full UI-review-checklist pass required to close the feature). Setting
`ValuationMethod`/`IncomePolicy` from either front end's Admin Asset form remains a follow-up — both
DTOs carry the fields, but only the API sets them today (`docs/investment-performance-roadmap.md`
does not track UI backlog items; see `specs/005-valuation-methods-provenance/tasks.md` T074's note).

### Wave 3 — P49 · Multi-currency and reporting currency **[delivered 2026-09-13]**

*Gates only the all-brokers consolidated view; per-broker views work without it.*

| # | Feature | Notes |
|---|---|---|
| F01 | Promote `IExchangeRateProvider` to `Financial.Shared.Abstractions`; implementation into its own `Integrations/` project | Also cut `Financial.CashFlow`'s `ControleMaeService` over to the shared interface — a separate copy no longer exists. |
| F02 | `Transaction.Currency` + dated FX rate captured and stored at record time | Auto-filled from the broker; pre-existing records backfilled automatically on the next load, no separate tool. |
| F03 | Reporting-currency setting; converted totals with original currency always preserved and shown | Each flow-based converted figure sums its contributing records converted individually at each one's own date. |
| F04 | React | Settings control, converted totals, per-record FX provenance affordance (rate/source/retrieved-at). |
| F05 | WPF parity | Same control, same totals, same provenance affordance — WPF composes the Investment Application layer in-process, no HTTP round trip. |

**Deliverable:** an honest all-brokers total exists; every converted figure states its provenance and degrades visibly (`Partial`/`ReportingCurrencyUnavailable`) rather than silently.

Unlike Waves 0–2, P49 used the `docs/prd/` spec-writer + implement-feature workflow rather than
`specs/00N-.../` — shipped as `docs/prd/P49-prd-multi-currency-reporting-currency/` across **16 PRs**
for F01–F05 (#803–#821, several features split into 2–4 stacked PRs each to stay within the
8-non-test-file limit), close to one PR per feature-stage rather than one per feature. Two same-day
follow-ups outside the original table: #822 (in-memory FX rate cache + a reporting-currency on/off
toggle, after live use on the merged branch surfaced slow broker navigation — every unique
transaction/credit date was re-fetched from Frankfurter on every click with no caching at all) and
#823 (a cross-feature acceptance test proving F01's shared rate provider gives F02's entry-time
snapshot and F03's on-demand conversion the identical rate for the same date, closing the last two
unchecked PRD boxes). 18 PRs total for the wave.

### Wave 4 — P50 · Disposals and cost basis **[delivered 2026-09-15]**

| # | Feature |
|---|---|
| F01 | `CostBasisMethod` strategy in Domain: AverageCost (default) / FIFO / SpecificId, selected per jurisdiction |
| F02 | Persisted immutable `DisposalRecord` per sell/redemption: date, units, method, basis, proceeds, fees, gain/loss, tax year |
| F03 | Recalculation policy — changing method never rewrites an existing record; recalculation from a date creates new ones |
| F04 | React disposal view |
| F05 | WPF parity |

**Deliverable:** every sell or redemption produces a persisted, immutable `DisposalRecord`;
AverageCost, FIFO or SpecificId can be selected per broker without ever rewriting a filed result — a
later method change or backdated correction supersedes, never overwrites, and a full audit chain
survives.

Like Wave 3, P50 used the `docs/prd/` spec-writer + implement-feature workflow — shipped as
`docs/prd/P50-prd-disposals-and-cost-basis/` across **15 PRs** for F01–F05 (#825–#840, skipping #836,
an unrelated Docker fix), each feature split into 1–6 stacked PRs to stay within the 8-non-test-file
limit: F01 in 2 (#825 the `CostBasisMethod` strategy, #827 a same-day refactor stripping descriptive
comments the review checklist flagged), F02 in 2 (#826 the disposal record calculator, #828 its
wiring, backfill and app plumbing), F03 in 1 (#829, the recalculation/supersession policy), F04 in 6
(#830 the API read surface, #831 the open-lots endpoint, #832 the broker cost-basis method write
path, #833 the React disposals tab, #834 the admin broker cost-basis-method field, #835 specific-ID
lot allocation on sell/redemption entry), and F05 in 4 (#837–#839 WPF parity for the same four
capabilities, #840 a docs-only PR marking Section 9 acceptance criteria complete).

### Wave 5 — P51 · Tax reporting support **[delivered 2026-09-15]**

*Scoped per §5: classification and reporting, never computation of tax due.*

| # | Feature |
|---|---|
| F01 | `TaxProfile` per asset; `TaxClassification` per event; calculation status |
| F02 | Dated, configurable tax rules — the 2026 BR dividend change is a rule with an effective date, not a code branch |
| F03 | Per-jurisdiction, per-tax-year workbook with evidence references and status |
| F04 | CSV export + React tax page |
| F05 | WPF parity |

**Deliverable:** every disposal and qualifying income event carries an auditable, jurisdiction-tagged
`TaxClassification` with an honest calculation status; a per-tax-year workbook assembles them with
evidence references back to source records; tax rules are dated admin data, not code — and tax due
itself is never computed, per D1.

Like Waves 3–4, P51 used the `docs/prd/` spec-writer + implement-feature workflow — shipped as
`docs/prd/P51-prd-tax-reporting-support/` across **12 PRs** for F01–F05 (#841–852): F01 in 2 (#841 the
`TaxRule` domain entity, #842 its service and API), F02 in 3 (#843 the `TaxClassification` domain
entity, #844 backfill plus an F01 delete-guard, #845 live disposal/credit classification), F03 in 1
(#846, the tax-year workbook), F04 in 3 (#847 the tax profile on the asset detail view, #848 the tax
page and CSV export, #849 the admin tax-rules screen), and F05 in 3 (#850–#852, the same three WPF
parity increments). Running total across the six delivered waves so far: **64 PRs** (12 + 2 + 5 + 18 +
15 + 12).

### Wave 6 — P52 · Portfolio dashboard and data quality **[delivered 2026-09-17]**

| # | Feature |
|---|---|
| F01 | Dashboard aggregate: market value, invested, unrealised, realised, income YTD and lifetime, gross and net XIRR |
| F02 | Allocation by class, currency, country, broker/provider |
| F03 | Data-quality warnings: missing price, missing basis, stale valuation, unknown tax treatment, impossible cash-flow sequence |
| F04 | Upcoming income, coupon and maturity dates |
| F05 | React |
| F06 | WPF parity |

**Deliverable:** a single dashboard, in both front ends, giving the aggregate portfolio figures G7
asked for (market value, invested, unrealised, realised, income YTD and lifetime, gross and net XIRR),
allocation broken down by class/currency/country/broker, data-quality warnings with click-through
navigation to the affected holding, and upcoming income/coupon/maturity projections — verified live,
side by side against the same backend and data, to render identically (allocation percentages
byte-for-byte matching) between React and WPF.

Like Waves 3–5, P52 used the `docs/prd/` spec-writer + implement-feature workflow — shipped as
`docs/prd/P52-prd-portfolio-dashboard-and-data-quality/` across **12 PRs** for F01–F06 (#853–#864):
F01 in 1 (#853, the dashboard aggregate DTO and endpoint), F03 in 1 (#854, the data-quality-report
endpoint — shipped ahead of F02 since both backend features were independent), F02 in 1 (#855, the
allocation-breakdown endpoint), F04 in 1 (#856, the upcoming-income endpoint), F05 in 4 (#857 the
dashboard page and KPI tiles, #858 the allocation panel, #859 the data-quality warnings panel, #860
the upcoming-income panel — each a genuine working vertical slice, never a scaffolded placeholder, to
stay within the 8-non-test-file limit without shipping an empty panel), and F06 in 4 (#861–#864, the
same four WPF parity increments, including the DI-composition fix for the dashboard's click-through
navigation — see the feature's own `spec.md` Decision 13). Running total across the seven delivered
waves so far: **76 PRs** (12 + 2 + 5 + 18 + 15 + 12 + 12).

### Wave 7 — P53 · Corporate actions

Split, reverse split, rights issue, merger, spin-off. Deliberately last: rare, and each needs its
own position-effect rule validated against real history.

### Sequencing

```
P46 ──> P47 ──┬──> P48 ──┬──> P52 [delivered 2026-09-17] ──> P53
              │          │
              └──> P50 ──> P51 [delivered 2026-09-15]

P49 [delivered 2026-09-13] (independent; required only for all-brokers consolidated totals)
```

Approximately 43 PRs plus migration tools — **[corrected 2026-09-10]** likely more, since P46 alone
measured at ~13 rather than the 7 its feature count implied, and the same undercount probably applies
to the later waves.

**[revised 2026-09-13]** The undercount held for P46 (12 actual PRs, close to the ~13 estimate) but
inverted for P47: it shipped in **2** PRs against a 6-feature table, because CI's `openapiFreshness.test.ts`
gate forces a backend contract change and its `Financial.Web` consumer into the same PR — F01–F05
landed together in one PR, WPF parity in a second. P48 landed in **5**, one more than its four-feature
table implied, the extra PR being the whole-feature polish/verification pass.

**[revised 2026-09-14]** P49 landed in **16** PRs against its five-feature table (each feature split
into 2–4 stacked PRs to stay within the 8-non-test-file limit), plus 2 same-day follow-up PRs outside
the table (a performance fix and a gap-closing test) — 18 total for the wave. Running total across the
four delivered waves so far: **37 PRs** (12 + 2 + 5 + 18). Treat the per-wave PR count as unreliable
against any feature-count estimate going forward and expect the actual number only once each wave is
delivered.

P46, P47, P48 and P49 all shipped (§6 above) — P46 fixed a real defect, removed the React/WPF calculation
duplication, and produced the first portfolio-level return figure (`specs/003-investment-calculation-core/spec.md`,
74 functional requirements, 14 success criteria, 7 user stories); P47 widened the transaction/income
vocabulary and added net-of-tax return (`specs/004-transaction-income-vocabulary/`); P48 added
valuation methods and price provenance (`specs/005-valuation-methods-provenance/`); P49 added
multi-currency support and an honest all-brokers total
(`docs/prd/P49-prd-multi-currency-reporting-currency/`). Read each spec/PRD, not this section, for the
decisions taken during that work. Wave 4 (P50 · Disposals and cost basis) is next per the sequencing
above.

**[revised 2026-09-15]** P50 landed in **15** PRs against its five-feature table — F04 alone split
into 6 stacked PRs, F05 into 3 feature PRs plus a closing docs-only PR, both to stay within the
8-non-test-file limit — closer to P49's density than P46's. Running total across the five delivered
waves so far: **52 PRs** (12 + 2 + 5 + 18 + 15).

P46 through P50 have now all shipped (§6 above); P50 added persisted, immutable disposal records with
a per-broker choice of cost-basis method (AverageCost / FIFO / SpecificId) and a supersede-never-rewrite
recalculation policy (`docs/prd/P50-prd-disposals-and-cost-basis/`). D7 (custody vs. domicile) was also
resolved during this revision, ahead of Wave 5's implementation — see §7 — closing G13.

**[revised 2026-09-15]** P51 landed in **12** PRs against its five-feature table — close to P46's
density, each feature in 1–3 stacked PRs to stay within the 8-non-test-file limit. Running total across
the six delivered waves so far: **64 PRs** (12 + 2 + 5 + 18 + 15 + 12).

P46 through P51 have now all shipped (§6 above); P51 closed G5 by classifying every disposal (P50) and
qualifying income event (P47) into a dated, jurisdiction-tagged `TaxClassification` matched against
admin-configured, dated `TaxRule`s, assembled into a per-jurisdiction, per-tax-year workbook with an
honest calculation status (`docs/prd/P51-prd-tax-reporting-support/`) — scoped throughout to reporting
support, never tax-due computation, per D1. Wave 6 (P52 · Portfolio dashboard and data quality) is next
per the sequencing above.

**[revised 2026-09-17]** P52 landed in **12** PRs against its six-feature table — F01/F02/F03/F04 one
PR each (independent backend endpoints), F05 and F06 four PRs each (one genuine, working vertical
slice per dashboard panel, to stay within the 8-non-test-file limit without ever shipping an empty
placeholder panel). Running total across the seven delivered waves so far: **76 PRs**
(12 + 2 + 5 + 18 + 15 + 12 + 12).

P46 through P52 have now all shipped (§6 above); P52 closed G7 by giving the dashboard the aggregate
figures, allocation breakdown, data-quality warnings and upcoming-income projections it was missing,
in both React and WPF (`docs/prd/P52-prd-portfolio-dashboard-and-data-quality/`) — verified live,
side by side against the same backend and data, to render identically between the two front ends. It
also gave G11's report-only unclassified-holding tool (P46-F07) a visible home as dashboard
warnings with click-through navigation. Wave 7 (P53 · Corporate actions) is next per the sequencing
above.

---

## 7. Decisions required before slicing

| # | Decision | Recommendation |
|---|---|---|
| D1 | Tax scope | Reporting support only (§5). Records and classifies; never computes tax due. **[delivered 2026-09-15]** P51 shipped exactly this — `TaxClassification`/`TaxRule`/workbook classify and report; no UK Section 104 pooling, no BR swing-trade rules, no filing is generated. |
| D2 | Cost-basis default | Weighted average — it matches both BR and UK (Section 104) practice. FIFO and specific-ID ship in P50 as options, not defaults. **[delivered 2026-09-15]** P50 shipped exactly this — AverageCost remains the default; FIFO and SpecificId are selectable per broker (P50-F01). |
| D3 | Reporting currency | Defer P49 until P48 ships. Per-broker views are correct without it; only the all-brokers total needs it. **[2026-09-13]** P48 shipped 2026-09-12 — P49 is now unblocked, though the sequencing diagram in §6 already treats it as independent of the P47→P48 chain. **[delivered 2026-09-13]** P49 shipped — see G2 and §6 Wave 3. |
| D4 | `Credit` migration | Rewrite in place with a tool + temp-copy verification, keeping `Value` as derived net, rather than dual-writing a parallel collection. |
| D5 | ~~Historic `Unknown` assets~~ **Unclassified assets** | **[corrected 2026-09-10] Overturned.** A backfill is not possible — classification is manual, one instrument at a time (G11), and 87 of the 90 are closed positions where it changes nothing on screen. P46-F07 therefore reports and never writes. **The dependency inverts:** P48's valuation methods and P51's tax profiles must each define their behaviour for an unclassified holding rather than assuming the rows were cleaned first. Nothing in P46 may require a classification. |
| D6 | Inco / value-based funds | Model as provider-valued holdings in P48-F03 — not as synthetic single-unit assets. |
| D7 | **Country: custody or domicile?** **[new 2026-09-10]** **[resolved 2026-09-15]** | Decide in **P51**, not before. `CountryCode` currently records where a holding is *held*, not where the issuer is domiciled (G13), which is correct for classification and price routing but wrong for withholding. The options are to split the field into custody and domicile, or to derive domicile from the ISIN prefix, which is already present and already contradicts the country field on at least two holdings. P46–P50 need no change. **[resolved 2026-09-15]** Custody only — the user files and pays tax in the broker's jurisdiction regardless of the issuer's domicile. Rather than `CountryCode` (already shown above to be inconsistently set), P51's `TaxProfile` derives jurisdiction from each event's own `Currency` — the same signal P50-F02 already uses for `DisposalRecord.TaxYear`. No new domicile field, no ISIN-prefix derivation, `CountryCode` not consulted. Closes G13 as not applicable to tax-profile derivation. |

---

## 8. Risks

| Risk | Mitigation |
|---|---|
| Schema churn breaks the OpenAPI contract repeatedly | Every wave regenerates the snapshot and the TS types in the same PR; `openapiFreshness.test.ts` already catches omissions. |
| WPF parity lags and the two front ends diverge | Server-side calculation (P46) makes divergence a display bug, not a maths bug. WPF parity is a named feature in every wave, not a follow-up. |
| A migration corrupts the live data file | Every migration runs against a temp copy first and produces a diff report before touching `data/data-investment.json`; the process must be restarted afterwards for changes to load. |
| Tax rules change under the implementation | D1 + dated configurable rules (P51-F02) mean a rule change is configuration, not a release. |
| Programme stalls half-migrated | Each wave is independently deployable and leaves the app fully working; there is no wave that is only useful once a later one lands. |
| **This document goes stale against the code** **[new 2026-09-10]** | It already did: twenty claims were wrong when checked, and two of them described work that could not be done as written. Before specifying each wave, verify its gaps against the code and the data file rather than against this assessment. The structural checks in a spec — numbering, cross-references, checklists — cannot catch a requirement that is internally consistent and false about the codebase. |
| **Point-in-time counts drift** **[new 2026-09-10]** | Price counts and staleness ages change hourly because the app is running and fetching; the data file gained five prices during a single working session. Cite such numbers as illustration, never as the thing a requirement turns on. |
