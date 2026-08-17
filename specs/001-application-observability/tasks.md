---
description: "Task list for Application Observability (revised — interface + Integrations/Observability isolation)"
---

# Tasks: Application Observability

**Input**: Design documents from `specs/001-application-observability/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md, logging-audit.md (all present)

**Tests**: Included — Constitution Principle V makes unit/integration tests mandatory for every feature in this repository. Tests follow this repo's convention: xUnit + FluentAssertions, no mocking framework. `ITelemetryTracer`/`ITelemetrySpan` are trivial to fake by hand (a `RecordingTelemetryTracer` test double), which is *easier* than the reverted attempt's direct-SDK testing.

**Organization**: Tasks are grouped by user story, and further grouped into **suggested PR slices sized to the user's ~5-source/config-file target** (excluding docs and test files — each slice's file count is stated and counts only production code/config files). A phase that would otherwise need more files is split into multiple slices/PRs, per the user's explicit instruction.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Every task includes exact file paths

## User Story 4 — already satisfied

**User Story 4** ("Assess whether existing logging is fit for purpose", P3) is already complete: [logging-audit.md](./logging-audit.md), carried forward unchanged from the original planning pass, satisfies FR-011/SC-005 as a standalone deliverable. No tasks needed; consumed by US1 tasks below (T023, T024, T032) as the prioritized remediation list.

---

## Phase 1: Setup — the first-party abstraction (Suggested PR 1 — 4 files)

**Purpose**: The interface + decorator that everything else in this feature depends on. No OpenTelemetry package reference anywhere in this phase — pure BCL/first-party code, fully unit-testable in isolation.

- [X] T001 [P] Create `Financial.Shared.Infrastructure/Observability/ITelemetryTracer.cs` — `StartSpan(string name) : ITelemetrySpan`, per [contracts/telemetry-tracer-interface-contract.md](./contracts/telemetry-tracer-interface-contract.md)
- [X] T002 [P] Create `Financial.Shared.Infrastructure/Observability/ITelemetrySpan.cs` — `IDisposable` + `SetAttribute(string,string)` + `RecordException(Exception)`
- [X] T003 Create `Financial.Shared.Infrastructure/Observability/TracingDispatchProxy.cs` — generic `System.Reflection.DispatchProxy` subclass; on each intercepted call, starts a span named `{BoundedContext}.{InterfaceName}.{MethodName}` via the injected `ITelemetryTracer`, invokes the real target, calls `RecordException` if the target throws, disposes the span — depends on T001, T002; per [research.md](./research.md) Decision D3. Handles sync success/failure, async `Task`/`Task<T>` success/failure/cancellation, and unwraps `TargetInvocationException` so callers still see the original exception type.
- [X] T004 Create `Financial.Shared.Infrastructure/Observability/ServiceCollectionDecorationExtensions.cs` — `Decorate<TInterface>(this IServiceCollection, string boundedContext)` helper that re-registers `TInterface` wrapped in a `TracingDispatchProxy<TInterface>` around the previously registered implementation — depends on T003
- [X] T004a (added post-review) Create `Financial.Shared.Infrastructure/Observability/TelemetryAttributeKeys.cs` — centralizes the `bounded_context`/`operation.name`/`operation.result` attribute keys and `success`/`failed`/`canceled`/`rejected` result values as named constants, per the architecture-reviewer's "no magic strings, shared across future PRs" finding

**Tests (same PR, not counted toward the 5-file limit)**:
- [X] T005 [P] Unit tests in `Tests/Financial.Shared.Infrastructure.Tests/Observability/TracingDispatchProxyTests.cs` using a hand-written `RecordingTelemetryTracer` fake: sync success, sync exception (type preserved), async `Task` success, async `Task<T>` exception, async cancellation. 5/5 passing.

**Post-review fixes** (architecture-reviewer caught two real bugs before commit): (1) `Invoke`'s catch clause only handled `TargetInvocationException` with a non-null `InnerException` — broadened to catch any exception and unwrap only when that specific shape applies, so the span can never leak uncompleted. (2) A canceled `Task` was reported as `operation.result=success` — now reported as `canceled`, without calling `RecordException` (cancellation isn't treated as an error).

**Checkpoint**: The abstraction and decoration mechanism are proven correct with zero dependency on any real telemetry backend.

---

## Phase 2: Foundational — Integrations/Observability project, no-op only (Suggested PR 2 — 5 files)

**Purpose**: Stand up the new project and its DI registration, but only the no-op path — no OpenTelemetry package reference yet. This alone, once wired into the composition roots (Phase 3), already delivers User Story 2.

- [ ] T006 Create `Integrations/Observability/Integrations.Observability.csproj` — new class library project added to `Financial.slnx`; references `Financial.Shared.Infrastructure` only (no OpenTelemetry package yet)
- [ ] T007 [P] Create `Integrations/Observability/ObservabilityOptions.cs` — `Enabled`/`Backend`/`Endpoint`/`Langfuse` per [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md) and [data-model.md](./data-model.md) — depends on T006
- [ ] T008 [P] Create `Integrations/Observability/ObservabilityBackend.cs` — enum `Jaeger`/`Langfuse` — depends on T006
- [ ] T009 Create `Integrations/Observability/NoOpTelemetryTracer.cs` — `ITelemetryTracer`/`ITelemetrySpan` implementation returning a cached, allocation-free no-op span (FR-006a) — depends on T001, T002, T006
- [ ] T010 Create `Integrations/Observability/ObservabilityServiceCollectionExtensions.cs` — `AddObservability(this IServiceCollection, IConfiguration, string serviceName)`: binds `ObservabilityOptions`, registers `NoOpTelemetryTracer` as `ITelemetryTracer` unconditionally for now (the real implementation is added in Phase 4) — depends on T007, T008, T009

**Tests (not counted toward the 5-file limit)**:
- [ ] T011 [P] Unit tests in `Tests/Integrations.Observability.Tests/ObservabilityServiceCollectionExtensionsTests.cs`: `ITelemetryTracer` always resolves and never throws/returns null from `StartSpan`, regardless of `Enabled`

**Checkpoint**: `Integrations/Observability` exists and is self-contained; nothing outside it yet references it.

---

## Phase 3: User Story 2 - Run the application with observability fully disabled (Priority: P1)

**Goal**: Wire the no-op-only `Integrations/Observability` project into both composition roots, proving the app works with zero observability dependency — User Story 2's full requirement, achievable before any real OpenTelemetry code exists.

**Independent Test**: Start the app via the standard production path with no observability container running; every existing feature still works; `ITelemetryTracer` resolves (to the no-op).

### Suggested PR 3a — Financial.Api wiring (3 files) [US2]

- [ ] T012 [US2] Add `ProjectReference` to `Integrations/Observability/Integrations.Observability.csproj` in `Financial.Api/Financial.Api.csproj`
- [ ] T013 [US2] Call `builder.Services.AddObservability(configuration, serviceName: "Financial.Api")` in `Financial.Api/Program.cs`, alongside the other `Add*` composition calls — depends on T010, T012
- [ ] T014 [US2] Add the `Observability` section (default `Enabled: false`) to `Financial.Api/appsettings.json`

### Suggested PR 3b — Financial.App wiring (3 files) [US2]

- [ ] T015 [US2] Add `ProjectReference` to `Integrations/Observability/Integrations.Observability.csproj` in `Financial.App/Financial.App.csproj`
- [ ] T016 [US2] Call `services.AddObservability(context.Configuration, serviceName: "Financial.App")` in `Financial.App/App.xaml.cs`'s `ConfigureServices`, mirroring T013 — depends on T010, T015
- [ ] T017 [US2] Add the `Observability` section (default `Enabled: false`) to `Financial.App/appsettings.json`

### Tests (not counted toward either slice's file limit)

- [ ] T018 [US2] Integration test in `Tests/Financial.Api.Tests/ObservabilityDisabledTests.cs` (`WebApplicationFactory`): app starts and `GET /api/v1/financial/sync-status` succeeds with `Observability:Enabled=false` and no telemetry endpoint reachable — depends on T013
- [ ] T019 [P] [US2] Test in `Tests/Financial.Presentation.Tests/DependencyInjection/ObservabilityServiceRegistrationTests.cs`: CashFlow services still resolve and `ITelemetryTracer` resolves to a usable no-op with `Observability:Enabled=false` — depends on T016
- [ ] T020 [US2] Run and record [quickstart.md](./quickstart.md) Scenario A (`docker-compose up`, base file only) as the PR's Constitution Principle VIII start-up check

**Checkpoint**: User Story 2 is fully and independently satisfied — two small PRs, no OpenTelemetry package anywhere yet.

---

## Phase 4: User Story 1 - Diagnose a request end to end while it's happening (Priority: P1) 🎯 MVP

**Goal**: Make tracing real (OpenTelemetry SDK, confined to `Integrations/Observability`) and produce one correlated trace spanning entry point → application use case → storage, for both bounded contexts and both clients, with the highest-value logging-audit gaps closed.

**Independent Test**: Enable observability, start Jaeger, perform one action from a client, find one trace with linked spans for entry point, application service, and storage.

### Suggested PR 4a — Real OpenTelemetry wiring, still produces zero spans elsewhere (3 files) [US1]

- [ ] T021 [US1] Add `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime` package references to `Integrations/Observability/Integrations.Observability.csproj` — the only project in the solution to ever get these references (FR-005/SC-008)
- [ ] T022 [US1] Create `Integrations/Observability/OpenTelemetryTracer.cs` — `ITelemetryTracer`/`ITelemetrySpan` implementation wrapping a `System.Diagnostics.ActivitySource` internally — depends on T001, T002, T021
- [ ] T023 [US1] Update `Integrations/Observability/ObservabilityServiceCollectionExtensions.cs`: when `Enabled=true`, register `OpenTelemetryTracer` instead of `NoOpTelemetryTracer`, and wire `.AddOpenTelemetry().WithTracing(...)` (AddSource on the internal `ActivitySource` name, AspNetCore + HttpClient instrumentation, OTLP exporter at `Endpoint`) and `.WithMetrics(...)` (AspNetCore + HttpClient + Runtime instrumentation, same exporter) — depends on T022

**Tests (not counted)**: extend `Tests/Integrations.Observability.Tests/ObservabilityServiceCollectionExtensionsTests.cs` (T011) with an `Enabled=true` case asserting `TracerProvider`/`MeterProvider` resolve.

### Suggested PR 4b — Storage spans, shared by both contexts (2 files) [US1]

- [ ] T024 [US1] Wrap JSON load/save in `Financial.Shared.Infrastructure/Persistence/DebouncedJsonStorage.cs` with `ITelemetryTracer.StartSpan("JsonStorage.Load"/"JsonStorage.Save")` per [contracts/telemetry-semantic-conventions.md](./contracts/telemetry-semantic-conventions.md) — depends on T001, T002
- [ ] T025 [P] [US1] Same for `Financial.Shared.Infrastructure/Persistence/GoogleDriveJsonStorage.cs` (`"GoogleDrive.Upload"`/`"GoogleDrive.Download"`) — depends on T001, T002

### Suggested PR 4c — Jaeger local overlay (1 file) [US1]

- [ ] T026 [US1] Create `docker-compose.observability.yml` with a `jaeger` Compose profile (Jaeger all-in-one, OTLP receiver on 4317, UI on 16686) per [research.md](./research.md) Decision D7 — never referenced by base `docker-compose.yml`

### Suggested PR 4d — CashFlow use-case spans (1 file) [US1]

- [ ] T027 [US1] In `Financial.CashFlow.Infrastructure/DependencyInjection/CashFlowInfrastructureServiceCollectionExtensions.cs`, after `AddFinancialCashFlowApplication()` has registered the real services, decorate each `I*Service` with `services.Decorate<I*Service>("CashFlow")` (T004) — depends on T004, T023

**Checkpoint after PR 4a–4d**: User Story 1's Independent Test is satisfiable end to end for a CashFlow action via Web — this is the MVP.

### Suggested PR 4e — Investment use-case spans, parity (1 file) [US1]

- [ ] T028 [US1] Same as T027 for `Financial.Investment.Infrastructure/DependencyInjection/InvestmentInfrastructureServiceCollectionExtensions.cs` (its Application services registered by `AddFinancialApplication()`) — depends on T004, T023

### Suggested PR 4f — WPF trace root, one representative command (2 files) [US1]

- [ ] T029 [US1] Add an `ITelemetryTracer` constructor dependency to `Financial.App/ViewModels/CashFlow/MonthlyViewModel.cs` and wrap its save-expense command body in `StartSpan("App.MonthlyViewModel.SaveExpense")`, establishing the WPF trace root per FR-004a
- [ ] T030 [US1] Update the `MonthlyViewModel` registration in `Financial.App/App.xaml.cs` to pass `sp.GetRequiredService<ITelemetryTracer>()` — depends on T029

### Suggested PR 4g — Log correlation + top logging-audit fix (3 files) [US1]

- [ ] T031 [US1] Add `Serilog.Sinks.OpenTelemetry` package reference to `Integrations/Observability/Integrations.Observability.csproj` only; add a `WriteToObservability(this LoggerConfiguration, IConfiguration)` extension in `Integrations/Observability/SerilogObservabilityExtensions.cs` that adds trace/span-id enrichment (and OTLP export when enabled) — per [research.md](./research.md) Decision D4
- [ ] T032 [US1] Call `.WriteToObservability(context.Configuration)` from the existing `UseSerilog(...)` configuration in both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` — depends on T031
- [ ] T033 [US1] Log the caught exception (via `ILogger`, `error.type` only, no raw message that embeds a financial value) in `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs` before translating it to a `ProblemDetails` response — logging-audit.md's top remediation priority

