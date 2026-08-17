# Backend (.NET) — Domain / Application / Infrastructure

See legend in [README.md](README.md). See [02-architecture.md](02-architecture.md) for layering rules and the dependency map.

## Folder shape (symmetric across both bounded contexts)

**CONFIRMED**:

- **Domain**: `Entities`, `ValueObjects`, `Enums`, `Rules`. No framework code, no database code, no dependencies.
- **Application**: `Interfaces`, `Services` (use-case orchestration), `DTOs`, `Validation`, `Configuration` (options classes), `DependencyInjection`.
- **Infrastructure**: `Persistence` (serializers/converters/loaders), `Repositories` (repository impl + factory + provider selection), `Services` (external adapters), `Configuration`, `DependencyInjection`.
- **`Financial.Shared.Infrastructure`** (used by both contexts' Infrastructure): `Persistence` (`IJsonStorage`, `LocalJsonStorage`, `GoogleDriveJsonStorage`, `DebouncedJsonStorage`, `JsonStorageFactory`), `Resilience` (`TransientRetryPolicy`), `Sync` (`ISyncStatusProvider`, `SyncState`/`SyncStatus`), `Hosting` (`ShutdownFlushHostedService`), `Configuration` (`RepositoryProviderResolver`).

Repository pattern per context: a JSON repository (`InvestmentJsonRepository`, `CashFlowJsonRepository`) is constructed already holding the fully-deserialized aggregate root in memory. Reads are pure in-memory LINQ; writes re-serialize and rewrite the whole document (see [07-data-persistence.md](07-data-persistence.md) for detail).

## Dependency injection

**CONFIRMED** — each layer/context exposes one `Add<Context><Layer>()` extension method. Both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` call the identical sequence independently (no shared helper):

```
AddFinancialApplication()          // Investment.Application
AddGoogleDriveFileClient()         // GoogleFinancialSupport (Investment.Infrastructure-adjacent, see 02-architecture.md)
AddFinancialInfrastructure()       // Investment.Infrastructure
AddFinancialCashFlowApplication()  // CashFlow.Application
AddFinancialCashFlowInfrastructure() // CashFlow.Infrastructure
```

Options bound via `Configure<T>()`: `WatchlistOptions`, `AssetPriceFetchOptions`, `DividendOptions` (Investment-side; documented in `README.md`).

## Logging

**CONFIRMED — Serilog at the host level only**, in both `Financial.Api` and `Financial.App` (daily rolling file sink `logs/app-.log`, 14-day retention).

**CONFIRMED — application-level logging is almost entirely absent.** Across every `.cs` file in both contexts' Domain/Application/Infrastructure projects, exactly **one** class uses `ILogger`: `Financial.CashFlow.Application/Services/CardStatementService.cs`. `Financial.Investment.Application` doesn't even reference `Microsoft.Extensions.Logging.Abstractions` (CashFlow.Application does). Serilog effectively only captures ASP.NET Core's own framework request logs plus this one service's output.

## Error handling

**CONFIRMED — one custom middleware, CashFlow-only, asymmetric between contexts.** `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs` is the single exception→HTTP translation point, registered once in the pipeline after the built-in `UseExceptionHandler()`. It maps exactly three types:

| Exception | HTTP status |
|---|---|
| `OverdraftConfirmationRequiredException` (CashFlow — Reserve bucket withdrawals only, see [10-domain-cashflow.md](10-domain-cashflow.md)) | 409 |
| `KeyNotFoundException` | 404 |
| `ArgumentException` | 400 |

**CONFIRMED — Investment has no custom domain exception types at all.** Its errors rely entirely on the two generic BCL types above happening to be thrown, or fall through to the built-in `UseExceptionHandler()`/ProblemDetails path. This is a real, current asymmetry — not a defect that's been flagged for correction.

WPF has **no centralized exception handler found** — errors surface ad hoc via `MessageBox.Show` callbacks injected per-ViewModel from the composition root (`App.xaml.cs`). **UNKNOWN** whether a global unhandled-exception handler (e.g. `DispatcherUnhandledException`) exists — not verified during discovery.

## Configuration and environment handling

**CONFIRMED** — standard ASP.NET Core / Generic Host layering: `appsettings.json` → `appsettings.{Environment}.json` → environment variables (double-underscore syntax, e.g. `Investment__DataJsonFile`). Both `Financial.Api` and `Financial.App` build independent but structurally identical bootstraps.

Each bounded context's configuration lives under its own top-level JSON element — `Investment:*` and `CashFlow:*` — never at the config root (**CONFIRMED**, `README.md`, load-bearing convention: leaving either unset no longer risks the two contexts sharing a data file, since each has a distinct default filename).

Key settings (per `README.md`):

- `Investment:DataJsonFile` / `CashFlow:DataJsonFile` — path to each context's JSON document.
- `Investment:Repository:Provider` / `CashFlow:Repository:Provider` — `LocalJson` (default) or `GoogleDrive`, selected independently per context.
- `Investment:GoogleDrive:*` / `CashFlow:GoogleDrive:*` — credentials path + file ID/path, only relevant when provider is `GoogleDrive`.
- `Cors:AllowedOrigins` — only present in `appsettings.Development.json` (Vite dev server origins). Absent elsewhere → all cross-origin requests blocked (relevant only when running Api/Web dev servers separately instead of via Docker).
- `Watchlist:Items`, `AssetPriceFetch:Portfolios`, `Dividends:DefaultExchange` — Investment-side feature configuration, user-personalized.
