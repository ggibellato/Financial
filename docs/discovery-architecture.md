# Architecture Discovery — Current State

Read-only architecture discovery pass over the existing (brownfield) codebase, produced as the second step of introducing Spec-Driven Development (SDD). Describes **only** the architecture that currently exists — not an ideal architecture, and not a set of refactoring recommendations.

Classifications used throughout:

- **CONFIRMED** — direct repository evidence (file path/detail cited).
- **INFERRED** — a reasonable interpretation, not explicit.
- **UNKNOWN** — insufficient evidence.

## 1–4. C#/.NET architecture, project dependencies, dependency direction, layer boundaries

**Dependency direction — CONFIRMED, holds strictly for the core Clean Architecture chain.** Verified empirically (not just by convention): both Domain projects (`Financial.Investment.Domain`, `Financial.CashFlow.Domain`) have zero `ProjectReference`/`PackageReference` beyond the bare SDK, and a full `using`-statement sweep shows no framework/infra leakage. Application → Domain only. Infrastructure → Application + Domain + `Financial.Shared.Infrastructure`. This is also **mechanically enforced** by `Financial.Architecture.Tests` via plain reflection (`Assembly.GetReferencedAssemblies()`), not a library like NetArchTest — but it only checks the *forbidden* inward edges (Domain↛Application, Domain↛Infrastructure, Application↛Infrastructure) per context; it does not check Presentation-layer boundaries at all.

**Project dependency table (CONFIRMED from .csproj files):**

| Project | References |
|---|---|
| `*.Domain` (both contexts) | none |
| `*.Application` (both contexts) | own `*.Domain` |
| `*.Infrastructure` (both contexts) | own `*.Application`, `*.Domain`, `Financial.Shared.Infrastructure` |
| `Financial.Shared.Infrastructure` | none (standalone) |
| `Integrations/WebPageParser` | `Financial.Investment.Domain` only |
| `Integrations/GoogleFinancialSupport` | `Financial.Investment.Application`, `.Domain`, **`.Infrastructure`**, `WebPageParser` |
| `Financial.Api` | both contexts' `.Application` + `.Infrastructure`, `GoogleFinancialSupport` |
| `Financial.App` (WPF) | both contexts' `.Application` + `.Infrastructure`, `GoogleFinancialSupport` |
| `Financial.Web` | none (separate npm project; talks to `Financial.Api` over HTTP only) |

**CONFIRMED — one layering inversion**: `GoogleFinancialSupport.csproj` references `Financial.Investment.Infrastructure` directly (an "Integrations" project depending on Infrastructure, not the reverse) and its own `RootNamespace`/`AssemblyName` is literally `Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport` — it self-identifies as part of Infrastructure but ships as a separate assembly wired directly into both Presentation projects, bypassing Investment.Infrastructure's own DI surface (`AddFinancialInfrastructure()`). Registered separately via `AddGoogleDriveFileClient()` in both `Program.cs` and `App.xaml.cs`.

**Boundary contents (CONFIRMED, symmetric folder shape both contexts):**

- Domain: `Entities`, `ValueObjects`, `Enums`, `Rules`.
- Application: `Interfaces`, `Services`, `DTOs`, `Validation`, `Configuration`, `DependencyInjection`.
- Infrastructure: `Persistence`, `Repositories`, `Services` (external adapters), `Configuration`, `DependencyInjection`.
- `Financial.Shared.Infrastructure`: cross-context primitives only — `Persistence` (`IJsonStorage`, `LocalJsonStorage`, `GoogleDriveJsonStorage`, `DebouncedJsonStorage`, `JsonStorageFactory`), `Resilience` (`TransientRetryPolicy`), `Sync` (status reporting only — see §12–13), `Hosting`, `Configuration`.

