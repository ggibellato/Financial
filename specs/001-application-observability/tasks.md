---
description: "Task list for Application Observability (revision 3 — Shared.Abstractions + explicit calls)"
---

# Tasks: Application Observability

**Input**: Design documents from `specs/001-application-observability/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md, logging-audit.md (all present)

**Tests**: Included — Constitution Principle V makes unit/integration tests mandatory. xUnit + FluentAssertions, no mocking framework. `ITelemetryTracer`/`ITelemetrySpan` are trivial to fake by hand.

**Organization**: Tasks are grouped by user story, further grouped into **suggested PR slices sized to the user's ~5-source/config-file target** (excluding docs and test files). Revision note: this supersedes a design that used a `TracingDispatchProxy` decorator (fully built, tested, and reverted — see git history on this branch and research.md Decision D1/D3) in favor of a dependency-free `Financial.Shared.Abstractions` project that Application code calls directly.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Every task includes exact file paths

## User Story 4 — already satisfied

[logging-audit.md](./logging-audit.md) satisfies FR-011/SC-005 as a standalone deliverable. No tasks needed; consumed by US1 tasks below.

---

## Phase 1: Setup — the dependency-free abstraction (Suggested PR 1 — 4 files)

**Purpose**: The interface everything else in this feature depends on, in a project with zero dependencies of its own.

- [X] T001 Create `Financial.Shared.Abstractions/Financial.Shared.Abstractions.csproj` — new class library, **zero** `PackageReference`/`ProjectReference` entries, added to `Financial.slnx` (under the existing `/DDD/Shared/` folder, alongside `Financial.Shared.Infrastructure`)
- [X] T002 [P] Create `Financial.Shared.Abstractions/ITelemetryTracer.cs` — `StartSpan(string name) : ITelemetrySpan`, per [contracts/telemetry-tracer-interface-contract.md](./contracts/telemetry-tracer-interface-contract.md) — depends on T001
- [X] T003 [P] Create `Financial.Shared.Abstractions/ITelemetrySpan.cs` — `IDisposable` + `SetAttribute(string,string)` + `RecordException(Exception)` — depends on T001
- [X] T004 Create `Financial.Shared.Abstractions/TelemetryAttributeKeys.cs` — `TelemetryAttributeKeys`/`TelemetryOperationResults` constants per [contracts/telemetry-semantic-conventions.md](./contracts/telemetry-semantic-conventions.md) — depends on T001

**Tests (not counted toward the 4-file limit)**:
- [X] T005 [P] Create `Tests/Financial.Architecture.Tests/SharedAbstractionsDependencyRuleTests.cs` asserting `Financial.Shared.Abstractions`'s compiled assembly has zero referenced project assemblies (research.md Decision D9) — depends on T001. Required adding a direct `ProjectReference` from `Financial.Architecture.Tests.csproj` to the new project (mirrors the existing pattern for every other project under test). 1/1 passing (8/8 in the full `Financial.Architecture.Tests` suite).

**Post-review note**: architecture-reviewer approved with no corrections required. One flag for later PRs (not this one): once Application/Infrastructure start referencing this project, a new architecture test should assert Application does not reach *past* it into `Integrations/Observability` — add that when PR 4a/4b/4d first wires a real consumer.

**Checkpoint**: `Financial.Shared.Abstractions` exists, is provably dependency-free, and defines the contract. Nothing references it yet.

---

## Phase 2: Foundational — Integrations/Observability project, no-op only (Suggested PR 2 — 5 files)

**Purpose**: Stand up the new project and its DI registration with only the no-op path. Delivers User Story 2 once wired into the composition roots (Phase 3).

- [X] T006 Create `Integrations/Observability/Observability.csproj` (deviation: named `Observability.csproj` to match the leaf-folder-name convention used by `Integrations/GoogleFinancialSupport/GoogleFinancialSupport.csproj`, rather than `Integrations.Observability.csproj`) — new class library, added to `Financial.slnx`; references `Financial.Shared.Abstractions` only (no OpenTelemetry package yet) — depends on T001
- [X] T007 [P] Create `Integrations/Observability/ObservabilityOptions.cs` — `Enabled`/`Backend`/`Endpoint`/`Langfuse` per [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md) — depends on T006
- [X] T008 [P] Create `Integrations/Observability/ObservabilityBackend.cs` — enum `Jaeger`/`Langfuse` — depends on T006
- [X] T009 Create `Integrations/Observability/NoOpTelemetryTracer.cs` — `ITelemetryTracer`/`ITelemetrySpan` implementation returning a cached, allocation-free no-op span (FR-006a) — depends on T002, T003, T006
- [X] T010 Create `Integrations/Observability/ObservabilityServiceCollectionExtensions.cs` — `AddObservability(this IServiceCollection, IConfiguration, string serviceName)`: binds `ObservabilityOptions` via `services.Configure<ObservabilityOptions>(configuration.GetSection(...))`, registers `NoOpTelemetryTracer` as `ITelemetryTracer` unconditionally for now — depends on T007, T008, T009

**Tests (not counted)**:
- [X] T011 [P] Unit tests in `Tests/Financial.Observability.Tests/ObservabilityServiceCollectionExtensionsTests.cs` (deviation: project renamed to `Financial.Observability.Tests` to match this repo's `Financial.*`-prefixed test-project convention): `ITelemetryTracer` always resolves and never throws/returns null from `StartSpan`, span methods never throw, and `ObservabilityOptions` binds correctly from configuration (both defaults and explicit values including nested `Langfuse`). 5/5 passing.

**Post-review fixes** (architecture-reviewer caught one required issue before commit): T010's original implementation left `ObservabilityOptions` completely unbound — three fully-built config types with a documented contract shape but zero consumers, which is scaffolding by the letter of Constitution Principle VII. Fixed by adding the `Configure<ObservabilityOptions>` call (required a new `Microsoft.Extensions.Options.ConfigurationExtensions` package reference) plus two binding tests. Also renamed the integration project's `.csproj` and the test project/folder per the reviewer's naming-convention recommendation.

**Checkpoint**: `Integrations/Observability` exists and is self-contained; nothing outside it yet references it.

---

## Phase 3: User Story 2 - Run the application with observability fully disabled (Priority: P1)

**Goal**: Wire the no-op-only `Integrations/Observability` project into both composition roots.

**Independent Test**: Start the app via the standard production path with no observability container running; every existing feature still works; `ITelemetryTracer` resolves (to the no-op).

### Suggested PR 3a — Financial.Api wiring (3 files) [US2]

- [ ] T012 [US2] Add `ProjectReference` to `Integrations/Observability/Integrations.Observability.csproj` in `Financial.Api/Financial.Api.csproj`
- [ ] T013 [US2] Call `builder.Services.AddObservability(configuration, serviceName: "Financial.Api")` in `Financial.Api/Program.cs` — depends on T010, T012
- [ ] T014 [US2] Add the `Observability` section (default `Enabled: false`) to `Financial.Api/appsettings.json`

### Suggested PR 3b — Financial.App wiring (3 files) [US2]

- [ ] T015 [US2] Add `ProjectReference` to `Integrations/Observability/Integrations.Observability.csproj` in `Financial.App/Financial.App.csproj`
- [ ] T016 [US2] Call `services.AddObservability(context.Configuration, serviceName: "Financial.App")` in `Financial.App/App.xaml.cs`'s `ConfigureServices` — depends on T010, T015
- [ ] T017 [US2] Add the `Observability` section (default `Enabled: false`) to `Financial.App/appsettings.json`

### Tests (not counted)

- [ ] T018 [US2] Integration test in `Tests/Financial.Api.Tests/ObservabilityDisabledTests.cs`: app starts and `GET /api/v1/financial/sync-status` succeeds with `Observability:Enabled=false` and no telemetry endpoint reachable — depends on T013
- [ ] T019 [P] [US2] Test in `Tests/Financial.Presentation.Tests/DependencyInjection/ObservabilityServiceRegistrationTests.cs`: CashFlow services still resolve and `ITelemetryTracer` resolves to a usable no-op — depends on T016
- [ ] T020 [US2] Run and record [quickstart.md](./quickstart.md) Scenario A as the PR's Constitution Principle VIII start-up check

**Checkpoint**: User Story 2 fully and independently satisfied.

---

## Phase 4: User Story 1 - Diagnose a request end to end while it's happening (Priority: P1) 🎯 MVP

**Goal**: Make tracing real, and produce one correlated trace spanning entry point → application use case → storage, via explicit `ITelemetryTracer` calls (research.md Decision D3).

**Independent Test**: Enable observability, start Jaeger, perform one action from a client, find one trace with linked spans for entry point, application service, and storage.

### Suggested PR 4a — Real OpenTelemetry wiring, still produces zero spans elsewhere (3 files) [US1]

- [ ] T021 [US1] Add `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime` package references to `Integrations/Observability/Integrations.Observability.csproj` — the only project in the solution to ever get these references (FR-005/SC-008)
- [ ] T022 [US1] Create `Integrations/Observability/OpenTelemetryTracer.cs` — `ITelemetryTracer`/`ITelemetrySpan` implementation wrapping a `System.Diagnostics.ActivitySource` internally — depends on T002, T003, T021
- [ ] T023 [US1] Update `Integrations/Observability/ObservabilityServiceCollectionExtensions.cs`: when `Enabled=true`, register `OpenTelemetryTracer`; wire `.AddOpenTelemetry().WithTracing(...)` (AddSource, AspNetCore + HttpClient instrumentation, OTLP exporter) and `.WithMetrics(...)` — depends on T022

### Suggested PR 4b — Storage spans, shared by both contexts (3 files) [US1]

- [ ] T024 [US1] Add `ProjectReference` to `Financial.Shared.Abstractions` in `Financial.Shared.Infrastructure/Financial.Shared.Infrastructure.csproj` — depends on T001
- [ ] T025 [US1] Inject `ITelemetryTracer` into `Financial.Shared.Infrastructure/Persistence/DebouncedJsonStorage.cs` and wrap load/save with `StartSpan("JsonStorage.Load"/"JsonStorage.Save")` per [contracts/telemetry-semantic-conventions.md](./contracts/telemetry-semantic-conventions.md) — depends on T024
- [ ] T026 [P] [US1] Same for `Financial.Shared.Infrastructure/Persistence/GoogleDriveJsonStorage.cs` (`"GoogleDrive.Upload"`/`"GoogleDrive.Download"`) — depends on T024

### Suggested PR 4c — Jaeger local overlay (1 file) [US1]

- [ ] T027 [US1] Create `docker-compose.observability.yml` with a `jaeger` Compose profile per [research.md](./research.md) Decision D7 — never referenced by base `docker-compose.yml`

### Suggested PR 4d — CashFlow's first traced use case, MVP checkpoint (2 files) [US1]

- [ ] T028 [US1] Add `ProjectReference` to `Financial.Shared.Abstractions` in `Financial.CashFlow.Application/Financial.CashFlow.Application.csproj` — depends on T001
- [ ] T029 [US1] Inject `ITelemetryTracer` into `Financial.CashFlow.Application/Services/ExpenseService.cs`'s constructor and wrap `CreateExpenseAsync` (and any other public method) with `StartSpan("CashFlow.ExpenseService.{MethodName}")`, setting `entity.type`/`operation.result` per the allow-list — depends on T028

**Checkpoint after PR 4a–4d**: User Story 1's Independent Test is satisfiable end to end for "create an expense" via Web — this is the MVP.

### Suggested PR 4e onward — extend CashFlow coverage (repeatable, ~1 csproj + 3-4 services per PR) [US1]

- [ ] T030 [US1] Repeat T029's pattern for `IncomeService`, `BankService`, `TransferService` in `Financial.CashFlow.Application/Services/` — depends on T028
- [ ] T031 [US1] Repeat for the remaining CashFlow services (`ReserveService`, `CardStatementService` — extend its existing `ILogger` usage with a span too, `CreditCardService`, `CategoryService`, `IncomeSourceService`, `ReserveBucketService`, `InvestmentAccountService`, `TitheService`, `BalanceAdjustmentService`, `ControleMaeService`, `MensaisService`, `InvestmentSnapshotService`, `AnnualSummaryService`), spread across as many small PRs as needed (~3-4 services each) — depends on T028

### Suggested PR 4f — Investment's first traced use case, parity (2 files) [US1]

- [ ] T032 [US1] Add `ProjectReference` to `Financial.Shared.Abstractions` in `Financial.Investment.Application/Financial.Investment.Application.csproj` — depends on T001
- [ ] T033 [US1] Inject `ITelemetryTracer` into one representative Investment service (e.g. `AssetPriceService.cs`) and wrap its primary method, same pattern as T029 — depends on T032

### Suggested PR 4g onward — extend Investment coverage (repeatable) [US1]

- [ ] T034 [US1] Repeat T033's pattern for the remaining Investment services, spread across small PRs — depends on T032

### Suggested PR 4h — WPF trace root, one representative command (3 files) [US1]

- [ ] T035 [US1] Add `ProjectReference` to `Financial.Shared.Abstractions` in `Financial.App/Financial.App.csproj` — depends on T001
- [ ] T036 [US1] Add an `ITelemetryTracer` constructor dependency to `Financial.App/ViewModels/CashFlow/MonthlyViewModel.cs` and wrap its save-expense command body in `StartSpan("App.MonthlyViewModel.SaveExpense")` (FR-004a) — depends on T035
- [ ] T037 [US1] Update the `MonthlyViewModel` registration in `Financial.App/App.xaml.cs` to pass `sp.GetRequiredService<ITelemetryTracer>()` — depends on T036

### Suggested PR 4i — Log correlation (3 files) [US1]

- [ ] T038 [US1] Add `Serilog.Sinks.OpenTelemetry` package reference to `Integrations/Observability/Integrations.Observability.csproj` only; add `WriteToObservability(this LoggerConfiguration, IConfiguration)` in `Integrations/Observability/SerilogObservabilityExtensions.cs` — per [research.md](./research.md) Decision D4
- [ ] T039 [US1] Call `.WriteToObservability(context.Configuration)` from `UseSerilog(...)` in both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` — depends on T038

