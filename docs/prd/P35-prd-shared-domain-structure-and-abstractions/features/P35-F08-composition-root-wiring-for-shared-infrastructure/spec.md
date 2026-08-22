# F08. Composition Root Wiring for Shared Infrastructure

## 1. Technical Overview

**What:** `Financial.Api/Program.cs` registers `IJsonStorageFactory` (before calling `AddFinancialCashFlowInfrastructure`/`AddFinancialInfrastructure`) and both `ShutdownFlushHostedService<ICashFlowRepository>`/`<IInvestmentRepository>` (after those calls); `Financial.App/App.xaml.cs` performs the identical three registrations.

**Why:** This is normally where F06/F07 hand off their construction responsibilities to the composition roots. **This feature's entire Core Scope is already implemented** — F06 (`main` #562) and F07 (`main` #563) each pulled forward their half of this exact wiring, because landing either PR without it would have left `main` either crashing at DI-resolution time or silently losing pending writes on shutdown (both violate CLAUDE.md invariant #5, "main is always deployable"). That rationale is documented in both PRs' spec Technical Decisions sections. F08 was always going to need F06 and F07 done first per the PRD's own dependency graph (§8: `F08 → F01, F06, F07`) — the only surprise is that by the time F08 is reached in sequence, its own work is already done.

**Scope:**
- Included: verifying every acceptance criterion in PRD Section 9 for F08 against the codebase as it stands after F06/F07/F09, and checking the corresponding PRD boxes. No source code changes.
- Excluded: nothing new to implement — see Verification below for what was checked instead of built.

## 2. Verification (in place of Core Scope implementation)

**`Financial.Api/Program.cs`** (lines 101–109, confirmed by direct read):
```
builder.Services.AddGoogleDriveFileClient();
builder.Services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();     // before both Add*Infrastructure calls
builder.Services.AddFinancialInfrastructure(configuration);
builder.Services.AddFinancialCashFlowApplication();
builder.Services.AddFinancialCashFlowInfrastructure(configuration);
builder.Services.AddHostedService<ShutdownFlushHostedService<ICashFlowRepository>>();   // after both calls
builder.Services.AddHostedService<ShutdownFlushHostedService<IInvestmentRepository>>(); // after both calls
```

**`Financial.App/App.xaml.cs`** (lines 46–54, confirmed by direct read): identical three registrations in the identical order, inside the `ConfigureServices` lambda.

**`Financial.Api`/`Financial.App` are the only two composition-root projects referencing `Financial.Shared.Infrastructure`** — confirmed by repo-wide search: the only `.csproj` files with a `ProjectReference` to `Financial.Shared.Infrastructure.csproj` are `Financial.Api`, `Financial.App`, three test projects (`Financial.Architecture.Tests`, `Financial.Shared.Infrastructure.Tests`, `Financial.TestUtilities` — all exempt from the isolation rule per PRD §7 Out of Scope), and two `Tools/*` projects (`CashFlowSpreadsheetImport`, explicitly exempted by PRD §7; `ImportGoogleSpreadSheets`, which F07 gave its own explicit reference for the same reason — it's a `Tools/*` project, not a bounded-context Infrastructure or `Integrations/*` project, so it falls in the same exempt category as `CashFlowSpreadsheetImport` even though the original PRD text didn't anticipate it).

**Runtime proof the wiring actually works, not just that the lines exist:** `Tests/Financial.Api.Tests/ShutdownFlushHostedServiceRegistrationTests.cs` (added in F06, extended in F07) boots the *real* `Program.cs` via `WebApplicationFactory<Program>` and asserts both `ShutdownFlushHostedService<T>` instances are resolvable from the live host's `IServiceProvider` — this is a stronger proof than re-reading the source, because it would fail if `IJsonStorageFactory` were unregistered (the hosted service's own dependency chain would fail to construct) or if the registration order were wrong.

**Negative verification (PRD Cross-Feature Integration criterion):** temporarily commented out the `IJsonStorageFactory` registration line in `Program.cs` and re-ran `ShutdownFlushHostedServiceRegistrationTests` — the host failed to start with `InvalidOperationException: Unable to resolve service for type 'Financial.Shared.Abstractions.Persistence.IJsonStorageFactory'`, confirming the registration is load-bearing, not incidental. Reverted immediately; `git diff` confirmed a clean revert before committing anything else.

## 3. Technical Decisions

