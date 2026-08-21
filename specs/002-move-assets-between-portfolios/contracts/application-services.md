# Contract: Application & Domain Surface

**Feature**: `specs/002-move-assets-between-portfolios` | **Date**: 2026-08-21

The in-process surface, consumed directly by `Financial.App` (WPF) and via controllers by
`Financial.Api`. Signatures are the contract; bodies belong to implementation.

---

## Application — `IAssetMoveService`

`Financial.Investment.Application/Interfaces/IAssetMoveService.cs`

```csharp
public interface IAssetMoveService
{
    /// <summary>Moves an asset into another portfolio of the same broker, existing or created by
    /// the move, and returns it read back from its new location.</summary>
    Task<AssetDetailsDTO> MoveAssetAsync(MoveAssetRequestDTO request);
}
```

`DeleteEmptyPortfolioAsync` is **not** declared here yet. It arrives with the deletion increment,
alongside the code that calls it — declaring it now would mean shipping a member that throws
`NotImplementedException`, which is the scaffolding Constitution VII forbids.

**Why these throw rather than return null.** The rest of the Investment application layer returns
`AssetDetailsDTO?` and lets `OkOrBadRequest` collapse null into a 400. That convention cannot satisfy
FR-041 — the user must be told *which* rule blocked the move, and `Financial.App` must show the same
sentence as `Financial.Web`. A thrown exception carries the reason to both front ends from a single
source (`research.md` §D5). This is a deliberate, local departure from the neighbouring services, not
an oversight.

**Why deletion is a separate method, not a flag on the move.** FR-023 makes deletion the user's
choice, and `ApplyAndSaveAsync` is not reentrant, so the two must be two calls
(`research.md` §D4).

**Orchestration contract** — `MoveAssetAsync` must:

1. Parse the single `Scope`; a request naming one scope cannot express a cross-scope move at all.
2. Resolve the broker within `Scope`; `KeyNotFoundException` if absent.
3. Run `broker.MoveAsset(source, asset, destination)` **inside** `repository.ApplyAndSaveAsync(...)`.
4. Return the moved asset via the existing `INavigationService.GetAssetDetails(..., scope)`, read
   back from the portfolio it actually landed in (the domain trims the destination name).
5. Follow the mandatory span convention: `StartServiceSpan` / `MarkSuccess` / `MarkFailed`, with no
   logging in the rethrow — domain messages embed holdings the user owns.

Validation happens inside the delegate, never before it — checking first and mutating second lets a
concurrent write invalidate the check.

---

## Application — `IInvestmentRepository` addition

`Financial.Investment.Application/Interfaces/IInvestmentRepository.cs`

```csharp
/// <summary>The aggregate root. Needed because archiving may have to add a broker to the
/// Historic collection, which no scoped query can do.</summary>
Investments GetInvestments();
```

**Not added by the move increment** — `GetBrokerList(scope)` already resolves the broker a move
needs. It arrives with the archive increment, which is the first thing that cannot be done without
it. Justified in `plan.md` §Complexity Tracking.

---

## Domain — `Portfolio`

```csharp
public bool IsEmpty { get; }                        // derived; never stored
public Asset? FindAsset(string name);
internal bool RemoveAsset(string name);             // internal: only Broker relocates assets
```

## Domain — `Broker`

```csharp
public Portfolio? FindPortfolio(string name);

/// <summary>Moves an asset between two of this broker's portfolios. Creates the destination when
/// the name is new.</summary>
/// <exception cref="KeyNotFoundException">Source portfolio or asset does not exist.</exception>
/// <exception cref="ArgumentException">Destination name is blank or whitespace.</exception>
/// <exception cref="InvestmentRuleViolationException">Destination is the source; destination already
/// holds an asset with that name; or the new name duplicates an existing portfolio ignoring case and
/// padding.</exception>
public void MoveAsset(string sourcePortfolioName, string assetName, string destinationPortfolioName);

/// <exception cref="InvestmentRuleViolationException">The portfolio still holds assets.</exception>
public bool RemoveEmptyPortfolio(string name);   // deletion increment
```

## Domain — `Investments`

```csharp
public Broker? FindActiveBroker(string name);
public Broker? FindHistoricBroker(string name);

/// <summary>Moves a fully closed asset from Active into Historic under the same broker, creating
/// that broker's Historic record if it does not exist yet.</summary>
/// <exception cref="KeyNotFoundException">Active broker, portfolio, or asset does not exist.</exception>
/// <exception cref="InvestmentRuleViolationException">Quantity is not exactly zero.</exception>
public void ArchiveAsset(string brokerName, string sourcePortfolioName, string assetName,
                         string destinationPortfolioName);
```

`ArchiveAsset` copies `Name` and `Currency` from the Active broker when creating the Historic record.
See `research.md` §D2 for why this is necessary (Coinbase has no Historic counterpart today) and
`plan.md` §Spec Amendments for the spec change it requires.

---

## Presentation contracts

### `Financial.Api`

- `AssetsController` gains `POST /assets/move`.
- `PortfoliosController` arrives with the deletion increment.
- `DomainExceptionMappingMiddleware` gains `InvestmentRuleViolationException → 409`.

### `Financial.App` (WPF)

- `MoveAssetDialog` — destination is either an existing portfolio or a typed new name, following the
  existing `TransactionDialog` / `CreditDialog` / `PriceDialog` pattern. It validates **shape only**;
  whether a destination is legal is the domain's to decide, and its refusal is what the user sees.
- `MainNavigationViewModelBase` gains the move command plus the post-move reload and reselection
  (FR-037); the delete command arrives with the deletion increment. Everything after the dialog runs
  inside one catch — the command is invoked as `async void`, so an escaping exception ends the
  process, and a storage fault is likelier than a domain refusal on a Drive-backed install.
- `TreeNodeViewModel` gains `bool IsDropTarget` and the "can this node accept this asset?" predicate.
  This must live on the view model, not on the container: the `TreeView` uses
  `VirtualizingPanel.VirtualizationMode="Recycling"`, so a recycled `TreeViewItem` would otherwise
  carry another node's highlight.
- A drag attached-behaviour supplies only the pointer plumbing (`research.md` §D7).

### `Financial.Web`

- `financialApiClient` gains `moveAsset`; `deleteEmptyPortfolio` arrives with the deletion increment.
- `InvestmentTree` gains the drag source on asset rows and drop handlers on portfolio and broker
  rows; `onDragOver` calls `preventDefault()` **only** for a valid target, which is how FR-034's
  refusal is both implemented and asserted.
- `SelectedNodeContext` exposes `reload()` so the tree re-fetches after a move (FR-030).
- A move dialog component covers the non-drag route (FR-038). It validates shape only, for the same
  reason the WPF one does.