### Tests (not counted toward any slice above)

- [ ] T034 [US1] Integration test in `Tests/Financial.Api.Tests/` using an in-memory span-capture `ITelemetryTracer` test double (registered via `ConfigureTestServices`) to assert one correlated trace spans controller → CashFlow `ExpenseService` → `JsonStorage.Save` for a sample request — depends on T027, T024
- [ ] T035 [P] [US1] Unit test in `Tests/Financial.Shared.Infrastructure.Tests/Observability/` asserting `TracingDispatchProxy`'s fixed attribute set never includes a denied FR-014 attribute — depends on T003

**Checkpoint**: User Story 1 fully functional — one connected trace end to end via Jaeger, for both bounded contexts and both clients, with correlated logs and the top logging-audit gap closed.

---

## Phase 5: User Story 3 - Toggle and choose an observability backend via configuration only (Priority: P2)

**Goal**: Switch between Jaeger and Langfuse (or disabled ↔ enabled) using configuration alone, per FR-007/FR-008/SC-003.

### Suggested PR 5a — Backend switch + fail-fast validation (1 file) [US3]

- [ ] T036 [US3] Update `Integrations/Observability/ObservabilityServiceCollectionExtensions.cs`: branch the OTLP exporter configuration on `Backend` (Langfuse Basic Auth header from `PublicKey`/`SecretKey` vs. plain endpoint for Jaeger); throw a clear startup exception for an unrecognized `Backend` or a missing Langfuse key when `Backend=Langfuse` — depends on T023

