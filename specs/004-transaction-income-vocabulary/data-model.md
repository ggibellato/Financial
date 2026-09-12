# Data Model: Transaction and Income Event Vocabulary

Source: `spec.md` Key Entities, Requirements FR-001–FR-022; design decisions in `research.md`.

## Transaction (widened)

`Financial.Investment.Domain/Entities/Transaction.cs`

| Field | Type | Change | Notes |
|---|---|---|---|
| `Id` | `Guid` | unchanged | |
| `Date` | `DateTime` | unchanged | |
| `Type` | `TransactionType` (enum) | **widened**: `Buy, Sell, Fee, Redemption, TransferIn, TransferOut, CapitalCall, ReturnOfCapital` | Existing `Buy`/`Sell` values and their JSON string form are untouched — no migration needed for `Transaction.Type`. |
| `Quantity` | `decimal` | validation relaxed | MUST be `> 0` when the type's `QuantityEffect ≠ None` (FR-008); MUST be `0` (not merely optional) when `QuantityEffect = None`, so a stray value can't silently imply a phantom position change. |
| `UnitPrice` | `decimal` | validation relaxed | Same rule as `Quantity` — required (`> 0`) only when `QuantityEffect ≠ None`; for a `None`-quantity-effect type this represents no cost-basis and MUST be `0`. |
| `Fees` | `decimal` | unchanged (floored at 0) | Now meaningful for every type, including `QuantityEffect/CashEffect = None` combinations that don't exist (Fee itself already has `CashEffect = Out`) — but always independent of the type's own principal cash effect (research.md #1). |
| `Withheld` | `decimal` | **new** | Floored at 0, same validation posture as `Fees`. Defaults to `0` on deserialization for every pre-existing row (FR-012). |
| `Gross` *(rename of existing `UnitPrice * Quantity`)* | computed | **conceptual, not stored** | Existing `GrossAmount` private property already computes this; kept as-is, just documented here as the "Gross" FR-009 refers to. |
| `NetCash` | computed, excluded from JSON | **replaces `TotalPrice`** | Per research.md #2: `Out → -(Gross+Fees+Withheld)`, `In → Gross-Fees-Withheld`, `None → -Fees`. `TotalPrice` is removed; callers (`AssetCashFlowBuilder`, `TransactionFeeCalculator`, tests) move to `NetCash` or the type-specific formula. |

**Validation rules** (Domain, `Transaction` private constructor):
- `Gross`, `Fees`, `Withheld` MUST NOT combine to a negative `NetCash` magnitude beyond what the
  type's cash direction implies (FR-011) — concretely: `Fees + Withheld` MUST NOT exceed `Gross`
  for a `CashEffect = In` type, and MUST NOT be negative for any type (already enforced by the
  existing "floored at 0" convention, extended to `Withheld`).
- Quantity/UnitPrice MUST be `> 0` iff the type's `QuantityEffect ≠ None`; MUST be exactly `0`
  otherwise (FR-008).

**Existing-row migration** (FR-012): no raw JSON change required — `Withheld` defaults to `0` via
CLR default on missing JSON property (research.md #5); `Buy`/`Sell` values are untouched.

## Credit / income event (widened)

`Financial.Investment.Domain/Entities/Credit.cs`

| Field | Type | Change | Notes |
|---|---|---|---|
| `Id` | `Guid` | unchanged | |
| `Date` | `DateTime` | unchanged | |
| `Type` | `CreditType` (enum) | **widened + one rename**: `Dividend, SecuritiesLendingIncome (was Rent), JCP, Coupon` | `Rent` → `SecuritiesLendingIncome` requires a raw-JSON string rewrite migration (research.md #5/#6) — this is the one entity change in this feature that is not purely additive. |
| `Value` | `decimal` | validation relaxed, semantics reinterpreted | Becomes the **Gross** amount. Sign: positive for an ordinary payment; **negative permitted** for a correction (FR-015) — the existing `ValidateValue` (`value <= 0` throws) is relaxed to allow negative, while continuing to reject exactly `0` (a zero-value event carries no information and stays invalid). |
| `Withheld` | `decimal` | **new** | Same magnitude/sign rule as `Value`: for a correction (negative `Value`), `Withheld` is also expressed on the same signed basis so `NetAmount = Value - Withheld` stays consistent in sign. Defaults to `0` on deserialization (FR-012). |
| `NetAmount` | computed, excluded from JSON | **new** | `Value - Withheld`, per research.md #3. |

**Validation rules**:
- `Value ≠ 0` (relaxed from `Value <= 0` invalid to `Value == 0` invalid) — both positive (payment)
  and negative (correction, FR-015) are valid.
- `Withheld` MUST NOT make `NetAmount` invert sign relative to `Value` (FR-011's "negative net"
  refusal, applied here as: `|Withheld| ≤ |Value|` and `Withheld` shares `Value`'s sign or is zero).
- A correction MUST use the same `Type` as the payment it corrects (FR-015) — this is a usage
  convention enforced by the UI/Application layer (both front ends must let the user pick the same
  kind and enter a negative value), not a new Domain invariant tying two records together; the
  Domain has no concept of "corrects record X" per the spec's own scope (Edge Cases: no linkage is
  modeled).

**Existing-row migration** (FR-012 + FR-013):
1. `Withheld` defaults to `0` via CLR default (no raw JSON change needed).
2. Every stored `"Rent"` string MUST be rewritten to `"SecuritiesLendingIncome"` (raw JSON string
   rewrite, `TransactionIncomeVocabularyMigrator` per research.md #6) — required for deserialization
   to succeed at all after the enum rename ships, not merely for display correctness.

## Transaction Type Effect Declaration (new, Domain, data not code — FR-007)

A small static lookup, analogous in spirit to `GlobalAssetClassMapping`
(`Financial.Investment.Domain/Rules/GlobalAssetClassMapping.cs`) but far smaller:

```csharp
public enum QuantityEffect { Increase, Decrease, None }
public enum CashEffect { In, Out, None }

public sealed record TransactionTypeEffect(QuantityEffect Quantity, CashEffect Cash);

public static class TransactionTypeEffects
{
    public static TransactionTypeEffect For(Transaction.TransactionType type) => type switch
    {
        Transaction.TransactionType.Buy => new(QuantityEffect.Increase, CashEffect.Out),
        Transaction.TransactionType.Sell => new(QuantityEffect.Decrease, CashEffect.In),
        Transaction.TransactionType.Fee => new(QuantityEffect.None, CashEffect.Out),
        Transaction.TransactionType.Redemption => new(QuantityEffect.Decrease, CashEffect.In),
        Transaction.TransactionType.TransferIn => new(QuantityEffect.Increase, CashEffect.None),
        Transaction.TransactionType.TransferOut => new(QuantityEffect.Decrease, CashEffect.None),
        Transaction.TransactionType.CapitalCall => new(QuantityEffect.None, CashEffect.Out),
        Transaction.TransactionType.ReturnOfCapital => new(QuantityEffect.None, CashEffect.In),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
```

This single table is consumed by:
- `Transaction` construction (validate Quantity/UnitPrice per `QuantityEffect`, compute `NetCash`
  per `CashEffect`).
- `Transactions.Apply`/`Recompute` (average price / realized gain per research.md #1).
- `SaleCoverageRule` (extended to treat any `QuantityEffect.Decrease` type as a "sale" for coverage
  purposes, not just `Sell` — covers FR-004's Redemption/Transfer Out oversell parity).
- Both front ends' entry forms (FR-008/FR-022: hide Quantity/UnitPrice when `QuantityEffect =
  None`), surfaced through the API so `Financial.Web` doesn't hand-duplicate the table (see
  `contracts/`).

A later type (e.g. a Wave 7 corporate action) extends this same switch rather than touching every
consumer — this is the FR-007 "as data, not scattered per-type code branches" requirement realized
concretely (as concretely as a C# `switch` expression can be "data" — the point is *one* place to
extend, not *N* call sites).

## Relationships (unchanged)

`Asset` 1—* `Transaction`, `Asset` 1—* `Credit` (today's shape, unaffected — this feature widens the
two leaf entities, not the aggregate structure). `Asset.RecordTransaction`/`ReviseTransaction`/
`RetractTransaction` continue to be the only mutation entry points, still enforcing
`SaleCoverageRule` (extended per above) before applying.

## State / lifecycle

No new lifecycle — both entities remain create/update/delete records with no status field, matching
today's `Transaction`/`Credit`. A correction (FR-015) is a new *record*, not a state transition on
an existing one.
