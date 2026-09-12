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

### G1 — Transaction vocabulary is two values

`Transaction.TransactionType` is `{ Buy, Sell }`. The brief specifies seventeen. Fees exist only
as a field on a buy/sell; tax withheld, return of capital, capital calls, transfers, redemption,
maturity, splits, and standalone valuation adjustments are **unrepresentable**. `Credit` carries
three income types and a single positive `Value` — no gross, no withheld, no net, no
return-of-capital. This is the root gap: net-of-tax return, tax reporting, and most instrument
rules are all blocked behind it.

### G2 — No currency on transactions, no FX

Currency lives on `Broker` only (`Trading 212` / `Coinbase` / `FreeTrade` = GBP, `XPI` = BRL).
There is no per-transaction currency, no reporting currency, no dated FX rate, and therefore no
honest all-brokers total. An `IExchangeRateProvider` (Frankfurter) exists but sits in **CashFlow**
Infrastructure — bounded-context isolation forbids Investment referencing it, so it must be
promoted to a shared abstraction rather than reused in place.

### G3 — Cost basis is hard-coded and order-dependent

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

### G4 — No disposal record

Realised gain is a running scalar (`Transactions.RealizedCapitalGain`) recomputed by full replay.
Nothing persists *which* units were disposed, under which method, at what basis, in which tax
year. The brief requires that a later configuration change must not retroactively rewrite a filed
result — today, changing anything replays everything.

### G5 — No tax domain at all

Zero fields anywhere: no jurisdiction, tax year, event classification, withheld amount, exempt
amount, evidence reference, or calculation status. No net-of-tax return of any kind.

### G6 — Valuation maths lives in the front ends

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

### G7 — No portfolio-level return, no dashboard

XIRR is per-asset only. `AggregatedSummaryDTO` is four numbers (bought / sold / credits /
invested): no market value, no unrealised total, no income YTD, no allocation by currency, country
or provider, no gross-vs-net portfolio return, no data-quality warnings.

### G8 — Valuation method is implicit

Every asset is priced by ticker lookup. There is no `valuationMethod`
(market price / NAV / provider value / manual / bond quote), so **value-based funds and
property-platform investments (Inco) have no honest representation**. `Transaction` also rejects
`quantity <= 0` and `unitPrice <= 0`, so a plain "I contributed £5,000" cannot be recorded without
inventing units.

### G9 — No corporate actions

Splits, consolidations, rights issues, mergers and spin-offs would silently corrupt quantity and
average price.

### G10 — Provenance is thin

`AssetPriceSnapshot` has Date / Price / IsManual — no source, source reference, market status,
currency, or retrieved-at. `Transaction` has no source, source reference, estimated flag, or
created/updated timestamps. The brief requires all of these for reproducibility and for detecting
provider corrections.

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
- **The same-date tie-break is undefined, and defining it moves real figures.** **[new 2026-09-10]**
  A transaction carries a date but no time, so any date-ordered replay must decide how to order
  same-date rows. Two holdings store a sale ahead of a purchase on one date; ordering purchases first
  (the rule chosen) changes Bitcoin's average price from 62,713.15 to 62,709.05 and its realised gain
  from 0.25 to 0.17, and AGNC's realised gain from 9.98 to 9.66. Small, one-off and defensible as a
  correction — but it must be stated, not discovered.
- **`TotalInvested = TotalBought - TotalSold`** (`AssetInvestedAmountSelector`) understates the
  cost basis of a partially-sold position. Buy 100 @ £10, sell 50 @ £20 → reported invested = £0
  while 50 units are still held at £500 cost. **[corrected 2026-09-10]** There are **five** independent
  derivations of this figure, not the two the selector suggests: the rows, the portfolio/broker total
  (`SummaryService` hardcodes the subtraction and never consults the selector), the allocation chart
  (which additionally filters `> 0`), and a footer in each front end that sums the rows. Both front
  ends therefore already print two different "total invested" figures on the same screen. All 15
  Historic portfolios disagree today; the 10 Active ones reconcile only because no active holding
  currently has quantity zero — the code paths diverge, the data just has not exposed it yet.
- **The invested figure cannot be redefined in isolation.** **[new 2026-09-10]** The same number
  serves three purposes at once: the invested amount, the basis for portfolio weight, and the
  denominator of the income yield percentages. Changing it for one purpose silently moves the other
  two — so market-based weight cannot be introduced without accidentally converting yield-on-cost into
  yield-on-market. The three must be separated before either change ships.
- **`PortfolioWeight` is cost-based, not market-based** (`PortfolioAssetSummaryBuilder`
  `weightBasis`). The brief's allocation views require market value.
- **Yield percentages are yield-on-cost**, not market yield. Legitimate, but currently unlabelled.
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

### G13 — `CountryCode` records custody, not domicile **[new 2026-09-10]**

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

### Wave 0 — P46 · Investment calculation core

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

### Wave 1 — P47 · Transaction and income event vocabulary

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

### Wave 2 — P48 · Valuation methods and provenance

*Unblocks the two instrument types that cannot be represented today.*