### Suggested PR 4j — Top logging-audit fix (1 file) [US1]

- [ ] T040 [US1] Log the caught exception (via `ILogger`, `error.type` only, no raw message that embeds a financial value) in `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs` before translating it to a `ProblemDetails` response — logging-audit.md's top remediation priority

### Tests (not counted toward any slice above)

- [ ] T041 [US1] Integration test in `Tests/Financial.Api.Tests/` using an in-memory span-capture `ITelemetryTracer` test double (registered via `ConfigureTestServices`) asserting one correlated trace spans controller → `ExpenseService` → `JsonStorage.Save` for a sample request — depends on T029, T025
- [ ] T042 [P] [US1] Unit tests in `Tests/Financial.CashFlow.Application.Tests/` asserting `ExpenseService` calls `StartSpan` with the expected name and only allow-listed attributes, using a hand-written `RecordingTelemetryTracer` — depends on T029

**Checkpoint**: User Story 1 fully functional for the instrumented services — one connected trace end to end via Jaeger, with correlated logs and the top logging-audit gap closed. Full coverage of every service (T031/T034) continues incrementally after the MVP checkpoint.

---

## Phase 5: User Story 3 - Toggle and choose an observability backend via configuration only (Priority: P2)

### Suggested PR 5a — Backend switch + fail-fast validation (1 file) [US3]