### Suggested PR 5b — Langfuse local overlay (1 file) [US3]

- [ ] T037 [US3] Add a `langfuse` Compose profile to `docker-compose.observability.yml`, referencing Langfuse's official minimal self-hosted stack with ephemeral/local-only volumes, per [research.md](./research.md) Decision D7 — depends on T026

### Tests (not counted)

- [ ] T038 [P] [US3] Unit test in `Tests/Integrations.Observability.Tests/`: exporter configured with a Basic Auth header when `Backend=Langfuse`, without one when `Backend=Jaeger`; unrecognized `Backend` throws — depends on T036
- [ ] T039 [US3] Run and record [quickstart.md](./quickstart.md) Scenario C (switch `Backend=Jaeger` → `Langfuse`, single restart, no code change) — depends on T036, T037

**Checkpoint**: Both backends usable and swappable purely via configuration.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T040 [P] Integration test in `Tests/Financial.Api.Tests/` for the backend-unreachable edge case (`Observability:Enabled=true`, no container running) — [quickstart.md](./quickstart.md) Scenario D / SC-006 — depends on T023
- [ ] T041 [P] Add an "Observability" section to `README.md` covering local setup, backend choice, and where `logging-audit.md`'s findings live
- [ ] T042 Run `dotnet build --configuration Release`, `dotnet test`, and `docker-compose up` (base file, unchanged) and record results per Constitution Principle VIII
- [ ] T043 [P] Apply remaining `logging-audit.md` remediation not covered by T033: extend `TracingDispatchProxy` (T003) to optionally log use-case entry/success via `ILogger` alongside the span it already creates (one file: `TracingDispatchProxy.cs`), and add retry/fallback logging in `Financial.Shared.Infrastructure/Resilience/TransientRetryPolicy.cs` and `Financial.Investment.Infrastructure/Services/FallbackFinanceService.cs`
- [ ] T044 [P] Instrument the remaining high-value WPF `catch` blocks identified in `logging-audit.md` (`ReservaViewModel`, `ControleMaeViewModel`, `MensaisViewModel`) with `ILogger`, following the pattern established in T029

