# Research: Transaction and Income Event Vocabulary

All items below were open technical-design questions after the spec was finalized (no
`NEEDS CLARIFICATION` markers remain in `spec.md` itself — these are implementation-level
decisions the plan phase needs before data-model/contracts can be written).

## 1. How new transaction types plug into `Transactions.Apply` / average price / realized gain

**Decision**: Generalize the existing Buy/Sell 2-way switch in `Transactions.Apply()`
(`Financial.Investment.Domain/Entities/Transactions.cs:104`) into a 3-rule model driven by the
type's declared `QuantityEffect` (Increase/Decrease/None) and `CashEffect` (In/Out/None):

- **QuantityEffect = Increase** (Buy, Transfer In): contributes to the weighted-average price the
  same way Buy does today — `AveragePrice = (AveragePrice * Quantity + transaction.NetOfFeesCost) /
  resultingQuantity` — regardless of whether cash actually moved. Transfer In supplies its own
  recorded cost-basis price (its `UnitPrice`) for this purpose even though no cash effect exists.
- **QuantityEffect = Decrease, CashEffect ≠ None** (Sell, Redemption): realizes gain/loss exactly
  as Sell does today — `RealizedCapitalGain += proceeds - (quantity * AveragePrice)` — where
  proceeds is the type's Net Cash. Redemption is economically a full/partial exit with real cash
  proceeds, so it is treated identically to Sell for this purpose (only the transaction-list label
  differs, per FR-002).
- **QuantityEffect = Decrease, CashEffect = None** (Transfer Out): quantity decreases at the
  *existing* average price with proceeds defined as `quantity * AveragePrice` — i.e. realized
  gain/loss is always exactly zero. This is what "no change to average price beyond what the
  transfer's own recorded cost basis implies" (User Story 1, Scenario 3) requires on the sending
  side, and matches the spec's own accepted limitation that a Transfer Out/In pair is not
  automatically linked (Edge Cases): each side is cost-basis-neutral on its own, rather than one
  side manufacturing a fictitious gain or loss.
- **QuantityEffect = None** (Fee, Capital Call, Return of Capital): no interaction with
  `AveragePrice` or `RealizedCapitalGain` at all — these are pure cash entries.

**Rationale**: This is the minimal generalization of the code that already exists (`Transactions.cs`
lines 104–125) that satisfies every acceptance scenario in User Story 1 without adding a third,
spec-undeclared "cost effect" axis — the behavior falls out mechanically from the two axes FR-007
already requires each type to declare.

**Alternatives considered**: A third explicit "cost basis effect" declaration per type was
considered and rejected — it would let a future type combination land in an unhandled state,
whereas deriving cost-basis behavior from the two required axes means every valid
(QuantityEffect, CashEffect) combination has exactly one defined behavior by construction.

## 2. Net Cash formula generalization (FR-009)

**Decision**: Generalize `Transaction.TotalPrice` (today: `IsPurchase ? GrossAmount + Fees :
GrossAmount - Fees`) into a `NetCash` computed property keyed on the type's `CashEffect`:

- `CashEffect.Out` → `NetCash = -(Gross + Fees + Withheld)` (money leaving; withheld is
  vanishingly rare on a trade but the field exists uniformly per FR-009).
