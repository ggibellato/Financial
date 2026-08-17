# Architecture

See legend in [README.md](README.md).

## Layering (Clean Architecture)

**CONFIRMED, verified empirically and partially mechanically enforced.** Each bounded context is split Domain → Application → Infrastructure, with strict inward dependency:

- Domain has zero `ProjectReference`/`PackageReference` beyond the bare SDK, and no framework/infra `using` statements anywhere (verified by a full sweep, not just by convention).
- Application depends only on its own Domain.
- Infrastructure depends on its own Application + Domain + `Financial.Shared.Infrastructure`.

`Financial.Architecture.Tests` mechanically enforces the **forbidden inward edges** (Domain↛Application, Domain↛Infrastructure, Application↛Infrastructure) per bounded context, via plain reflection over `Assembly.GetReferencedAssemblies()` — not a library like NetArchTest. **It does not check Presentation-layer boundaries** — nothing mechanically stops `Financial.App` or `Financial.Api` from reaching into the wrong place.

## Project dependency table

**CONFIRMED**, from `.csproj` files:

| Project | References |
|---|---|
| `Financial.Investment.Domain` / `Financial.CashFlow.Domain` | none |
| `Financial.Investment.Application` / `Financial.CashFlow.Application` | own `*.Domain` |
| `Financial.Investment.Infrastructure` / `Financial.CashFlow.Infrastructure` | own `*.Application`, `*.Domain`, `Financial.Shared.Infrastructure` |
| `Financial.Shared.Infrastructure` | none (standalone) |
| `Integrations/WebPageParser` | `Financial.Investment.Domain` only |
| `Integrations/GoogleFinancialSupport` | `Financial.Investment.Application`, `.Domain`, **`.Infrastructure`**, `WebPageParser` |
| `Financial.Api` | both contexts' `.Application` + `.Infrastructure`, `GoogleFinancialSupport` |
| `Financial.App` (WPF) | both contexts' `.Application` + `.Infrastructure`, `GoogleFinancialSupport` |
| `Financial.Web` | none (separate npm project; talks to `Financial.Api` over HTTP only) |

Each layer registers itself into DI via an `Add<Context><Layer>()` extension method (e.g. `AddFinancialCashFlowApplication`, `AddFinancialInfrastructure`), called explicitly from both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` — **CONFIRMED**, and in the same order in both: Investment DI is always registered before CashFlow DI.

## The one confirmed layering inversion

**CONFIRMED** — `GoogleFinancialSupport.csproj` references `Financial.Investment.Infrastructure` directly (an "Integrations" project depending on Infrastructure, not the reverse). Its own `RootNamespace`/`AssemblyName` is `Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport` — it self-identifies as part of Infrastructure but ships as a separate assembly, wired directly into both Presentation projects via a separate `AddGoogleDriveFileClient()` call, bypassing Investment.Infrastructure's own `AddFinancialInfrastructure()` DI surface entirely.

## Two independent composition roots

**CONFIRMED — architecturally significant.** `Financial.Api` and `Financial.App` are two separate, independent composition roots that each host **the same** Application/Infrastructure code for both bounded contexts in-process. They are not client/server of each other:

- `Financial.Api` exposes it over REST, consumed by `Financial.Web`.
- `Financial.App` (WPF) references both contexts' Application/Infrastructure projects directly and has no `HttpClient`/API-base-URL wiring — it is not an HTTP client of `Financial.Api`. See [04-wpf-app.md](04-wpf-app.md).

Both independently duplicate the identical five-call DI bootstrap sequence (`AddFinancialApplication()` → `AddGoogleDriveFileClient()` → `AddFinancialInfrastructure()` → `AddFinancialCashFlowApplication()` → `AddFinancialCashFlowInfrastructure()`) plus the same `Configure<T>()` options bindings — **CONFIRMED**, no shared composition-root helper exists between them.

**Consequence — OBSERVED, no mitigation exists:** if both processes run concurrently against the same JSON file / Google Drive target, there is no synchronization mechanism anywhere in the codebase. The single-writer-per-process assumption in [07-data-persistence.md](07-data-persistence.md) does not account for two different *processes* writing to the same file/target.

## Cross-cutting concerns (summary — see [03-backend-dotnet.md](03-backend-dotnet.md) for detail)

- **Logging** — Serilog configured at the host level in both `Financial.Api` and `Financial.App` (daily rolling file, 14-day retention). **OBSERVED — coverage is minimal**: across every Application/Infrastructure/Domain file in both contexts, exactly one class (`CashFlow.Application/Services/CardStatementService.cs`) actually uses `ILogger`.
- **Error handling** — one custom middleware (`DomainExceptionMappingMiddleware`) in `Financial.Api`, asymmetric between contexts (CashFlow has custom domain exceptions mapped to specific HTTP statuses; Investment has none, relies on generic BCL exceptions). WPF has no centralized handler; errors surface via per-ViewModel `MessageBox.Show` callbacks.
- **Configuration** — standard ASP.NET Core/Generic Host layering (`appsettings.json` → `appsettings.{Environment}.json` → env vars), fully separate `Investment:*`/`CashFlow:*` config sections per context.
- **Authentication** — none, anywhere (see [01-system-overview.md](01-system-overview.md)).

## Dependency map

```
                         Financial.Web (React/TS, separate npm project)
                                    │  HTTP only (/api/v1/financial, relative)
                                    ▼