---

## Suggested PR sequence & file counts (production code/config only — excludes docs and `*Tests*` files)

| PR | Tasks | Files | Story |
|---|---|---|---|
| 1 | T001–T004 | 4 | Setup |
| 2 | T006–T010 | 5 | Foundational |
| 3a | T012–T014 | 3 | US2 |
| 3b | T015–T017 | 3 | US2 |
| 4a | T021–T023 | 3 | US1 |
| 4b | T024–T025 | 2 | US1 |
| 4c | T026 | 1 | US1 |
| 4d | T027 | 1 | US1 — **MVP checkpoint** |
| 4e | T028 | 1 | US1 |
| 4f | T029–T030 | 2 | US1 |
| 4g | T031–T033 | 3 | US1 |
| 5a | T036 | 1 | US3 |
| 5b | T037 | 1 | US3 |
| 6 | T040–T044 | up to 5 (can split further) | Polish |

Every slice is at or under the ~5-file target; most are well under it.

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately
- **Foundational (Phase 2)**: depends on Phase 1 (needs `ITelemetryTracer`/`ITelemetrySpan`)
- **User Story 2 (Phase 3)**: depends on Phase 2 — delivers a complete, deployable increment on its own
- **User Story 1 (Phase 4)**: depends on Phase 2 (for `ITelemetryTracer` resolving) and, for the real-tracing sub-slices (4a+), does not strictly require Phase 3 to have merged first, but Phase 3's composition-root wiring (T013/T016) is a prerequisite for `AddObservability` to be called at all — so Phase 3 should land first in practice
- **User Story 3 (Phase 5)**: depends on Phase 4a (T023)
- **Polish (Phase 6)**: depends on the tasks it references

### Parallel Opportunities

- T001, T002 (Phase 1) in parallel
- T007, T008 (Phase 2) in parallel
- T024, T025 (Phase 4b) touch different files, in parallel
- T040, T041, T043, T044 (Polish) in parallel

---

## Implementation Strategy

### MVP First

1. PR 1 → PR 2 → PR 3a → PR 3b (User Story 2 fully provable, zero OpenTelemetry package in the solution yet)
2. PR 4a → PR 4b → PR 4c → PR 4d (**MVP**: one correlated CashFlow trace via Jaeger)
3. **STOP and VALIDATE**: run [quickstart.md](./quickstart.md) Scenario B for a CashFlow action
4. Continue with 4e (Investment parity), 4f (WPF), 4g (log correlation) as separate small PRs
5. PR 5a/5b (Langfuse), then Polish

### Notes

- Every PR above keeps `Observability:Enabled=false` as the shipped default, so Constitution Principle VIII (deployable after every merge) holds at every single step, not just at phase boundaries.
- Commit after each task or logical group, per this repo's branch-per-feature workflow — one PR per slice per the user's explicit instruction, not one PR per phase.