- `CashEffect.In` → `NetCash = Gross - Fees - Withheld`.
- `CashEffect.None` → `NetCash = -Fees` (only a fee, if any, moves cash; per the FR-003/FR-004/
  FR-007 clarification that a fee is independent of the type's own declared cash effect). `Gross`
  for these types is retained as the cost-basis/notional value used by decision #1 above, not as a
  cash amount — this is why FR-009's "MUST derive net cash from them" is read as *type-specific*
  derivation, not one universal formula; the three inputs (Gross/Fees/Withheld) are the same for
  every type, but which of them move actual cash depends on the type's own `CashEffect`.

**Rationale**: This is the direct generalization of the existing `TotalPrice`/`TransactionFeeCalculator`
pair (`Financial.Investment.Domain/Rules/TransactionFeeCalculator.cs`) to three cash directions and
a withheld term, and is what `AssetCashFlowBuilder` (decision #4) consumes for the transaction side
of the cash-flow series.

## 3. `Credit`/income-event Net calculation

**Decision**: `NetAmount = Gross - Withheld` uniformly — income events always have a cash effect
(`In`), so no per-type branching is needed here, unlike transactions.

**Rationale**: Matches FR-010 literally; income has no "no cash effect" case in this feature's
vocabulary (a return of capital, which has no income character, is modeled as a **Transaction**
type per FR-006, not as a `Credit`).

## 4. Cash-flow builder: Gross vs Net series (FR-016, FR-017)

**Decision — refined during implementation**: `AssetCashFlowBuilder`
(`Financial.Investment.Application/Services/AssetCashFlowBuilder.cs`) gains a Net-of-tax build mode
alongside the existing one, but the two series differ **only on the credit (income) side**, not on
the transaction side:

- **Transaction side (both series, unchanged)**: amount = `Transaction.NetCash` (decision #2). A
  Transaction's own `Withheld` is real but the spec and this codebase's data both treat it as
  effectively never occurring in practice (withholding happens on income, not on a buy/sell/fee) —
  splitting a second "ignore Transaction.Withheld" transaction figure out of `NetCash` would add a
  property and a formula whose two outputs are identical for every transaction that exists today,
  to satisfy a case the domain doesn't produce. If a Transaction ever *does* carry a nonzero
  `Withheld`, both series reflect it identically, which does not violate FR-017/FR-018 — those
  requirements are about a *portfolio* being able to show a differing figure when withholding
  exists anywhere, not about attributing that difference to a specific side.
- **Credit (income) side**: Gross series uses `Credit.Value`; Net series uses `Credit.NetAmount`
  (decision #3). This is the one place the two series actually diverge, since income withholding is
  where FR-017/FR-018's "tax already taken into account" scenario lives (spec.md User Story 3 is
  framed entirely around "an income payment", never a transaction).
- The existing `BuildWithCredits`/`BuildWithoutCredits`/`ConcatenateWithCredits`/`ConcatenateWithoutCredits`
  method names and bodies are therefore the **Gross** series unchanged (no edit needed beyond the
  already-landed `NetCash`/`NetAmount` widening); a new `BuildNetOfTaxWithCredits`/
  `ConcatenateNetOfTaxWithCredits` pair adds the Net series, reusing `BuildFromTransactions` and
  substituting `c.NetAmount` for `c.Value` on the credit side.
- `BuildWithoutCredits`/`ConcatenateWithoutCredits` (price-only, no income at all) has no Net
  counterpart — per contracts/api-contract.md, `PriceOnlyReturn` already excludes all income by
  construction, so a "net of tax" variant of a series with no income in it is not a meaningful
  second figure.

`XirrCalculator` itself (`Financial.Investment.Domain/Rules/XirrCalculator.cs`) is unchanged (FR-019)
— it is a pure solver over `(DateTime, decimal)` pairs; only the series `AssetCashFlowBuilder` and
`XirrCalculationService` hand it changes. The same Gross/Net split applies at portfolio and broker
level (Wave 0's existing aggregation levels), since those already concatenate per-asset series
(`AssetCashFlowBuilder.ConcatenateWithCredits`).

**Rationale**: Keeps the "solver never changes" invariant Wave 0 established, and reuses the
existing two-method (`WithCredits`/`WithoutCredits`) shape rather than introducing a new builder
class.

## 5. JSON persistence: widening `Transaction`/`Credit` without breaking existing rows

**Decision**: `InvestmentTypeInfoResolver` (`Financial.Investment.Infrastructure/Persistence/InvestmentTypeInfoResolver.cs`)
already excludes computed properties (e.g. `Transaction.TotalPrice`) from serialization via its
`ExcludedProperties` set — the new computed properties (`NetCash` on `Transaction`, `NetAmount` on
`Credit`) are added to that same set, following the existing pattern exactly. New *stored* fields
(`Gross`/`Fees` already exist on `Transaction`; `Withheld` is new on both; `Gross`/`Withheld` are new
on `Credit`) are added as ordinary properties with private setters, wired through
`ReflectionJsonTypeInfoHelpers.WirePropertySetter` the same way every existing property already is.
Both entities keep their private parameterless constructor + property-setter deserialization path
(`Transaction()`, `Credit()`), so **no custom converter or migration-time schema version is needed
for the additive fields** — a JSON row missing `Withheld` deserializes it as `decimal` default `0`,
which is exactly FR-012's required migration behavior (existing rows show `Withheld = none`, i.e.
zero) with no explicit migration step for that part.

**What *does* require a migration tool**: `Credit.Type` and `Transaction.Type` are serialized via
`JsonStringEnumConverter` (`InvestmentSerializerAdapter.cs:11`), so the data file stores the literal
string `"Rent"` today. Renaming the C# enum member to `SecuritiesLendingIncome` (FR-013) without
rewriting the 52 stored `"Rent"` strings would make every one of those rows fail to deserialize at
next startup (`JsonStringEnumConverter` throws on an unrecognized string by default) — this is a
hard requirement, not a nicety. A one-time raw-JSON string rewrite (`"Rent"` → `"SecuritiesLendingIncome"`)
must ship in the same PR as the enum rename, verified against a temp copy first.

**Rationale**: Minimizes new machinery — the additive money-block fields ride the codebase's
existing "new field defaults to CLR default on old rows" convention (no bespoke migration needed),
while the enum rename is correctly scoped as the one piece of this feature that *does* need a raw
JSON rewrite, matching roadmap decision D4.

## 6a. `AssetTotalsCalculator` generalization (pre-existing bug surfaced by widening)

**Found during task planning**: `AssetTotalsCalculator.CalculateTotals`
(`Financial.Investment.Domain/Rules/AssetTotalsCalculator.cs:11-17`) buckets every transaction with
`if (Type == Buy) totalBought += TotalPrice; else totalSold += TotalPrice;` — an `else`, not a
type check. Left unchanged, every new type (`Fee`, `TransferIn`, `CapitalCall`, ...) would silently
fall into `totalSold`, corrupting `TotalInvested`/portfolio-weight figures the moment any of them is
recorded.

**Decision**: Rewrite the switch to key off `CashEffect` (decision #1's table), not `Type == Buy`:
`CashEffect.Out` (Buy, Fee, CapitalCall) → `totalBought`; `CashEffect.In` (Sell, Redemption,
ReturnOfCapital) → `totalSold`; `CashEffect.None` (TransferIn, TransferOut) → neither bucket, since
no cash moved for this holding. This must ship in the same increment as the `Transaction` widening
(Foundational), not deferred to a later story, because the corruption risk exists the moment a new
type can be recorded at all — before any UI or Gross/Net return work exists to surface it.

## 6. Migration tool shape (decision D4)

**Decision — corrected during task planning**: `Tools/InvestmentSpreadsheetImport` is not a console
migration tool — it is a class library (no `Program.cs`, no `OutputType=Exe`) consumed by
`Tools/ImportGoogleSpreadSheets`, which is itself a **WPF** GUI app. Neither is a fit for a one-shot
raw-JSON migration. The actual Investment-context precedent for a small, standalone, one-purpose
console tool is `Tools/InvestmentDataQualityReport` (added by Wave 0's P46-F07): a top-level-statements
`Program.cs` that takes an optional data-path argument, wires `LocalJsonStorage` +
`InvestmentSerializerAdapter` + `InvestmentJsonRepository` directly (no DI container), runs one
operation, and prints a report.

This feature's migration tool follows that exact shape as a new sibling project,
`Tools/InvestmentTransactionIncomeVocabularyMigration/`:

- `Program.cs` — takes the data-file path as `args[0]` (defaulting to `data/data-investment.json`
  the same way `InvestmentDataQualityReport`'s does), backs up the file, runs the migrator, prints
  the summary, returns a non-zero exit code on failure.
- `TransactionIncomeVocabularyMigrator` — the one raw-JSON rewrite decision #5 requires (`"Rent"` →
  `"SecuritiesLendingIncome"` on every `Credit` entry across `ActiveBrokers` and `HistoricBrokers`),
  operating on the raw `JsonElement`/`Utf8JsonWriter` level (before the normal typed
  `InvestmentSerializerAdapter` load) — the same reason `CashFlowSpreadsheetImport`'s raw-JSON
  migrators do this rather than deserializing first: deserializing first would already fail on the
  very strings this migration exists to fix.
- `TransactionIncomeVocabularyMigrationSummary` — a small counter type (rewritten-row count),
  matching the counting/reporting *shape* of CashFlow's `MigrationSummaryBase` without depending on
  it (that type lives in `Financial.CashFlow.Infrastructure` — reusing it would cross the
  bounded-context boundary, per Constitution Principle II; only the shape is reused).
- Backup-before-write (copy the file to a timestamped `.backup-migration-<timestamp>.json` sibling,
  matching `MigrationBackup`'s naming convention) and the "run against a temp copy first, verify,
  then apply" discipline `CLAUDE.md` and the constitution require for every migration in this
  codebase.

**Rationale**: Matches the Investment context's own established tool-per-feature convention
(`InvestmentDataQualityReport`) rather than importing a shape from a project (`InvestmentSpreadsheetImport`)
that turned out, on inspection, to already serve two other purposes (Google Sheets import, hosted
inside a WPF GUI) it shouldn't be made to also carry a one-shot JSON migration.

**Decision — superseded after real-data verification**: the standalone `Tools/InvestmentTransactionIncomeVocabularyMigration`
console tool above was built, verified against a temp copy, and run once against the real
`data/data-investment.json` (52 rows rewritten). The user then asked for a structural change: a
one-shot tool is a dead end for every *future* stored-vocabulary rename, since each one would need
its own tool, its own manual run, and a restart timed just right. Replaced with a persistent
**document version** the serializer itself upgrades on load, so no separate tool or manual run is
ever needed again:

- `InvestmentSerializerAdapter.Serialize` writes a top-level `"Version"` integer (`InvestmentDataMigrations.CurrentVersion`,
  currently `2`) into the JSON alongside the existing `ActiveBrokers`/`HistoricBrokers` — a
  persistence-envelope concern added via `JsonNode` post-processing, not a property on the `Investments`
  domain entity (Domain must not know about persistence versioning).
- `InvestmentSerializerAdapter.Deserialize` parses the raw JSON as a `JsonNode` first, reads
  `"Version"` (a document with none — every file written before this change — is treated as `1`),
  and runs `InvestmentDataMigrations.Apply(root, storedVersion)` before the normal typed
  deserialization. `InvestmentDataMigrations` holds one step per version gap — today just "rewrite
  every `Credit.Type` `\"Rent\"` string to `\"SecuritiesLendingIncome\"` for `fromVersion < 2`", the
  exact tree-walk the removed tool used — so the next stored-vocabulary rename adds one more step and
  bumps `CurrentVersion`, never a new project.
- The already-migrated real data file has no `Version` key yet (it predates this change), so it is
  read as version 1: the rename step re-runs but is a no-op (nothing left to match), and the very
  next save stamps it `Version: 2`. No manual data-file edit or backfill is needed.
- `Tools/InvestmentTransactionIncomeVocabularyMigration/` and its test project were deleted; the
  migration coverage moved into `InvestmentSerializerAdapterTests` (missing-version, explicit
  version-1, and already-current-version cases).
- `Financial.CashFlow.Infrastructure/Persistence/CashFlowSerializerAdapter.cs` gained the same
  `"Version"` envelope (constant `1`, no migration steps yet) so the *shape* exists in both bounded
  contexts before either one actually needs a second version — cheap to add now, and CashFlow's own
  first rename won't need this same research cycle repeated.

## 7. Enum/validation plumbing for new types

**Decision**: `Transaction.TransactionType` gains `Fee, Redemption, TransferIn, TransferOut,
CapitalCall, ReturnOfCapital`; `Credit.CreditType`'s `Rent` becomes `SecuritiesLendingIncome` and a
new `Coupon` is added. No change is needed to `TransactionTypeParser`/`CreditTypeParser`/`EnumParser`
(`Financial.Investment.Application/Validation/*.cs`) — they wrap `Enum.TryParse<TEnum>(...,
ignoreCase: true, ...)` generically, so widening the enums is sufficient; the parsers pick up new
values automatically.

**Rationale**: Confirmed by reading the existing parser code — it is already type-generic with no
per-value branching, so this is a zero-touch dependency.

## 8. Technical Context resolution (no NEEDS CLARIFICATION remain)

| Field | Value |
|---|---|
| Language/Version | C# / .NET 10 (backend, WPF), TypeScript 5 / React 18 (Web) |
| Primary Dependencies | ASP.NET Core (API), System.Text.Json custom resolver (persistence), xUnit + FluentAssertions (tests), Vite + Vitest + React Testing Library + Playwright (Web) |
| Storage | Single JSON document (`data-investment.json`), `LocalJson`/`GoogleDrive` provider, loaded once at process startup (unchanged by this feature) |
| Testing | `dotnet test` (unit + `WebApplicationFactory` integration + OpenAPI contract snapshot test), `npm test` (Vitest), `npm run smoke-test` (Playwright) |
| Target Platform | Docker/Linux container (API + built SPA), Windows desktop (WPF) |
| Project Type | Web application (API + SPA) plus a WPF desktop client sharing the same Application/Domain layers in-process |
| Performance Goals | N/A — single-user tool (Constitution Principle IV); migration must run in well under a second against ~900 transactions / ~1,485 credits, consistent with every prior migration in this codebase |
| Constraints | JSON loaded once at startup (restart required after migration); full-document rewrite on every save; OpenAPI snapshot + generated TS types must be regenerated in the same PR as any DTO shape change; both front ends must ship in the same increment (Wave 0 precedent) |
| Scale/Scope | 899 existing transactions, 1,485 existing credits (illustrative counts, will have grown by implementation time — do not treat as exact); 8 transaction types total (2 existing + 6 new); 4 income kinds total (Dividend, SecuritiesLendingIncome, JCP, Coupon) |
