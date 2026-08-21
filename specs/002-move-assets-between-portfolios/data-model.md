# Phase 1 Data Model: Move Assets Between Portfolios

**Feature**: `specs/002-move-assets-between-portfolios` | **Date**: 2026-08-21

No new persisted entity is introduced. The stored shape of `data-investment.json` is unchanged —
this feature only relocates an existing `Asset` object between `Portfolio` collections, and adds or
removes `Portfolio` entries. What follows is the existing model, the members this feature adds to it,
and where each spec rule is enforced.

---

## 1. Existing graph (unchanged shape)

```
Investments                       (aggregate root, one per process, held by the repository)
├── ActiveBrokers   : Broker[]    ── "Active Investments" in the UI
└── HistoricBrokers : Broker[]    ── "Historic Investments" in the UI
        └── Broker { Name, Currency }
              └── Portfolios : Portfolio[]
                    └── Portfolio { Name }
                          └── Assets : Asset[]
                                └── Asset { Name, ISIN, Exchange, Ticker, Country,
                                            LocalTypeCode, Class,
                                            Transactions, Credits, PriceHistory }
```

**Two facts that shape the design** (verified against the live data file):

1. A broker is **two independent records**, not one record with a scope flag. "Trading 212" appears
   once in `ActiveBrokers` and again in `HistoricBrokers`, each with its own `Portfolios`.
2. **The two lists are not mirrors.** Coinbase exists only in `ActiveBrokers`. Archiving a closed
   Coinbase holding therefore requires creating its Historic counterpart — see `research.md` §D2 and
   the amendment noted in `plan.md`.

`Asset` derives `Quantity`, `AveragePrice`, `RealizedGainLoss` and `PositionType` from the
`Transactions` and `Credits` it carries (`Financial.Investment.Domain/Entities/Asset.cs`). Moving the
object therefore moves every derived figure with it and recomputes nothing — which is precisely what
FR-002 and FR-003 require, and why the move is a reference relocation rather than a copy.

---

## 2. Domain members this feature adds

### `Portfolio`

| Member | Purpose |
|---|---|
| `bool IsEmpty` | `Assets.Count == 0`. Derived, never stored — a stored flag would be a second source of truth. |
| `Asset? FindAsset(string name)` | Case-sensitive lookup by name, matching how `InvestmentJsonRepository.GetAsset` already resolves assets. |
| `internal bool RemoveAsset(string name)` | Detaches the asset. `internal` because only `Broker` — which completes the move by re-attaching it — may relocate an asset; nothing else is allowed to make one disappear. |

### `Broker`

| Member | Purpose | Enforces |
|---|---|---|
| `Portfolio? FindPortfolio(string name)` | Lookup by name. | FR-011 |
| `void MoveAsset(string sourcePortfolioName, string assetName, string destinationPortfolioName)` | The complete same-broker move: resolve both ends, detach, attach. Creates the destination portfolio when the name is new. | FR-001 – FR-006, FR-008, FR-012 – FR-015 |
| `bool RemoveEmptyPortfolio(string name)` | Deletes a portfolio; throws if it still holds assets. | FR-021, FR-022 |

`Broker.AddPortfolio(string name)` already exists with get-or-create semantics and is reused for the
destination.

### `Investments`

| Member | Purpose | Enforces |
|---|---|---|
| `Broker? FindActiveBroker(string name)` / `FindHistoricBroker(string name)` | Resolve a broker within one scope. | FR-011 |
| `void ArchiveAsset(string brokerName, string sourcePortfolioName, string assetName, string destinationPortfolioName)` | The Active → Historic crossing: requires `Quantity == 0`, creates the Historic broker counterpart if absent, then transfers. | FR-016 – FR-018, FR-020 |

`AddActiveBroker` / `AddHistoricBroker` already exist and are reused.

---

## 3. Rule enforcement map

Every rule is enforced in exactly one place: the Domain. The front ends narrow the *choices* — the
source portfolio is left out of the destination list — and validate *shape* (something selected, a
typed name non-blank), but they never re-decide whether a destination is legal. A rule restated in a
dialog is a rule that drifts from the one the server enforces, and the user would then see two
different sentences for the same refusal.