┌───────────────────────────────────────────────────────────────────────┐
│ Financial.Api  (ASP.NET Core Controllers, composition root #1)        │
│   - DomainExceptionMappingMiddleware (CashFlow exceptions only)       │
│   - No auth. CORS = only access boundary.                             │
└───────────────────────────────────────────────────────────────────────┘
                                    │  in-process references
        ┌───────────────────────────┴────────────────────────────┐
        ▼                                                         ▼
┌─────────────────────────────┐                     ┌─────────────────────────────┐
│ Investment.Infrastructure    │                     │ CashFlow.Infrastructure      │
│  Repos, Persistence adapters │                     │  Repos, Persistence adapters │
│  Price-fetch services (many) │◄──inversion──┐      │  FrankfurterExchangeRateProv.│
└──────────────┬───────────────┘              │      └──────────────┬───────────────┘
               │                    GoogleFinancialSupport            │
               ▼                    (self-named ".Infrastructure     ▼
┌─────────────────────────────┐     .Integrations", refs Investment.Infra
│ Investment.Application       │     directly — bypasses its DI surface)
└──────────────┬───────────────┘                                    ┌─────────────────────────────┐
               ▼                                                    │ CashFlow.Application         │
┌─────────────────────────────┐                                    └──────────────┬───────────────┘
│ Investment.Domain (no deps)  │                                                   ▼
└───────────────────────────────┘                                    ┌─────────────────────────────┐
                                                                       │ CashFlow.Domain (no deps)    │
                                                                       └───────────────────────────────┘

        Both Infrastructure projects → Financial.Shared.Infrastructure
        (IJsonStorage/LocalJsonStorage/GoogleDriveJsonStorage/DebouncedJsonStorage,
         TransientRetryPolicy, ISyncStatusProvider — status-reporting only, no conflict resolution)

┌───────────────────────────────────────────────────────────────────────┐
│ Financial.App (WPF, composition root #2 — INDEPENDENT of Financial.Api)│
│   References BOTH contexts' Application+Infrastructure directly,       │
│   plus GoogleFinancialSupport. NOT an HTTP client of Financial.Api.    │
│   Own in-process Infrastructure instance = own file/Drive I/O.         │
│   No synchronization with Financial.Api if run concurrently.           │
└───────────────────────────────────────────────────────────────────────┘

Integrations/WebPageParser → Investment.Domain only (consumed by Investment.Infrastructure's GoogleFinanceService)

Tools/CashFlowSpreadsheetImport, Tools/ImportGoogleSpreadSheets — standalone one-off import utilities,
not part of the runtime dependency graph.
```

## Known architectural inconsistencies (descriptive, not a to-do list)

- Two independent, unsynchronized composition roots writing to the same data.
- `GoogleFinancialSupport` layering inversion (above).
- No API-owned DTO/contract layer — Application DTOs are the literal wire format (see [06-api.md](06-api.md)).
- Exception-handling asymmetry between contexts.
- Full-document rewrite on every persistence save, regardless of mutation size (see [07-data-persistence.md](07-data-persistence.md)).
- Architecture-Tests enforce only inward Domain/Application/Infrastructure edges, not Presentation boundaries.
- `/api/v1/` is a hardcoded string constant, not a real versioning mechanism.
