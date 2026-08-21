# Contract: REST API

**Feature**: `specs/002-move-assets-between-portfolios` | **Date**: 2026-08-21

Base path: `/api/v1/financial` (built in `Financial.Api/Program.cs` as
`$"/api/{apiVersionGroupName}/{ApiRouteSegment}"`).

Application DTOs are the literal wire format — there is no separate contract layer — so every shape
here is also the C# DTO and must be mirrored in `Financial.Web/src/api/types.ts`.

---

## `POST /assets/move`

Moves one asset into another portfolio of the same broker, within one investment scope. Archiving
from Active into Historic is a separate operation, introduced by its own increment.

**Request**

```json
{
  "brokerName": "Trading 212",
  "scope": "active",
  "sourcePortfolioName": "ETF",
  "assetName": "VUSA",
  "destinationPortfolioName": "ETF ISA"
}
```

`destinationPortfolioName` names an existing portfolio *or* one to be created — the server decides by
looking at the broker. There is no "create" flag (see `data-model.md` §5).

`scope` is parsed by the existing `InvestmentScopeParser`: `active` | `historic`. **One scope field,
not two.** A move stays within a scope; crossing from Active into Historic is archiving, a separate
operation with its own rule about the position being closed first. Naming a single scope makes the
crossing *unrequestable* rather than requestable-and-refused, which is a stronger reading of FR-019
than a validation rule. The archive increment introduces its own request shape.

**Responses**

| Status | Body | When |
|---|---|---|
| `200 OK` | `AssetDetailsDTO` | Moved and persisted; the asset read back from its new portfolio |
| `400 Bad Request` | `ProblemDetails` | Body missing, or a required field / new portfolio name is blank (FR-012) |
| `404 Not Found` | `ProblemDetails` | Broker, source portfolio, asset, or named existing destination does not exist (FR-011) |
| `409 Conflict` | `ProblemDetails` | A move rule refused it — see the table below |

`409` cases, each with `detail` carrying the plain-language reason required by FR-041:

| Reason | Requirement |
|---|---|
| Destination is the portfolio the asset is already in | FR-006 |
| Destination portfolio already holds an asset with that name | FR-008 |
| New portfolio name duplicates an existing one (ignoring case and padding) | FR-013 |

**200 response**

The existing `AssetDetailsDTO`, unchanged — the same shape the neighbouring mutation endpoints
return, read back from the destination portfolio.

**Deferred to the empty-portfolio-deletion increment**: whether the move emptied the source. It is
not reported here because nothing consumes it yet, and this DTO *is* the wire format — publishing a
field ahead of its reader would freeze a contract for behaviour not yet designed. When it arrives,
the deletion must still be a **separate call**, never a nested one, because `ApplyAndSaveAsync` is
not reentrant (`research.md` §D4).

---

## `DELETE /portfolios/{brokerName}/{portfolioName}`

Deletes an empty portfolio. Used both for the post-move offer (FR-024) and standalone (FR-025) —
one endpoint serves both, because they differ only in when the user is asked.

**Query**: `?scope=active` | `?scope=historic` (defaults to `active`, matching
`AssetsController` and `NavigationController`).

**Responses**

| Status | Body | When |
|---|---|---|
| `204 No Content` | — | Deleted and persisted |
| `404 Not Found` | `ProblemDetails` | Broker or portfolio does not exist |
| `409 Conflict` | `ProblemDetails` | The portfolio still holds at least one asset (FR-022) |

---

## Error body

The existing `DomainExceptionMappingMiddleware` writes RFC 7807 `ProblemDetails` with the reason in
`detail`, and deliberately keeps that message out of the logs.

```json
{ "status": 409, "detail": "Portfolio \"ETF ISA\" already holds an asset named \"VUSA\"." }
```

**Middleware**: `InvestmentRuleViolationException` (Domain) maps to `409`
(`Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs`). Deliberately its own type rather
than `InvalidOperationException`, which `research.md` §D5 originally proposed: Infrastructure already
throws that for genuine upstream faults — an unreadable Yahoo Finance response, a missing price
fetcher — and mapping it here would relabel real defects as client conflicts.

---

## Unchanged endpoints this feature relies on

| Endpoint | Used for |
|---|---|
| `GET /navigation/tree?scope=` | The re-fetch after a move (FR-030), and the destination list the dialogs derive |
| `GET /navigation/brokers?scope=historic` | Destination list for the archive dialog (FR-018) |
| `GET /assets/{brokerName}/{portfolioName}/{assetName}?scope=` | Verifying the asset at its new location |

---

## Web client additions

`Financial.Web/src/api/financialApiClient.ts`:

```ts
moveAsset: (request: MoveAssetRequestDto) => Promise<AssetDetailsDto>
// deleteEmptyPortfolio arrives with the empty-portfolio-deletion increment
```

`ApiError` already carries the status and the `ProblemDetails` detail, so the rejection reason
reaches the UI without new plumbing.

---

## WPF consumption

`Financial.App` calls the Application service **in-process** — it is not an HTTP client
(Constitution III). It therefore consumes `IAssetMoveService` directly and catches the same
exception types listed above. This is what makes FR-040's "same wording in both front ends" hold
without duplicating any message: the text originates in the Domain, once.