**CONFIRMED — DI composition is duplicated, not shared.** Both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` independently call the identical five-call sequence (`AddFinancialApplication()` → `AddGoogleDriveFileClient()` → `AddFinancialInfrastructure()` → `AddFinancialCashFlowApplication()` → `AddFinancialCashFlowInfrastructure()`) plus the same `Configure<T>()` options bindings, with no shared composition-root helper between the two Presentation entry points.

## 5–6. WPF architecture, Views/ViewModels

**Pattern — CONFIRMED — MVVM via .NET Generic Host, view-first/pre-instantiated navigation, hand-rolled commanding (no MVVM toolkit).** `App.xaml.cs` builds an `IHost`, registers all Views/ViewModels as `Transient` (one exception: `SyncStatusViewModel` as `Singleton`), resolves `MainWindow` from the container. `MainWindow.xaml.cs` builds a `Dictionary<string, object>` mapping nav keys to **already-constructed** View instances; `MainShellViewModel` swaps `SelectedContent` by dictionary lookup — not a lazy `DataTemplate`-resolved View-per-ViewModel pattern.

**CONFIRMED — Financial.App is a full client for both bounded contexts.** `Views/CashFlow/` (26 views) and `ViewModels/CashFlow/` (27 files) implement Monthly/Reserva/Mensais/Controle Mãe/Investment Snapshots/Annual Summary in full, alongside the Investment-side UI. `Navigation/NavTree.cs` has two `NavCategory`s ("Investments", "CashFlow") structurally mirroring `Financial.Web/src/navigation/navTree.ts` (same 10 leaf routes; a code comment explicitly notes the icon geometry is kept in sync with `Sidebar.tsx`).

**CONFIRMED — critical architectural fact: WPF does not call the REST API.** `Financial.App.csproj` references both contexts' `Application`/`Infrastructure` projects (and `Investment.Domain`, `GoogleFinancialSupport`) directly — no `HttpClient`/API-base-URL wiring exists in `App.xaml.cs`. Financial.App is a **second, independent in-process composition root** over the same Domain/Application code, with its own Infrastructure instance (own file/GoogleDrive I/O). It is architecturally asymmetric to Financial.Web, which only reaches the backend over HTTP. No synchronization mechanism is visible between the two processes if both run against the same JSON file/Drive target concurrently.

**Commanding/validation — CONFIRMED.** Custom `RelayCommand`/`RelayCommand<T>` (`ViewModels/RelayCommand.cs`), no CommunityToolkit.Mvvm/Prism dependency. Validation is per-form static classes (`static class XFormValidation { static string BuildValidationMessage(...) }`) — procedural, not `IValidatableObject`/FluentValidation/DataAnnotations. Shared `ViewModelBase` provides `INotifyPropertyChanged` + `SetProperty<T>` + a shared `ExecuteSaveAsync` validate→save→error sequence.

**State management — CONFIRMED.** One shared shell state (`MainShellViewModel`) for sidebar/selection/breadcrumb + one cross-cutting `Singleton` (`SyncStatusViewModel`). Feature ViewModels are independently `Transient`-scoped, each receiving injected `Func<string,bool>`/`Action<string>` closures for confirm/error `MessageBox.Show` calls — wired directly in `App.xaml.cs`, meaning the composition root itself contains WPF UI calls. **UNKNOWN** whether Investment-side tree/selection ViewModels share a "selected node" concept analogous to Web's `SelectedNodeContext`.

**Test coverage — CONFIRMED substantial.** `Financial.Presentation.Tests` references `Financial.App.csproj` directly, ~45+ files covering Converters, Helpers, Input, `Navigation/NavTreeTests.cs`, and ViewModels for both contexts including form-validation tests and at least one XAML binding test.

## 7–9. Web architecture, React component structure, TypeScript structure

**Architecture — CONFIRMED.** SPA, client-side routing via `react-router-dom` v7, `BrowserRouter` in `main.tsx`. Structure: `pages/` (route containers) → compose `components/` (presentational), driven by `hooks/` (data + form logic), talking to `api/` (HTTP client + hand-written types), one global Context (`SelectedNodeContext`), plus `navigation/`, `utils/`, `styles/`.

**Component structure — CONFIRMED, mixed responsibility.** Pages call one or more hooks and compose components; derived-state logic lives partly in hooks (e.g. totals in `useMonthly`) and partly inline in pages (e.g. tab-switch cancel logic in `MonthlyPage`) — no single rule for where business logic lives. Co-location: components/pages use sibling `__tests__/` folders; hooks/utils use inline `*.test.ts` next to source — two different conventions in the same codebase.

**TypeScript — CONFIRMED no explicit `strict: true`** in `tsconfig.app.json`/`tsconfig.node.json` — only granular flags (`noUnusedLocals`, etc.). `api/types.ts` is **hand-written**, not generated from any OpenAPI spec — no codegen tooling found. Types are manually mirrored against backend Application DTOs with no build-time sync guarantee.

**State/data-fetching — CONFIRMED no external library** (no react-query/SWR/Redux/Zustand/axios; only `react-router-dom` + `recharts` beyond React itself). A shared `useAsyncResource` reducer-based primitive exists, but more complex hooks (e.g. `useMonthly`) hand-roll their own equivalent reducer instead of composing it — a duplicated, not shared, pattern once extra action types are needed. `window.confirm` is used directly inside data hooks for delete confirmations, coupling data logic to a browser global.

**API client — CONFIRMED.** `api/financialApiClient.ts` — single hand-written factory, ~65 typed methods, native `fetch`, no axios. Base URL resolved from `API_BASE_URL` baked in at build time via Vite `define` (matches README/Docker docs). Errors normalized to a typed `ApiError` that parses ASP.NET Core `ProblemDetails` bodies.

**Routing — CONFIRMED two sources of truth**: routes are declared once flat in `main.tsx` and separately, by hand, in `navigation/navTree.ts` for the sidebar — nothing keeps them in sync mechanically. `RootRedirect.tsx` restores the last-visited domain (Investments/CashFlow) from storage.

**Testing — CONFIRMED high ratio.** Vitest + jsdom + React Testing Library, ~90% file-to-test ratio (70 test files / 78 source files), behavior-focused queries (not snapshots). Separate Playwright `smoke-test.mjs` for real end-to-end checks, run in CI.

## 10–11. API architecture, API consumers

**Style — CONFIRMED — Controller-based MVC**, not Minimal APIs (`AddControllers`/`MapControllers`). REST-ful, resource-oriented (`/expenses`, `/assets/{broker}/{portfolio}/{asset}`, etc.), one controller per aggregate/use-case group. 28 controllers total, each importing exclusively either Investment or CashFlow Application types (clean split, no mixed controllers). Base path `/api/v1/financial` is applied via `MapGroup(...)` at the pipeline level, not per-controller attributes — individual controllers use relative routes.

**Composition order — CONFIRMED — Investment DI always registered before CashFlow DI**, consistently, in both `Program.cs` and `App.xaml.cs`.

**DTOs/contracts — CONFIRMED — no API-owned contract layer for domain data.** `Financial.Api/DTOs` contains only 4 infra/diagnostic DTOs (health, sync status, repo config). All domain-facing controllers return Application-layer DTOs directly — any Application DTO shape change is automatically a wire-format change, with no anti-corruption/mapping seam at the API boundary. Same pattern holds for both contexts.

**Middleware — CONFIRMED — one custom middleware**, `DomainExceptionMappingMiddleware`, mapping exactly three exception types (`OverdraftConfirmationRequiredException`→409, `KeyNotFoundException`→404, `ArgumentException`→400) to `ProblemDetails`. It imports **only CashFlow exception types** — Investment has no custom domain exceptions at all (confirmed zero results grepping Investment.Application/.Domain for custom exception classes), so Investment errors rely entirely on the two generic BCL types happening to be thrown, or fall through to the built-in `UseExceptionHandler()`.

**API consumers — CONFIRMED Financial.Web (relative `/api/v1/financial` fetch calls, CORS-allowlisted dev origins `localhost:5173`/`5174`).** Financial.App is **confirmed not** an HTTP consumer (see §5–6) — it's an in-process host of the same Application/Infrastructure code.

**Versioning — INFERRED — cosmetic only.** `/api/v1/` is a hardcoded string constant, not a real ASP.NET versioning mechanism (no `Asp.Versioning` package, no v2 anywhere).

**Health — CONFIRMED.** `GET /health` returns a static `{Status:"ok"}` — liveness only, no actual check of JSON-file readability or Google Drive reachability. `GET /config/repository` (Development-only) exposes Investment repository settings only, not CashFlow's.

**Serialization — CONFIRMED default System.Text.Json via ASP.NET Core Controllers**, no explicit `JsonSerializerOptions`/naming-policy/enum-converter configuration found in `Program.cs`.

## 12–13. Data access architecture, database technology

**CONFIRMED — no relational database anywhere.** Persistence is exclusively one JSON document per bounded context, fully deserialized into memory once at process startup and held for the process lifetime (per `CLAUDE.md`'s documented restart requirement).

- Reads: pure in-memory LINQ over the loaded object graph — no I/O per query.
- Writes (`SaveChangesAsync`): **full-document re-serialization and rewrite** on every save, regardless of mutation size — not incremental/patch writes.
- `LocalJsonStorage`: direct `File.ReadAllTextAsync`/`WriteAllTextAsync`, no locking, no transactional semantics.
- `GoogleDriveJsonStorage` (wrapped by `DebouncedJsonStorage` when selected): 10-second write-coalescing debounce, retry via `TransientRetryPolicy` (up to 5 retries), status tracked via `ISyncStatusProvider`/`SyncState` (Idle/Pending/Saving/Failed) surfaced to both UIs (`SyncStatusBanner` in Web, `SyncStatusViewModel` in WPF).
- **CONFIRMED — `Shared.Infrastructure/Sync` is status-reporting only, not conflict resolution.** No merge/CRDT/multi-writer handling exists anywhere in the persistence stack — single-process, single-writer is an architecturally load-bearing assumption, not just a documented convention.
- Provider selection (`LocalJson`/`GoogleDrive`) is fully independent per context, driven by `Investment:Repository:Provider`/`CashFlow:Repository:Provider`.

## 14. External services/integrations

**CONFIRMED — heavily asymmetric between contexts.** Investment: ~10 external-integration classes — `YahooFinanceService`, `GoogleFinanceService`, `StatusInvestFinanceService`, `FallbackFinanceService` (fallback chain), bond/crypto/standard asset price fetchers, `WebPageParser` (HtmlAgilityPack scraping of Google Finance), `GoogleFinancialSupport` (Google Drive API + legacy Google Sheets import path). CashFlow: exactly one — `FrankfurterExchangeRateProvider` (FX rates; exact target host inferred from naming, not independently verified). Google Drive API (via `GoogleFinancialSupport`) is shared by both contexts for the `GoogleDrive` storage provider.

## 15. Authentication and authorization

**CONFIRMED — absent entirely.** Zero matches for `Authorize`/`Authentication`/`JWT`/`ApiKey`/`Identity` across Api, App, Application, Infrastructure, Integrations (excluding one doc-comment noting the external Yahoo endpoint is itself unauthenticated). No `AddAuthentication()`/`AddAuthorization()` calls, no `[Authorize]` attributes anywhere. CORS origin allowlisting is the sole access-control mechanism, environment-file-configured, consistent with the single-user/self-hosted framing in `CLAUDE.md`.

## 16. Configuration and environment handling

**CONFIRMED — standard ASP.NET Core/Generic Host layering** (`appsettings.json` → `appsettings.{Environment}.json` → env vars, double-underscore syntax). Both Presentation projects build structurally identical, independently-duplicated bootstraps. Investment/CashFlow config sections are fully separate (`Investment:*`/`CashFlow:*`), confirming the README-documented pattern. `Cors:AllowedOrigins` exists only in `appsettings.Development.json`.

## 17. Logging

**CONFIRMED — Serilog wired at the host level only**, in both `Financial.Api` and `Financial.App` (daily rolling file sink, 14-day retention).

**CONFIRMED — application logging coverage is almost nonexistent.** Across every `.cs` file in both contexts' Application/Infrastructure/Domain projects, exactly **one** class (`Financial.CashFlow.Application/Services/CardStatementService.cs`) injects/uses `ILogger`. Investment.Application doesn't even reference the `Microsoft.Extensions.Logging.Abstractions` package that CashFlow.Application does. Serilog effectively only captures ASP.NET Core's own framework request logs plus this one service.

## 18. Error handling

**CONFIRMED — asymmetric, API-layer only.** `DomainExceptionMappingMiddleware` is the single translation point (see §10–11), CashFlow-specific in the exception types it knows about. No per-controller try/catch (deliberate, per an in-code comment). WPF has **no centralized handler found** — errors surface ad hoc via `MessageBox.Show` callbacks injected per-ViewModel in `App.xaml.cs`. **UNKNOWN** whether a global unhandled-exception handler (`DispatcherUnhandledException` or similar) exists in WPF — not verified.

## 19. Testing architecture

**CONFIRMED — uniform stack, no mocking framework anywhere.** All 12 test projects: xUnit + FluentAssertions + coverlet.collector; zero Moq/NSubstitute references. Test doubles are entirely hand-written fakes centralized in `Financial.TestUtilities` (`StubCashFlowRepository`, `StubInvestmentRepository`, sync-status stubs, `FakeHttpMessageHandler`, `FakeTimeProvider`).

- **Architecture enforcement**: `Financial.Architecture.Tests` — plain reflection over `Assembly.GetReferencedAssemblies()`, checks only the forbidden inward edges (Domain↛Application/Infrastructure, Application↛Infrastructure) per context. No Presentation-boundary checks.
- **Domain tests**: pure entity/invariant unit tests, no I/O, no doubles.
- **Application tests**: services tested against hand-written stub repositories, asserting both return values and `SaveChangesCallCount`.
- **Infrastructure tests**: real temp-file I/O round-trips for JSON storage; custom fake HTTP handler for the Frankfurter integration. GoogleDrive-backed storage test coverage is **UNKNOWN** — not confirmed present or absent.
- **API tests**: real in-memory integration tests via `WebApplicationFactory<Program>` against temp-file-backed JSON data, ~30 files, one per resource group, full HTTP round-trips (not isolated controller unit tests).
- **WPF tests**: confirmed substantial (see §5–6).
- **Web tests**: Vitest/RTL component+hook tests (~90% file ratio) plus a genuine Playwright end-to-end smoke test, run in CI against a fully published build.
- **Package-version drift**: `Financial.Investment.Infrastructure.Tests.csproj` pins older xunit/coverlet/Test.Sdk versions than every other test project — reason unconfirmed.
- **`coverlet.runsettings` appears disconnected from CI** — exists, well-formed, documented in `CLAUDE.md`, but `build.yml`'s `dotnet test` step never references it — unclear if coverage is collected anywhere in the actual pipeline.

## 20. Build and deployment

**CONFIRMED** (from CI/deploy config): `.github/workflows/build.yml` runs three jobs per PR — .NET build+test (Windows), Web lint+test+build (Ubuntu/Node 24), and a `browser-smoke-test` job that publishes the real API + built SPA, boots them against seeded test JSON data, and runs the Playwright smoke test end-to-end, gated on the other two jobs passing. `semantic-pr.yml` enforces Conventional Commit PR titles. `Dockerfile`/`docker-compose.yml` build a single image serving both API and SPA from port 8080, `./data` volume-mounted. `scripts/deploy.ps1` is separate, manual, local-only tooling (not part of CI) that publishes framework-dependent builds of both Presentation projects to a git-ignored `deploy/` folder, fixed to `GoogleDrive` storage via checked-in `appsettings.Production.json` files, with launcher scripts (`deploy/start-all.ps1` etc.).

## Architecture Dependency Map (current state)

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
not part of runtime dependency graph (not deeply investigated in this pass — UNKNOWN beyond README description).
```