| # | Feature |
|---|---|
| F01 | Widen `AssetPriceSnapshot`: currency, source, source reference, market status, retrieved-at (`IsManual` becomes derived from source) |
| F02 | `Asset.ValuationMethod` + `IncomePolicy`; drive fetcher routing from method, not class alone |
| F03 | Value-based holdings: contribution / withdrawal / provider valuation without inventing units — **funds without units, Inco** |
| F04 | React: as-of, source, market status, stale-data warning |
| F05 | WPF parity |

### Wave 3 — P49 · Multi-currency and reporting currency

*Gates only the all-brokers consolidated view; per-broker views work without it.*

| # | Feature |
|---|---|
| F01 | Promote `IExchangeRateProvider` to `Financial.Shared.Abstractions`; implementation into its own `Integrations/` project |
| F02 | `Transaction.Currency` + dated FX rate captured and stored at record time |
| F03 | Reporting-currency setting; converted totals with original currency always preserved and shown |
| F04 | React |
| F05 | WPF parity |

### Wave 4 — P50 · Disposals and cost basis

| # | Feature |
|---|---|
| F01 | `CostBasisMethod` strategy in Domain: AverageCost (default) / FIFO / SpecificId, selected per jurisdiction |
| F02 | Persisted immutable `DisposalRecord` per sell/redemption: date, units, method, basis, proceeds, fees, gain/loss, tax year |
| F03 | Recalculation policy — changing method never rewrites an existing record; recalculation from a date creates new ones |
| F04 | React disposal view |
| F05 | WPF parity |

### Wave 5 — P51 · Tax reporting support

*Scoped per §5: classification and reporting, never computation of tax due.*

| # | Feature |
|---|---|
| F01 | `TaxProfile` per asset; `TaxClassification` per event; calculation status |
| F02 | Dated, configurable tax rules — the 2026 BR dividend change is a rule with an effective date, not a code branch |
| F03 | Per-jurisdiction, per-tax-year workbook with evidence references and status |
| F04 | CSV export + React tax page |
| F05 | WPF parity |

### Wave 6 — P52 · Portfolio dashboard and data quality

| # | Feature |
|---|---|
| F01 | Dashboard aggregate: market value, invested, unrealised, realised, income YTD and lifetime, gross and net XIRR |
| F02 | Allocation by class, currency, country, broker/provider |
| F03 | Data-quality warnings: missing price, missing basis, stale valuation, unknown tax treatment, impossible cash-flow sequence |
| F04 | Upcoming income, coupon and maturity dates |
| F05 | React |
| F06 | WPF parity |

### Wave 7 — P53 · Corporate actions

Split, reverse split, rights issue, merger, spin-off. Deliberately last: rare, and each needs its
own position-effect rule validated against real history.

### Sequencing

```
P46 ──> P47 ──┬──> P48 ──┬──> P52 ──> P53
              │          │
              └──> P50 ──> P51

P49 (independent; required only for all-brokers consolidated totals)
```

Approximately 43 PRs plus migration tools — **[corrected 2026-09-10]** likely more, since P46 alone
measured at ~13 rather than the 7 its feature count implied, and the same undercount probably applies
to the later waves.

P46 delivers standalone value: it fixes a real defect, removes the React/WPF calculation duplication,
and produces the first portfolio-level return figure. It is now specified in full at
**`specs/003-investment-calculation-core/spec.md`** (74 functional requirements, 14 success criteria,
7 user stories), with the decisions taken during that work recorded in its Clarifications section and
its `checklists/requirements.md`. Read the spec, not this section, before planning P46.

---

## 7. Decisions required before slicing

| # | Decision | Recommendation |
|---|---|---|
| D1 | Tax scope | Reporting support only (§5). Records and classifies; never computes tax due. |
| D2 | Cost-basis default | Weighted average — it matches both BR and UK (Section 104) practice. FIFO and specific-ID ship in P50 as options, not defaults. |
| D3 | Reporting currency | Defer P49 until P48 ships. Per-broker views are correct without it; only the all-brokers total needs it. |
| D4 | `Credit` migration | Rewrite in place with a tool + temp-copy verification, keeping `Value` as derived net, rather than dual-writing a parallel collection. |
| D5 | ~~Historic `Unknown` assets~~ **Unclassified assets** | **[corrected 2026-09-10] Overturned.** A backfill is not possible — classification is manual, one instrument at a time (G11), and 87 of the 90 are closed positions where it changes nothing on screen. P46-F07 therefore reports and never writes. **The dependency inverts:** P48's valuation methods and P51's tax profiles must each define their behaviour for an unclassified holding rather than assuming the rows were cleaned first. Nothing in P46 may require a classification. |
| D6 | Inco / value-based funds | Model as provider-valued holdings in P48-F03 — not as synthetic single-unit assets. |
| D7 | **Country: custody or domicile?** **[new 2026-09-10]** | Decide in **P51**, not before. `CountryCode` currently records where a holding is *held*, not where the issuer is domiciled (G13), which is correct for classification and price routing but wrong for withholding. The options are to split the field into custody and domicile, or to derive domicile from the ISIN prefix, which is already present and already contradicts the country field on at least two holdings. P46–P50 need no change. |

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
