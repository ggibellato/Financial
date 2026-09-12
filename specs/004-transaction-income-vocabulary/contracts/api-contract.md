# API Contract Changes: Transaction and Income Event Vocabulary

`Financial.Api` DTOs are the literal wire format (Constitution: Technology & Persistence
Constraints). Every change below is a wire-format change and MUST regenerate the OpenAPI snapshot
(`Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`) and `Financial.Web`'s generated TS
types in the same PR.

## `TransactionDTO` / `TransactionCreateDTO` / `TransactionUpdateDTO`

`Financial.Investment.Application/DTOs/Transaction*.cs`

| Field | Change |
|---|---|
| `Type` | Wire values widen from `"Buy" \| "Sell"` to also include `"Fee" \| "Redemption" \| "TransferIn" \| "TransferOut" \| "CapitalCall" \| "ReturnOfCapital"`. |
| `Quantity`, `UnitPrice` | No shape change; validation on the server relaxes to `0` (not required `> 0`) when the selected `Type`'s quantity effect is `None` (FR-008). |
| `Fees` | Unchanged shape; now meaningful (optionally non-zero) for every type per FR-003/FR-004's fee-independence rule. |
| `Withheld` | **New** `decimal`, default `0`, on `TransactionDTO`, `TransactionCreateDTO`, `TransactionUpdateDTO`. |
| `TotalPrice` | **Removed** from `TransactionDTO` (was the Gross±Fees convenience field for Buy/Sell only) — **replaced by `NetCash`** (`decimal`), the type-aware signed cash-movement figure from `research.md` #2. Removing a field is a breaking wire-format change for any consumer reading `totalPrice`; `Financial.Web`/`Financial.App` call sites are updated in the same PR (FR-021 front-end parity). |

## `CreditDTO` / `CreditCreateDTO` / `CreditUpdateDTO`

`Financial.Investment.Application/DTOs/Credit*.cs`

| Field | Change |
|---|---|
| `Type` | Wire value `"Rent"` is retired; existing rows serve `"SecuritiesLendingIncome"` after the migration (FR-013). New wire value `"Coupon"` added (FR-014). |
| `Value` | No shape change; server validation relaxes from "must be `> 0`" to "must not be `0`" — negative is now valid (a correction, FR-015). |
| `Withheld` | **New** `decimal`, default `0`. |
| `NetAmount` | **New**, read-only (`CreditDTO` only, not the Create/Update request DTOs) `decimal` — `Value - Withheld`. |

## New: transaction type effect metadata endpoint

To satisfy FR-021 (both front ends show identical terminology/field order/validation) without
`Financial.Web` and `Financial.App` each hand-maintaining a duplicate copy of the
`TransactionTypeEffects` switch (`data-model.md`), the effect table is served once from the
Application layer both front ends already share in-process (`Financial.App`) or over HTTP
(`Financial.Web`):

```
GET /transactions/type-effects
200 OK
[
  { "type": "Buy", "quantityEffect": "Increase", "cashEffect": "Out" },
  { "type": "Sell", "quantityEffect": "Decrease", "cashEffect": "In" },
  { "type": "Fee", "quantityEffect": "None", "cashEffect": "Out" },
  { "type": "Redemption", "quantityEffect": "Decrease", "cashEffect": "In" },
  { "type": "TransferIn", "quantityEffect": "Increase", "cashEffect": "None" },
  { "type": "TransferOut", "quantityEffect": "Decrease", "cashEffect": "None" },
  { "type": "CapitalCall", "quantityEffect": "None", "cashEffect": "Out" },
  { "type": "ReturnOfCapital", "quantityEffect": "None", "cashEffect": "In" }
]
```

`Financial.Web`'s transaction entry form fetches this once (or it is embedded in an existing
lookup/bootstrap payload if one already exists — an implementation-time decision for
`/speckit-tasks`, not a new architectural surface either way) to decide whether to show
Quantity/UnitPrice as required (FR-022). `Financial.App` resolves `TransactionTypeEffects.For(...)`
directly in-process, per Constitution Principle III (WPF is not an HTTP client of `Financial.Api`).

## `AggregatedSummaryDTO` (portfolio/broker level — Wave 0 F06)

`Financial.Investment.Application/Services/SummaryService.cs`'s `AggregatedSummaryDTO`

| Field | Change |
|---|---|
| `TotalReturn` | Unchanged meaning — becomes the **Gross** return (computed the same way as today, now over the full widened vocabulary per FR-016). |
| `TotalReturnNetOfTax` | **New**, nullable `decimal` — computed via the Net cash-flow series (`research.md` #4), same null-when-incomplete rule Wave 0 established (a total missing a valuation withholds both figures, per spec Edge Cases / User Story 3 Scenario 3). |
| `PriceOnlyReturn` | **Unchanged, no Net counterpart** — it already excludes all income by construction, so withheld tax (which only arises on income and, vanishingly rarely, on a transaction) does not create a meaningful second figure here; FR-017's "both available wherever a return figure is shown" is scoped to `TotalReturn`, which is the figure User Story 3 is about. |

## Asset-level holding valuation — `PortfolioAssetSummaryItemDTO`

`Financial.Investment.Application/DTOs/PortfolioAssetSummaryItemDTO.cs` — found during
implementation to be the DTO `PortfolioAssetSummaryBuilder` maps `HoldingValuation` into (there is
no separate `HoldingValuationDTO`); this is what surfaces the per-holding grid row, including
Wave 0's `TotalReturn`, to `Financial.Api`/`Financial.Web`.

| Field | Change |
|---|---|
| `TotalReturnNetOfTax` | **New**, nullable `decimal`, alongside the existing `TotalReturn` — same Gross/Net pairing as `AggregatedSummaryDTO`, sourced from `HoldingValuation.TotalReturnNetOfTax` (`HoldingValuationService.GetValuation`). |

## Regeneration checklist (per `CLAUDE.md`)

1. `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` (bash) or the PowerShell
   env-var form, then unset it.
2. Review the snapshot diff.
3. `cd Financial.Web && npm run generate-api-types`.
4. Update `Financial.Web/src/api/types.ts` aliases only if a type name changed (existing aliases by
   name should keep working for additive fields).
5. `tsc -b` (via `npm run build`) to find every call site touching `totalPrice` (removed) or the
   widened `Type` string unions.