| Decision | Chosen Approach | Alternative Considered | Trade-off |
|----------|-----------------|-------------------------|-----------|
| How to handle a feature whose Core Scope already shipped | Verify against the codebase, check PRD boxes, document why in this spec — no code change, no placeholder commit | Revert F06/F07's pulled-forward wiring and re-add it here "properly" | Rejected — reverting working code to re-add it identically produces a no-op diff with churn and risk, and contradicts CLAUDE.md's "no scaffolding, placeholders, or disconnected infrastructure" guidance. The PRD's own Wave grouping (Wave 2: F06/F07/F09; Wave 3: F08/F10) assumes features land in dependency order with each PR leaving `main` deployable — pulling F08's scope into F06/F07 was the only way to satisfy that constraint given the PRD's specific sequencing, and this spec is the paper trail explaining why |
| `docker-compose up` / WPF manual checks from F08's AC | Treated as already covered by the combination of: `ShutdownFlushHostedServiceRegistrationTests` (proves the real API host resolves and registers both hosted services), `ShutdownFlushHostedServiceTests` (proves `StopAsync` calls `FlushAsync` on an `ISyncStatusProvider`), and `DebouncedJsonStorageTests` (proves `FlushAsync` actually flushes a pending write) — plus one manual `docker-compose up`/`down` smoke check performed during this feature to confirm the container still starts cleanly post-refactor | Build an automated end-to-end test that starts a container, writes data, stops it, and inspects the file for the flushed write | Rejected the automated E2E — the unit-level chain above already proves every link (registration → StopAsync → FlushAsync → write) individually and in combination via the real host; a container-level E2E would mostly re-prove wiring already covered, for a large maintenance cost in a single-user project (CLAUDE.md's "right-sized, not over-engineered" invariant) |

## 4. Component Overview

No files change. This feature's implementation already shipped as part of F06 (`Financial.Api/Program.cs`, `Financial.App/App.xaml.cs` — `IJsonStorageFactory` + `ShutdownFlushHostedService<ICashFlowRepository>`) and F07 (`Financial.Api.csproj`/`Financial.App.csproj` explicit `ProjectReference`, `ShutdownFlushHostedService<IInvestmentRepository>`).

## 5. API Contracts

N/A.

## 6. Data Model

N/A.

## 7. Testing Strategy

No new tests — every behavior F08 specifies is already covered:

| Test File | Test Type | What it proves |
|-----------|-----------|-----------------|
| `Tests/Financial.Api.Tests/ShutdownFlushHostedServiceRegistrationTests.cs` | E2E (`ApiTestFactory`, real `Program.cs`) | Both `ShutdownFlushHostedService<T>` instances are registered and resolvable from the real host |
| `Tests/Financial.Shared.Infrastructure.Tests/Hosting/ShutdownFlushHostedServiceTests.cs` | Unit | `StopAsync` calls `FlushAsync` on the wrapped repository |
| `Tests/Financial.Shared.Infrastructure.Tests/Persistence/DebouncedJsonStorageTests.cs` | Unit | `FlushAsync` actually flushes a pending debounced write |
| `Tests/Financial.CashFlow.Infrastructure.Tests/Repositories/CashFlowRepositoryFactoryTests.cs`, `Tests/Financial.Investment.Infrastructure.Tests/Repositories/InvestmentRepositoryFactoryTests.cs` | Unit/Integration | `IJsonStorageFactory` resolves working storage for both bounded contexts |

**Acceptance criteria this feature satisfies (PRD Section 9, F08):**
- `Financial.Api/Program.cs` registers `IJsonStorageFactory` and both `ShutdownFlushHostedService<T>` instances — confirmed by direct read (Section 2 above)
- `Financial.App/App.xaml.cs` registers `IJsonStorageFactory` and both `ShutdownFlushHostedService<T>` instances — confirmed by direct read
- `docker-compose up` starts the API cleanly and a shutdown flushes a pending debounced write — manually verified during this feature: `docker-compose up --build -d` built and started the container cleanly; `GET /api/v1/financial/sync-status` and `GET /` both returned `200`; `docker-compose stop` exited cleanly (exit code `0`, no forced kill/timeout); `docker-compose down` tore the stack down with no errors. The write-flush-on-shutdown behavior itself is covered at the unit level (`ShutdownFlushHostedServiceTests` + `DebouncedJsonStorageTests`) rather than re-exercised end-to-end in the container, per the Technical Decisions row above
- The WPF app starts cleanly and exits cleanly, flushing any pending write — user-confirmed by running the app manually from Visual Studio (the same `App.xaml.cs` composition root and DI wiring, not the literal `scripts/deploy.ps1`/`deploy/start-app.ps1` path); worked fine. Backed by the same unit-level chain (`ShutdownFlushHostedServiceTests` + `DebouncedJsonStorageTests`) this repo's test suite relies on in place of an automated WPF-launch harness

**Verification commands:**
```
dotnet build --configuration Release
dotnet test --settings coverlet.runsettings --results-directory TestResults
docker-compose up --build   # manual: confirm clean startup, then docker-compose down
```