- [ ] T043 [US3] Update `Integrations/Observability/ObservabilityServiceCollectionExtensions.cs`: branch the OTLP exporter configuration on `Backend` (Langfuse Basic Auth header vs. plain endpoint for Jaeger); throw a clear startup exception for an unrecognized `Backend` or missing Langfuse key — depends on T023

### Suggested PR 5b — Langfuse local overlay (1 file) [US3]

- [ ] T044 [US3] Add a `langfuse` Compose profile to `docker-compose.observability.yml`, referencing Langfuse's official minimal self-hosted stack with ephemeral/local-only volumes — depends on T027

### Tests (not counted)

- [ ] T045 [P] [US3] Unit test in `Tests/Integrations.Observability.Tests/`: exporter configured with Basic Auth when `Backend=Langfuse`, without one when `Backend=Jaeger`; unrecognized `Backend` throws — depends on T043
- [ ] T046 [US3] Run and record [quickstart.md](./quickstart.md) Scenario C — depends on T043, T044

**Checkpoint**: Both backends usable and swappable purely via configuration.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T047 [P] Integration test in `Tests/Financial.Api.Tests/` for the backend-unreachable edge case — [quickstart.md](./quickstart.md) Scenario D / SC-006 — depends on T023
- [ ] T048 [P] Add an "Observability" section to `README.md` covering local setup, backend choice, and where `logging-audit.md`'s findings live
- [ ] T049 Run `dotnet build --configuration Release`, `dotnet test`, and `docker-compose up` (base file, unchanged) and record results per Constitution Principle VIII
- [ ] T050 [P] Add `ILogger`-based use-case entry/success logging alongside the spans added in T029-T031/T033-T034 (logging-audit.md priority 2 — the two land together naturally at the same call sites)
- [ ] T051 [P] Add retry/fallback logging in `Financial.Shared.Infrastructure/Resilience/TransientRetryPolicy.cs` and `Financial.Investment.Infrastructure/Services/FallbackFinanceService.cs` (logging-audit.md priority 3)
- [ ] T052 [P] Instrument the remaining high-value WPF `catch` blocks identified in `logging-audit.md` (`ReservaViewModel`, `ControleMaeViewModel`, `MensaisViewModel`) with `ILogger`, following the pattern established in T036