| Rule | Requirement | Enforced in | Failure |
|---|---|---|---|
| Asset / portfolio / broker exists | FR-011 | `Broker.MoveAsset`, `Investments.ArchiveAsset` | `KeyNotFoundException` → 404 |
| Destination differs from source | FR-006 | `Broker.MoveAsset` | `InvestmentRuleViolationException` → 409 |
| Same broker only | FR-007 | Application (one `BrokerName`, so both ends resolve to one `Broker`) | structurally impossible |
| Destination has no asset of that name | FR-008 | `Broker.MoveAsset` | `InvestmentRuleViolationException` → 409 |
| New portfolio name not blank | FR-012 | `Broker.MoveAsset` | `ArgumentException` → 400 |
| New portfolio name not a duplicate (case- and whitespace-insensitive) | FR-013 | `Broker.MoveAsset` | `InvestmentRuleViolationException` → 409 |
| New portfolio name trimmed | FR-014 | `Broker.MoveAsset` | — |
| Zero quantity to cross into Historic | FR-016, FR-017 | `Investments.ArchiveAsset` | *archive increment* |
| No Historic → Active | FR-019 | The request names one scope, so the crossing cannot be expressed | structurally impossible |
| Only empty portfolios delete | FR-021, FR-022 | `Broker.RemoveEmptyPortfolio` | *deletion increment* |
| Deletion is never automatic | FR-023 | Application exposes it only as its own operation | — |
| All-or-nothing | FR-009 | `IInvestmentRepository.ApplyAndSaveAsync` — the delegate throws before returning `true`, so nothing is serialized | — |

**FR-013's comparison rule**: `Broker.AddPortfolio` matches on `p.Name == name` (ordinal, exact)
today. Duplicate detection for a *new* destination name compares trimmed and case-insensitively so
that "isa" cannot join "ISA" in the tree, while lookup of an *existing* destination stays exact so it
keeps behaving as it does now.

---

## 4. State transitions

An asset is in exactly one portfolio at every observable moment. There is no "moving" state and no
partially-moved state: the detach and attach happen inside one `ApplyAndSaveAsync` delegate, and the
document is serialized only after both have succeeded.

```
Asset in (scope S, broker B, portfolio P)
  │
  ├── MoveAsset          → (S, B, P′)          any quantity; P′ may be created by the move
  ├── ArchiveAsset       → (Historic, B, P′)   only from Active, only when Quantity == 0;
  │                                            Historic B created if it does not exist
  └── any rejection      → (S, B, P) unchanged, nothing written
```

```
Portfolio: Populated ──(last asset moves out)──▶ Empty ──(user deletes)──▶ gone
                                                   │
                                                   └──(user declines)──▶ stays Empty, reusable
```

A portfolio may be created directly into the `Populated` state by a move (FR-005); it is never
created empty.

---

## 5. Transport DTOs (Application layer — these *are* the wire format)

There is no separate API contract layer, so these shapes are the public contract for `Financial.Web`
and must be mirrored in `Financial.Web/src/api/types.ts`. See `contracts/` for the HTTP surface.

### `MoveAssetRequestDTO`

| Field | Type | Notes |
|---|---|---|
| `BrokerName` | string | Required. The same broker holds both ends (FR-007). |
| `Scope` | string | `active` \| `historic`, parsed by the existing `InvestmentScopeParser`. **One field, not a source/destination pair** — a move stays within a scope, so the Active→Historic crossing is unrequestable here rather than requestable-and-refused. Archiving gets its own request shape. |
| `SourcePortfolioName` | string | Required. |
| `AssetName` | string | Required. |
| `DestinationPortfolioName` | string | Required. Existing portfolio, or the name of one to create. The request does not say which — the broker decides by looking. |

Deliberately **not** a field: any "create the portfolio" flag. FR-005 and FR-013 together mean the
name alone determines the outcome — an existing name is a move into it, a new name creates it, and a
name that differs from an existing one only by case or padding is a rejection. A caller-supplied flag
could contradict the graph and would add a failure mode with no user meaning.

### Response

The existing `AssetDetailsDTO`, read back from the destination — the same shape the neighbouring
mutation endpoints return. No wrapper type.

**Deliberately not published yet**: whether the move emptied the source portfolio. It would drive the
FR-024 deletion offer, but nothing consumes it until that increment exists, and these DTOs *are* the
wire format — a field published ahead of its reader freezes a contract for undesigned behaviour
(Constitution IV). It arrives with the code that reads it, as does
`DeleteEmptyPortfolioRequestDTO`.

---

## 6. What this feature does not touch

- **No stored field is added, removed, or renamed.** `data-investment.json` written by this feature
  is readable by the current build, and vice versa. **No migration is required** — which matters,
  because restarting the app never runs one.
- `Transaction`, `Credit`, and `AssetPriceSnapshot` are untouched: they move with their asset as
  object references, so no serialization, copying, or re-keying occurs.
- Watchlists, dividend lookups, and price fetching address assets by ticker and ISIN, not by
  portfolio, and are unaffected.
