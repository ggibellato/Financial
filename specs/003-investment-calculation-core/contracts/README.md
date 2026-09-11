# Interface Contracts: Investment Calculation Core

**Branch**: `003-investment-calculation-core` | **Date**: 2026-09-10

This feature exposes its behaviour through **two surfaces, not one**, and that asymmetry is the
single most important thing in this folder.

`Financial.App` is **not** an HTTP client — the constitution states it directly ("`Financial.App`
hosts both contexts' Application/Infrastructure layers in-process; it is not an HTTP client of
`Financial.Api`"), and there is no `HttpClient` anywhere in the project. So the contract that makes
both front ends agree is the **Application interface**, and the REST endpoint is an *additional*
surface for the browser only.

```
        Domain rules  (HoldingValuationCalculator, SaleCoverageRule, TransactionReplayOrder)
                │
        Application services + interfaces        ← the contract that guarantees parity
           ╱                        ╲
   Financial.App                Financial.Api
   (in-process DI)              (HTTP)  →  Financial.Web
```

A change to an Application DTO is therefore a change to **both** surfaces at once: it recompiles WPF
and it reshapes the wire. That is why the repository treats Application DTOs as the literal wire
format and why every DTO change below is sequenced with its snapshot regeneration.

---

## 1. In-process contract (consumed by `Financial.App`)

### New

| Interface | Member | Notes |
|---|---|---|
| `IHoldingValuationService` | valuation for a holding | Registered in `InvestmentApplicationServiceCollectionExtensions`. **No view model needs to resolve it** — valuation arrives precomputed on the summary DTOs. See the open item in `research.md`. |

### Changed

| Interface | Change | Compatibility |
|---|---|---|
| `IXirrCalculationService` | **+ overload** `Calculate(cashFlows, terminalValue, asOf)` | Additive. The existing two-argument member delegates with `DateTime.Today`, so no call site changes and `XirrController` is untouched. FR-044 protects `XirrCalculator`, which is not modified. |
| `ISummaryService` | return type gains five fields | Source-compatible: the DTO is `sealed` with `init`-only properties and every construction is an object initializer. |
| `IPortfolioAssetSummaryService` | return type gains valuation fields; `PortfolioWeight` becomes nullable | The nullable widening **intentionally breaks the compile** at `PortfolioAssetSummaryRowViewModel`, forcing it to declare its unknown-rendering. |

### Domain surface (not injected — called directly)

| Type | Entry point | Contract |
|---|---|---|
| `HoldingValuationCalculator` | `Calculate(...)` / `NotMarkedToMarket(...)` | Pure. Two named entry points rather than a `bool` flag, because `InvestmentScope` is an Application enum. |
| `SaleCoverageRule` | `FindFirstUncoveredSale(...)` | Pure, **non-throwing**, quantity-only. Returns the first violation or null. |
| `TransactionReplayOrder` | `Sort(...)` | One owner of the FR-002 ordering, shared by the collection, the rule and the report. |
| `Asset` | `RecordTransaction` / `ReviseTransaction` / `RetractTransaction` | **Strict** — raise `InvestmentRuleViolationException`. The tolerant `AddTransaction` / `UpdateTransaction` / `RemoveTransaction` / `AddTransactions` are unchanged, which is what keeps loading and importing working. |

---

## 2. HTTP contract (consumed by `Financial.Web`)

**No new endpoint.** Every figure this feature adds travels on responses that already exist.

| Endpoint | Change |
|---|---|
| `GET /summary/broker/{brokerName}` | `AggregatedSummaryDTO` gains 5 fields; `TotalInvested` changes meaning |
| `GET /summary/portfolio/{brokerName}/{portfolioName}` | same |
| `GET /summary/portfolio/{brokerName}/{portfolioName}/assets` | items gain valuation fields; `portfolioWeight` becomes nullable |
| `GET /assets/{brokerName}/{portfolioName}/{assetName}` | `AssetDetailsDTO` gains valuation fields |
| `POST /transactions`, `PUT /transactions`, `DELETE /transactions` | may now return **409 Conflict** carrying the refusal message |

### Error contract for a refused sale

`InvestmentRuleViolationException` → **409 Conflict** via `DomainExceptionMappingMiddleware`, with the
domain message in the ProblemDetails `detail`. `financialApiClient` already surfaces `problem.detail`
verbatim as `saveError`, so the web side needs no new plumbing.

The message has two forms, per FR-066:

- the offending sale **is** the one being edited — *"Only 100 units are held on 15/06/2026. Enter 100 or less."*
- the offending sale is a **later** one — *"This change would leave the sale of 80 units on 15/06/2026 short by 30 units."*

The quantity is formatted **round-trip, not `N2`** (FR-012), so a user closing a position entirely can
copy `2128.37599271` out of the message.

> **WPF has no equivalent path today and would crash.** `TransactionsTabViewModel` has no `try`/`catch`
> in Add/Update/Delete and `App.xaml.cs` registers no `DispatcherUnhandledException` handler, so the
> exception would escape an `async void` command handler. This is a pre-existing latent defect that
> this feature makes reachable, and FR-062 requires it fixed in the same increment.

### Contract regeneration, every time a DTO changes

```powershell
$env:UPDATE_OPENAPI_SNAPSHOT=1; dotnet test Tests/Financial.Api.Tests; Remove-Item Env:\UPDATE_OPENAPI_SNAPSHOT
cd Financial.Web; npm run generate-api-types
```

The `Remove-Item` matters — leave it set and every later run silently rewrites the snapshot instead of
checking it. `openapiFreshness.test.ts` fails if the generated types drift from the snapshot, so a
forgotten regeneration is caught in the same PR.

---

## 3. The contract change no tooling will catch

`AggregatedSummaryDTO.TotalInvested` and `PortfolioAssetSummaryItemDTO.TotalInvested` **keep their
name and their type while changing meaning** (FR-019: cost of open units, not bought − sold).

The snapshot compares *shapes*. `openapiFreshness.test.ts` compares *shapes*. `tsc -b` compares
*shapes*. All three pass. Every automated guard this repository has is blind to it.

It therefore needs verification of its own — an assertion on the value for a partially-sold holding,
not on the schema — and it must be stated in the PR body, because it moves numbers on screen:

| | Change |
|---|---|
| Active holdings whose invested amount moves | 5 of 28, one from **−683.09 to 28.65** |
| Historic portfolio totals | all 15 move, e.g. `XPI/FII` **2,949.87 → 61,413.07** |
| Allocation chart | gains one slice as a holding crosses `> 0` |

The historic movement is large because the total is currently `bought − sold` while its own rows have
always shown `bought`. That is FR-022 being satisfied for the first time, not a regression — but no
reviewer should meet it for the first time in a diff.
