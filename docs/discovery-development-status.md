# Development Status Report — Current State

Read-only pass, building on the architecture/domain baseline established in `docs/baseline/`. Evidence sources used: source code presence/absence, dedicated test classes, PRD acceptance-criteria checkbox state across all 33 `docs/prd/P01`–`P33` folders, `git log` (commit messages, staged-feature PR sequences, file creation dates), and grep for TODO/FIXME/HACK/`NotImplementedException` (**zero hits in actual source — only in unrelated `.claude/skills/speckit-*` template files**, so in-code markers are not a usable signal in this codebase).

**Key methodology note on PRD checkboxes:** Of 33 PRDs, 20 are 100% checked, 8 are partially checked, and 5 (`P04`–`P08`, all created 2026-07-07 to 2026-07-12) are 0% checked despite their underlying code and tests demonstrably existing — these five predate what looks like a checkbox-tracking convention adopted later in the project (confirmed by file-creation dates: `P28`, fully tracked, was created 2026-08-07). 0%-checked-but-code-exists is treated as a **documentation/tracking gap**, not an implementation gap, and flagged explicitly rather than silently upgraded to "Complete."

## Summary Table

| Capability | Status |
|---|---|
| Broker/Portfolio/Asset Hierarchy + Active/Historic split (P10) | Complete |
| Transaction Recording | Complete |
| Credits (manual dividends/rent) | Complete |
| Asset Price Fetching & Fallback Strategy (P08, P09) | Mostly complete |
| Dividend Tracking & Valuation (P01) | Complete |
| Performance Analytics (XIRR, Profit) | Complete (with a known duplication risk) |
| Portfolio/Asset Summary (P02) | Complete |
| Broker-Level View (P04) | Mostly complete — unverified |
| Credits Analysis (P03) | Partially implemented (Web done, WPF not) |
| Cryptocurrency Handling (P07) | Mostly complete — unverified |
| Investment Price History (P32) | Complete |
| WPF/Web DataGrid Visual Consistency (P05/P06) | Mostly complete — unverified |
| Bank Entity & Balance Management | Complete |
| Bank Round-Up (P13) | Complete |
| Bank Transfers & Balance Reconciliation (P20) | Complete |
| Credit Card Entity, Statements & Settlement (P12/P24/P25/P29) | Mostly complete |
| Expense Tracking | Complete |
| Income & Income Sources (P26/P33-F01) | Complete |
| Recurring Bills / Mensais | Complete |
| Categories (P30) | Complete |
| Tithe Calculation (P33) | Complete |
| Reserve Buckets (P28) | Partially implemented |
| Controle Mãe | Complete |
| CashFlow Quick-Access Investment Snapshots (P18) | Complete |
| Monthly Aggregation | Complete (UI-only, no backend capability) |
| Annual Summary (P15/P16/P17/P19) | Complete |
| Async Persistence (P31) | Complete |
| Sidebar Navigation Redesign (P23) | Complete |
| WPF CashFlow Parity (P21) | Mostly complete — 5 ACs likely superseded by P23 |

---

## INVESTMENT CONTEXT

### Broker/Portfolio/Asset Hierarchy + Active/Historic split
- **Status:** Complete. **Completeness:** High.
- **Tests:** `BrokerTests`, `PortfolioTests`, `AssetTests`, `InvestmentsTests` (Domain); `NavigationServiceTests`, `PortfolioAssetSummaryServiceTests` (Application).
- **Docs/spec:** P10 (`historic-investments`) — 42/42 AC boxes checked.
- **TODO/FIXME:** None found.
- **Incomplete implementations:** None observed.
- **Dependencies/blockers:** None.
- **Uncertainty:** None significant.

### Transaction Recording
- **Status:** Complete. **Completeness:** High — weighted-average-cost, realized-gain, and short-position behavior are all explicitly test-asserted.
- **Tests:** `TransactionTests`, `TransactionsTests` (Domain, thorough); `TransactionServiceMutationTests`, `TransactionServiceQueryTests` (Application).
- **Docs/spec:** No dedicated numbered PRD identified — foundational, pre-existing before the sequential PRD system, documented in `context.md`.
- **TODO/FIXME:** None.
- **Incomplete implementations:** None functionally; replay-on-edit uses list order not date order (documented as a confirmed deliberate implementation choice, not a bug — see `docs/baseline/09-domain-investment.md`).
- **Dependencies/blockers:** None.
- **Uncertainty:** None significant.