## Consolidated inconsistencies, technical debt, and unclear areas

*(Descriptive only — no fixes proposed.)*

- **Two independent, unsynchronized composition roots** (`Financial.Api`, `Financial.App`) hold the same bounded-context code in-process, each with its own Infrastructure/persistence instance, and can both write to the same JSON file/Drive target with no coordination mechanism visible anywhere in the codebase.
- **`GoogleFinancialSupport` layering inversion** — names itself part of Investment.Infrastructure, depends on it, but ships separately and is wired directly into both Presentation projects, bypassing Investment.Infrastructure's own DI extension method.
- **No API-owned contract/DTO layer** — Application DTOs are the literal wire format for `Financial.Api`; any Application DTO shape change is an automatic breaking API change.
- **Exception-handling asymmetry** — CashFlow has a custom domain exception mapped to a specific HTTP status; Investment has zero custom exception types, relying on generic BCL exceptions incidentally being caught by the same middleware.
- **Logging coverage gap** — Serilog is fully configured at the host level in both Presentation projects, but only one Application-layer class in the entire backend actually logs anything; Investment.Application doesn't even reference the logging abstraction package.
- **Full-document rewrite persistence** — every save re-serializes and rewrites the entire bounded-context JSON document regardless of mutation size (previously flagged elsewhere as a performance concern, confirmed architecturally here).
- **Duplicated composition-root code** — identical five-call DI bootstrap sequence hand-copied between `Program.cs` and `App.xaml.cs` with no shared helper.
- **Sharp integration-density asymmetry** — Investment has ~10 external market-data integration classes; CashFlow has exactly one.
- **Architecture-Tests enforce only inward-reference rules per bounded context** — no mechanical check on Presentation-layer boundaries (e.g., nothing stops Financial.App or Financial.Api from reaching into another context's Infrastructure, or Domain leaking into Presentation).
- **Frontend/backend contract drift risk** — `Financial.Web/src/api/types.ts` is hand-written and hand-mirrored against backend DTOs with no codegen or shared schema.
- **WPF Views/ViewModels internal inconsistencies**: most Investment XAML lives at the project root rather than under `Views/Investment/` (unlike CashFlow, which is fully under `Views/CashFlow/`); `ViewModels/CashFlow/` mixes true ViewModels, plain row/DTO presentation models, and static validator classes in one flat folder; a shared `MainNavigationViewModelBase` exists only on the Investment side despite CashFlow ViewModels sharing a similar constructor shape.
- **Web frontend inconsistencies**: two different test co-location conventions (components/pages vs. hooks/utils); two sources of truth for the route list (`main.tsx` vs. `navTree.ts`); a shared `useAsyncResource` primitive that more complex hooks bypass by hand-rolling an equivalent reducer; `window.confirm` called directly from data hooks.
- **TypeScript strict mode is not explicitly enabled** anywhere in the Web project's tsconfig chain — only granular lint-style flags are set.
- **`coverlet.runsettings` appears unreferenced by CI** — exists and is documented, but the `dotnet test` step in `build.yml` doesn't invoke it; unclear if/where coverage is actually consumed.
- **Test package-version drift**: `Financial.Investment.Infrastructure.Tests` pins older xunit/coverlet/Test.Sdk versions than every sibling test project, cause unconfirmed.
- **Diagnostic asymmetry**: `GET /config/repository` exposes only Investment repository settings, not CashFlow's, despite both contexts having symmetric config; `GET /health` is liveness-only with no actual dependency check.
- **`/api/v1/` versioning is cosmetic** — a hardcoded string constant, not a real version-negotiation mechanism.

### Open UNKNOWNs flagged across this pass (not resolved, would need targeted follow-up)

- Whether GoogleDrive-backed storage has any automated test coverage.
- Whether Financial.App has a global unhandled-exception handler beyond per-ViewModel `MessageBox` callbacks.
- Whether `FrankfurterExchangeRateProvider`'s target host is actually frankfurter.app (inferred from naming only).
- Sub-feature-level UI parity between Web and WPF below the navigation-tree level (nav-level parity is confirmed 1:1; component-by-component parity was not exhaustively checked).
- Depth of `Tools/CashFlowSpreadsheetImport` and `Tools/ImportGoogleSpreadSheets` internals (not investigated in this pass beyond README's description).