---

## Suggested PR sequence & file counts (production code/config only — excludes docs and `*Tests*` files)

| PR | Tasks | Files | Story |
|---|---|---|---|
| 1 | T001–T004 | 4 | Setup |
| 2 | T006–T010 | 5 | Foundational |
| 3a | T012–T014 | 3 | US2 |
| 3b | T015–T017 | 3 | US2 |
| 4a | T021–T023 | 3 | US1 |
| 4b | T024–T026 | 3 | US1 |
| 4c | T027 | 1 | US1 |
| 4d | T028–T029 | 2 | US1 — **MVP checkpoint** |
| 4e+ | T030, T031 | 1 csproj (already added) + services, ~3-4/PR | US1 (repeatable) |
| 4f | T032–T033 | 2 | US1 |
| 4g+ | T034 | ~3-4 services/PR | US1 (repeatable) |
| 4h | T035–T037 | 3 | US1 |
| 4i | T038–T039 | 3 | US1 |
| 4j | T040 | 1 | US1 |
| 5a | T043 | 1 | US3 |
| 5b | T044 | 1 | US3 |
| 6 | T047–T052 | up to 5, split further if needed | Polish |

## Dependencies & Execution Order

- **Setup (Phase 1)**: no dependencies
- **Foundational (Phase 2)**: depends on Phase 1
- **User Story 2 (Phase 3)**: depends on Phase 2 — a complete, deployable increment on its own
- **User Story 1 (Phase 4)**: depends on Phase 2/3 (composition roots must call `AddObservability`)
- **User Story 3 (Phase 5)**: depends on Phase 4a (T023)
- **Polish (Phase 6)**: depends on the tasks it references

### Parallel Opportunities

- T002, T003 (Phase 1) in parallel
- T007, T008 (Phase 2) in parallel
- T025, T026 (Phase 4b) in parallel
- T047, T048, T050, T051, T052 (Polish) in parallel

---

## Implementation Strategy

### MVP First

1. PR 1 → PR 2 → PR 3a → PR 3b (User Story 2 fully provable, zero OpenTelemetry package in the solution yet)
2. PR 4a → PR 4b → PR 4c → PR 4d (**MVP**: one correlated CashFlow trace via Jaeger, `ExpenseService` instrumented)
3. **STOP and VALIDATE**: run [quickstart.md](./quickstart.md) Scenario B
4. Continue extending coverage (4e+, 4f, 4g+), WPF (4h), log correlation (4i), the top logging fix (4j) — each a separate small PR
5. PR 5a/5b (Langfuse), then Polish

### Notes

- Every PR keeps `Observability:Enabled=false` as the shipped default, so Constitution Principle VIII holds at every step.
- Commit after each task or logical group — one PR per slice, per the user's explicit instruction.
- Full service-by-service tracing coverage (4e+/4g+) is intentionally open-ended in this document — each such PR follows the exact pattern established by T029/T033, so it doesn't need its own enumerated task per service.
