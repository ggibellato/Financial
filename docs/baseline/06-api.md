# API (Financial.Api)

See legend in [README.md](README.md).

## Style

**CONFIRMED — Controller-based MVC**, not Minimal APIs (`AddControllers`/`MapControllers`). RESTful, resource-oriented (`/expenses`, `/assets/{broker}/{portfolio}/{asset}`, etc.), one controller per aggregate/use-case group. 28 controllers total, each importing exclusively either Investment or CashFlow Application types — no controller spans both contexts.

Base path `/api/v1/financial` is applied via `app.MapGroup(ApiRoutePrefix).MapControllers()` at the pipeline level (`Program.cs`), not per-controller `[Route]` attributes — individual controllers use relative routes (e.g. `[Route("expenses")]`) that get prefixed at the group level.

**INFERRED — versioning is cosmetic.** `ApiRoutePrefix` is a hardcoded string constant, not a real ASP.NET versioning scheme (no `Asp.Versioning` package, no v2 anywhere).

## Composition order

**CONFIRMED** — Investment DI is registered before CashFlow DI, consistently, matching `Financial.App`'s bootstrap order (see [03-backend-dotnet.md](03-backend-dotnet.md)).

## Controllers, grouped by bounded context

**CONFIRMED**, by `using` statement inspection (each imports exclusively one context's Application namespace):

**Investment:** `AssetsController`, `AssetPricesController`, `AssetPriceFetchController`, `DividendsController`, `NavigationController`, `SummaryController`, `WatchlistController`, `XirrController`, `TransactionsController`, `AnnualSummaryController` *(NB: despite the name, this one is CashFlow — see below)*, `CreditsController`.

**CashFlow:** `ExpensesController`, `IncomesController`, `IncomeSourcesController`, `BanksController`, `TransfersController`, `ReserveController`, `ReserveBucketsController`, `MensaisController`, `InvestmentSnapshotsController`, `InvestmentAccountsController`, `CreditCardsController`, `CardStatementsController`, `ControleMaeController`, `CategoriesController`, `TitheController`.

**Cross-cutting/infra:** `DiagnosticsController` (health + dev-only repo config), `SyncStatusController`.

> **Naming note (CONFIRMED):** `AnnualSummaryController`, `InvestmentSnapshotsController`, `InvestmentAccountsController`, and `TitheController` all read as Investment-domain features by name, but are **entirely CashFlow-owned** (verified by `using` statements). `InvestmentSnapshotsController`/`InvestmentAccountsController` cover CashFlow's "quick-access investments" concept (e.g. emergency funds), explicitly distinct from the Investment bounded context's own asset/price tracking — see [10-domain-cashflow.md](10-domain-cashflow.md). Anyone extending the API surface should route by bounded context ownership, not by controller name.

## DTOs — no API-owned contract layer

**CONFIRMED.** `Financial.Api/DTOs` contains only 4 types, all infrastructure/diagnostic concerns: `HealthStatusDTO`, `RepositoryConfigDTO`, `SyncStatusDTO`, `SyncStatusResponseDTO`. Every domain-facing controller returns **Application-layer DTOs directly** (`Financial.CashFlow.Application.DTOs.ExpenseDTO`, `Financial.Investment.Application.DTOs.AssetDetailsDTO`, etc.) — there is no separate API contract/mapping layer. Consequence: any Application DTO shape change is automatically a wire-format/breaking-API change, in both contexts equally.

## Middleware

**CONFIRMED — one custom middleware.** `DomainExceptionMappingMiddleware` — see [03-backend-dotnet.md](03-backend-dotnet.md) for the exact exception→status mapping and the confirmed asymmetry (CashFlow-only exception types are mapped; Investment has none). Built-in `UseExceptionHandler()`/`AddProblemDetails()` is the fallback for everything else outside Development.

## Health / diagnostics

**CONFIRMED.** `GET /health` → static `{Status: "ok"}`, always 200 — **liveness only**, no actual check of JSON-file readability or Google Drive reachability. `GET /config/repository` — Development-only (404 outside Development) — exposes **Investment** repository provider/paths only; CashFlow's equivalent settings are not exposed by this endpoint (**OBSERVED** asymmetry).

## Authentication / authorization

**CONFIRMED — absent.** Zero `[Authorize]` attributes, no `AddAuthentication()`/`AddAuthorization()` calls anywhere in `Program.cs`. CORS origin allowlisting (`Cors:AllowedOrigins`, Development-only in config) is the sole access boundary.

## Serialization

**CONFIRMED — default System.Text.Json behavior via ASP.NET Core Controllers.** No explicit `JsonSerializerOptions`/naming-policy/enum-converter configuration found in `Program.cs`. **UNKNOWN** whether individual DTOs opt into per-property converters (e.g. string enums) — not exhaustively checked.

## Consumers

**CONFIRMED** — `Financial.Web` (relative `/api/v1/financial` fetch calls; CORS-allowlisted dev origins are `localhost:5173`/`5174`). **CONFIRMED not a consumer** — `Financial.App` (WPF); it hosts the same Application/Infrastructure code in-process instead (see [04-wpf-app.md](04-wpf-app.md)).