### Credits (manual dividends/rent entry)
- **Status:** Complete. **Tests:** `CreditTests`, `CreditServiceTests`, `CreditTypeParserTests`.
- **Docs/spec:** Feeds P02/P03/P04; no standalone PRD.
- **TODO/FIXME/Incomplete:** None found.
- **Uncertainty:** None significant.

### Asset Price Fetching & Fallback Strategy (P08 asset-snapshot-fetch-strategy, P09 br-bond-finance-service-integration)
- **Status:** Mostly complete. **Apparent completeness:** High by code/test evidence, but PRD tracking is inconsistent.
- **Test evidence:** `AssetPriceServiceTests`, `BondAssetPriceFetcherTests`, `CryptocurrencyAssetPriceFetcherTests`, `FallbackFinanceServiceTests`, `StandardAssetPriceFetcherTests`, `GoogleFinanceServiceTests`, `YahooFinanceServiceTests`, `StatusInvestFinanceServiceTests` — all confirmed present, matching every dispatch/fallback rule described in the PRDs.
- **Documentation status:** P08 is 0/21 checked (created 2026-07-12, predates the checkbox convention — code confirmed to exist regardless). P09 is 7/20 checked, with **13 unchecked items concentrated exactly on `StatusInvestFinanceService`/`BondAssetPriceFetcher` core behavior** (slug derivation, DI registration, dispatch exclusivity, 404 handling) — yet the corresponding classes and their dedicated test classes exist in the codebase.
- **Known TODOs/FIXMEs:** None in code.
- **Obvious incomplete implementations:** None found in code; the gap is entirely in whether the specific PRD-listed edge cases (e.g., the exact slug transform "TESOURO IPCA+ 2029" → "tesouro-ipca-2029") were individually verified — this pass did not re-run or trace that specific test assertion.
- **Uncertainty — genuine:** Whether P09's 13 unchecked items represent (a) a stale checklist against completed work, or (b) real unverified edge cases. Recommend a targeted look at `BondAssetPriceFetcherTests`/`StatusInvestFinanceServiceTests` content before assuming either way.

### Dividend Tracking & Valuation
- **Status:** Complete. **Docs/spec:** P01 (`web-frontend-parity`) — 92/92 checked.
- **Tests:** `DividendServiceTests`, `DividendValuationRulesTests`.
- **Known defect (confirmed, not fixed):** `DividendDataSourceAdapter` accepts an unused `exchange` parameter — confirmed error by product owner, not yet corrected. A code-quality defect within an otherwise complete feature, not a missing-functionality gap.
- **Uncertainty:** None beyond the confirmed defect above.

### Performance Analytics (XIRR, Profit)
- **Status:** Complete (backend and a separate frontend implementation both exist and are in active use).
- **Tests:** `XirrCalculatorTests`, `ProfitCalculatorTests`, `XirrCalculationServiceTests` (backend). Frontend `xirr.ts` has its own `xirr.test.ts`.
- **Docs/spec:** No dedicated PRD identified — foundational.
- **Known risk (not a completeness gap, but worth tracking):** Two independent XIRR implementations (backend `XirrCalculator`, frontend `xirr.ts`, actively used by `PortfolioSummaryTab.tsx`) — confirmed live duplication, divergence risk if either changes without the other.
- **Uncertainty:** Whether the two implementations currently agree on every edge case (e.g., <2 cash flows) was not independently re-verified in this pass.

### Portfolio/Asset Summary (P02)
- **Status:** Complete. **Docs/spec:** 46/46 checked.
- **Tests:** `PortfolioAssetSummaryServiceTests`, `SummaryServiceTests`.
- **Uncertainty:** None significant.

### Broker-Level View (P04 broker-level-view-improvements)
- **Status:** Mostly complete — genuinely unverified. **Docs/spec:** 0/73 checked (created 2026-07-07, predates checkbox convention).
- **Code evidence:** `BrokerBreakdownService`/`Builder` and `BrokerBreakdownCharts.tsx` confirmed to exist from prior discovery passes.
- **Uncertainty — real, not just a documentation lag:** Because literally none of P04's 73 specific ACs are checked (unlike P07/P08 where the class/test *names* line up closely with the PRD's stated design), this pass cannot positively confirm the exact behaviors P04 specifies (e.g., "Broker-level totals exclude all transactions and credits from assets in the Encerradas portfolio," "A broker with zero eligible portfolios returns all totals as 0, not an error") were actually implemented as described, only that a broker-breakdown feature of some form exists. Recommend direct verification before relying on this PRD's specific ACs.

### Credits Analysis (P03 portfolio-credits-analysis)
- **Status:** Partially implemented. **Docs/spec:** 29/42 checked, with all 13 unchecked items concentrated on WPF-specific behavior (5 new DataGrid columns, footer panel layout/values).
- **Verified directly this pass:** grepped `Financial.App` for the P03-specific field names (`LastMonthCredits`, `EstAnnualCredits`, etc.) — **zero matches**. The Web side (`CreditsTab.tsx`, `useCredits.ts`, backend `CreditFrequencyAnalyzer`) is confirmed implemented and tested.
- **Obvious incomplete implementation — confirmed:** The WPF-side DataGrid columns and footer panel described in P03 F02/F03 do not exist in the WPF codebase. A genuine, concrete gap, not a documentation lag — it directly matches the WPF/Web feature-parity expectation stated in `CLAUDE.md`.
- **Uncertainty:** None on the core finding; **UNKNOWN** whether this WPF work is scheduled/in-progress elsewhere (no branch/PR evidence checked beyond `main`'s history).

### Cryptocurrency Handling (P07)
- **Status:** Mostly complete — unverified at the AC level. **Docs/spec:** 0/29 checked (created 2026-07-11, predates checkbox convention).
- **Code evidence:** `GlobalAssetClass.Cryptocurrency`, `CryptocurrencyAssetPriceFetcher` + `CryptocurrencyAssetPriceFetcherTests`, `GoogleFinanceCryptocurrencyUrlTests` all confirmed present; matches `context.md`'s description of a dedicated Coinbase broker.
- **Uncertainty:** Same class of caveat as P04/P08 — code clearly exists, but the specific 29 listed ACs were not individually re-verified against test assertions in this pass.

### Investment Price History (P32)
- **Status:** Complete, and very recently shipped. **Docs/spec:** fully checked.
- **Git evidence:** 7 consecutive, tightly-staged commits (#427–#433) ending "completing F02," immediately preceding the current HEAD's unrelated refactor/tithe commits — a strong, concrete signal of a feature that was actively built and finished right before this discovery pass began.
- **Web:** `PriceHistoryTab.tsx` confirmed. **WPF:** price commands/tab wired per commit `77ee748`.
- **Uncertainty:** None significant.

### WPF/Web DataGrid Visual Consistency (P05/P06)
- **Status:** Mostly complete — unverified. **Docs/spec:** 0/11 and 0/10 checked respectively (both created 2026-07-10, predates checkbox convention; both have a single confirmed merged PR each — #130, #131).
- **Code evidence:** `App.xaml` (WPF) contains `DataGrid`-related resources, consistent with a shared style having been added; Web's global `styles/data-table.css` (confirmed in prior discovery) is consistent with P06's intent.
- **Uncertainty:** Whether every specific visual-consistency criterion in these PRDs was fully realized was not independently checked pixel-by-pixel or component-by-component.

---

## CASHFLOW CONTEXT

### Bank Entity & Balance Management
- **Status:** Complete. **Tests:** `BankTests` + Application/API test coverage confirmed in prior passes. **Docs/spec:** covered under P11/P13 foundational work.
- **Uncertainty:** None significant.

### Bank Round-Up (P13)
- **Status:** Complete. **Docs/spec:** 24/24 checked. Round-up math verified consistent across Domain, WPF, and Web in the domain-discovery pass.
- **Uncertainty:** None.

### Bank Transfers & Balance Reconciliation (P20)
- **Status:** Complete. **Docs/spec:** 33/33 checked.
- **Known, accepted non-gap:** No overdraft protection on transfers/adjustments (unlike Reserve) — explicitly confirmed by the product owner as acceptable for the current MVP stage, not a completeness defect.
- **Uncertainty:** None.

### Credit Card Entity, Statements & Settlement (P12, P24, P25, P29)
- **Status:** Mostly complete. **Docs/spec:** P12 31/31 checked, P25 46/46 checked, P29 29/29 checked; P24 37/41 checked (4 unchecked — two are minor regression-continuity checks, two are meta "no cross-feature criteria apply" bookkeeping lines, none describe missing core functionality).
- **Tests:** `CardStatementTests`, `CardStatementServiceTests`, `CardStatementsEndpointsTests` all confirmed present.
- **Known defect (confirmed, not fixed):** Re-invoking mark-paid/unmark-paid on a statement already in that state silently no-ops with no user feedback — confirmed by the product owner as a mistake requiring a fix, not yet implemented.
- **Uncertainty:** P25's specific invoice-date business rules were never independently read in the domain-discovery pass (marked UNKNOWN there) — this status report relies on the PRD's checkbox completion (46/46) as the only evidence for P25 specifically, not direct code verification.

### Expense Tracking
- **Status:** Complete. Full-stack test coverage confirmed (Domain/Application/API/Web component+hook tests all present).
- **Uncertainty:** None significant.

### Income & Income Sources (P26, P33-F01)
- **Status:** Complete. **Docs/spec:** P26 27/27 checked; P33 (covers F01) 14/14 checked.
- **Uncertainty:** None.

### Recurring Bills / Mensais
- **Status:** Complete. Covered within P11's foundational checklist (59/62 checked — none of the 3 unchecked items concern Mensais specifically).
- **Uncertainty:** `ResetAllToUnsetAsync`'s monthly-reset trigger was undocumented in code until this discovery's clarification pass confirmed its intended usage — a documentation gap now resolved in `docs/baseline/`, not an implementation gap.

### Categories (P30)
- **Status:** Complete. **Docs/spec:** 29/30 checked; the one unchecked item is explicitly self-documented in the PRD text itself as an intentional deviation (flag-and-skip vs. abort), not a real gap.
- **Uncertainty:** None.

### Tithe Calculation (P33)
- **Status:** Complete, and very recently shipped. **Docs/spec:** 14/14 checked.
- **Git evidence:** The most recent business-feature commits in the entire history (`73d6a8d` through `4077b9d`) are all P33-F01/F02 work, immediately followed only by a repo-wide refactor and a doc-cleanup commit — this is the newest completed capability in the codebase.
- **Uncertainty:** None on completeness; one **UNKNOWN** open item carried from domain discovery — whether the wife's-income-net-of-tithe reserve-bucket-split rule (a separate, related rule) is actually implemented in `ReserveService` was never verified (see Reserve Buckets below).

### Reserve Buckets (P28)
- **Status:** Partially implemented. **Docs/spec:** 39/40 checked — but the checklist itself does not appear to cover the missing pieces below, so checkbox completion overstates actual completeness here.
- **Tests:** `ReserveBucketTests`, `ReserveMovementTests`, `ReserveServiceTests`, `ReserveBucketServiceTests`, `ReserveBucketNameResolverTests`, endpoint tests — all confirmed present for the core split/withdrawal/balance functionality.
- **Confirmed gaps (verified directly in this pass, not implemented):**
  1. **"A backup of the data file is created before any write occurs"** — the one unchecked P28 AC. Grepped `Financial.Shared.Infrastructure` for any backup mechanism — **none found**. `LocalJsonStorage` does a direct overwrite with no backup step. Confirmed not implemented.
  2. The non-blocking percentage-sum-warning (documented P28 objective) was not found implemented in `ReserveService` in the domain-discovery pass, and the product owner separately confirmed it's missing and should be built server-side.
  3. `data-cashflow.example.json` is missing its `ReserveBuckets` seed key — confirmed by the product owner as a genuine gap.
- **Uncertainty:** Whether the wife's-income-net-of-tithe bucket-split rule is implemented remains genuinely **UNKNOWN** — not verified in any pass so far.

### Controle Mãe
- **Status:** Complete. Covered within P11's foundational checklist. Sign convention (positive/negative meaning) was undocumented anywhere in the repository until the clarification pass — now resolved in `docs/baseline/`, not a code gap.
- **Uncertainty:** None on functionality; historical documentation gap already closed.

### CashFlow Quick-Access Investment Snapshots (P18)
- **Status:** Complete. **Docs/spec:** 24/25 checked; the one unchecked item is a narrow migration-time edge case (abort behavior on an unmigrated enum value with no registry entry).
- **Uncertainty:** Whether that specific migration-abort behavior was implemented is genuinely open — not independently verified.

### Monthly Aggregation
- **Status:** Complete as a UI-layer feature; **N/A as a backend capability** — confirmed no `Monthly` Domain entity, Application service, or API endpoint exists; it is purely a client-side composition over other already-complete capabilities.
- **Uncertainty:** None.

### Annual Summary (P15, P16, P17, P19)
- **Status:** Complete. **Docs/spec:** P15 29/29, P16 23/23, P17 11/11, P19 28/28 — all fully checked.
- **Tests:** `AnnualSummaryServiceTests`, `AnnualSummaryEndpointsTests`, `AnnualSummaryViewModelTests` confirmed.
- **Uncertainty:** None on completeness; the 2017/11-month special case was previously undocumented but is now a confirmed, understood historical-data artifact, not a bug.

### Async Persistence (P31)
- **Status:** Complete, cross-cutting infrastructure. **Docs/spec:** 54/54 checked, wired symmetrically into both bounded contexts (separate parallel F04/F05 features per the PRD).
- **Uncertainty:** None on completion status. A separate, unrelated architectural risk (no cross-process write coordination between `Financial.Api` and `Financial.App`) is a design characteristic, not an incomplete-implementation signal — already documented in `docs/baseline/02-architecture.md`.

### Sidebar Navigation Redesign (P23)
- **Status:** Complete. **Docs/spec:** 42/42 checked. Matches the confirmed current WPF navigation model (single collapsible sidebar, per `NavTree.cs`).
- **Uncertainty:** None.

### WPF CashFlow Parity (P21)
- **Status:** Mostly complete. **Docs/spec:** 53/58 checked; the 5 unchecked items all describe a **top-level tab-strip** navigation model ("MainWindow's top-level tab strip shows 'Investments' and 'Cash Flow' as two peer domain tabs...").
- **Cross-referenced finding (moderate confidence, not fully verified):** This tab-strip design is inconsistent with the confirmed *current* WPF navigation model — a sidebar, per P23 (`Sidebar Navigation Redesign`, fully checked, created after P21 based on PRD numbering and typical chronological ordering). This strongly suggests P21's 5 unchecked items describe an earlier navigation design that was **superseded by P23**, not a feature that was left unbuilt.
- **Uncertainty — flagged, not resolved:** This supersession theory was not confirmed by directly diffing P21 against P23's text or by checking whether P21 was ever explicitly marked "superseded." Treat P21's unchecked items as likely-stale rather than confirmed-obsolete until verified directly.

---

## Cross-cutting observations

- **No TODO/FIXME/HACK markers exist anywhere in actual application source code** — a positive discipline signal, but it also means this codebase gives no in-code "planned work" markers to lean on; all incompleteness signals in this report come from PRD checkbox state, test presence/absence, and direct code/grep verification.
- **The PRD checkbox convention was adopted partway through the project** (sometime between P08's creation, 2026-07-12, and P28's, 2026-08-07). Any 0%-checked PRD should be read as "predates tracking," not "unbuilt," unless independently contradicted by code evidence (as happened concretely with P03/WPF).
- **Git history shows active, current, PR-per-feature-slice development** with no stale/abandoned branches visible from `main`'s log — the two most recent capability areas worked on were P32 (Investment Price History, now complete) and P33 (Tithe accuracy, now complete), followed by a repo-wide DRY/SOLID refactor and a documentation-cleanup pass.
