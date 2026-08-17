---
description: "Task list for Application Observability"
---

# Tasks: Application Observability

**Input**: Design documents from `specs/001-application-observability/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md, logging-audit.md (all present)

**Tests**: Included — Constitution Principle V ("Test-Backed Changes") makes unit/integration tests mandatory for every feature in this repository, not optional. Tests follow the existing convention: xUnit + FluentAssertions, no mocking framework, hand-written fakes/test doubles (`Financial.TestUtilities` and an in-memory `ActivityListener`/span-capture double for this feature).

**Organization**: Tasks are grouped by user story (spec.md's User Stories 1–4) to enable independent implementation and testing of each story, per Constitution Principle VII (Incremental Vertical Delivery).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Every task includes exact file paths

## User Story 4 — already satisfied

**User Story 4** ("Assess whether existing logging is fit for purpose", P3) is already complete: [logging-audit.md](./logging-audit.md) was produced during planning from an actual solution-wide code search (not an estimate) and satisfies FR-011/SC-005 as a standalone deliverable, independent of whether tracing has shipped yet (per its own Independent Test). No new tasks are needed for US4 itself; its findings are consumed by US1's tasks below (T019, T020, T032) as the prioritized remediation list.

---

## Phase 1: Setup

**Purpose**: Add the OpenTelemetry/Serilog package surface and the empty `Observability` module shape, before any wiring happens.

- [X] T001 Add `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime` package references to `Financial.Shared.Infrastructure/Financial.Shared.Infrastructure.csproj`
- [X] T002 [P] Add `Serilog.Sinks.OpenTelemetry` package reference to `Financial.Api/Financial.Api.csproj` and `Financial.App/Financial.App.csproj`
- [X] T003 [P] Create `Financial.Shared.Infrastructure/Observability/ObservabilityOptions.cs` and `Financial.Shared.Infrastructure/Observability/ObservabilityBackend.cs` (enum `Jaeger`/`Langfuse`) per [data-model.md](./data-model.md) and [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The core disabled-by-default plumbing every user story builds on. This phase's own output already satisfies User Story 2's requirements end-to-end (a config-gated, zero-dependency-when-off toggle) — Phase 3 below adds the tests that prove it.

**⚠️ CRITICAL**: No user story implementation task begins until this phase is complete.

- [X] T004 Implement `AddFinancialObservability(this IServiceCollection services, IConfiguration configuration, string serviceName)` in `Financial.Shared.Infrastructure/Observability/ObservabilityServiceCollectionExtensions.cs` (deviation: named `ObservabilityServiceCollectionExtensions.cs`, matching this repo's `<Feature>ServiceCollectionExtensions` convention rather than the generic `ServiceCollectionExtensions.cs`) — binds `ObservabilityOptions` from the `Observability` config section and returns immediately (no `AddOpenTelemetry()` call, no exporter, no background thread) when `Enabled=false`, per [research.md](./research.md) Decision D5
- [X] T005 In the same file, when `Enabled=true`, wires `.AddOpenTelemetry().WithTracing(...)` (AspNetCore + HttpClient instrumentation, `AddSource` for every `ActivitySource` name via the new `ObservabilitySourceNames` constants, OTLP exporter at `Observability:Endpoint`) and `.WithMetrics(...)` (AspNetCore + HttpClient + Runtime instrumentation, same OTLP exporter) — depends on T004
- [X] T006 [P] Declared the four ActivitySource holders (deviation: one `static readonly ActivitySource` field per project, each named `<Context>ActivitySource`, rather than bare fields, to give each a clear owning type): `CashFlowActivitySource` in `Financial.CashFlow.Application/Observability/`, `InvestmentActivitySource` in `Financial.Investment.Application/Observability/`, `SharedInfrastructureActivitySource` in `Financial.Shared.Infrastructure/Observability/` (deviation: placed alongside the other Observability types rather than under `Persistence/`, since it is not itself persistence code), `AppActivitySource` in `Financial.App/Observability/`
- [X] T007 [P] Added the `Observability` section (default `Enabled: false`, per [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md)) to `Financial.Api/appsettings.json` and `Financial.App/appsettings.json`
- [X] T008 Called `builder.Services.AddFinancialObservability(configuration, serviceName: "Financial.Api")` in `Financial.Api/Program.cs`, placed alongside the other `Add*` composition calls — depends on T004, T007
- [X] T009 Called `services.AddFinancialObservability(context.Configuration, serviceName: "Financial.App")` in `Financial.App/App.xaml.cs`'s `ConfigureServices`, mirroring T008 — depends on T004, T007
- [X] T010 [P] Unit tests in `Tests/Financial.Shared.Infrastructure.Tests/Observability/ObservabilityServiceCollectionExtensionsTests.cs`: assert no `TracerProvider`/`MeterProvider` is resolvable when `Enabled=false` (including when the `Observability` section is absent entirely), and both are resolvable when `Enabled=true` with a valid `Endpoint` — depends on T004, T005. 3/3 passing.

**Checkpoint**: Foundation ready. The disabled path (`Observability:Enabled=false`, the shipped default) is now fully implemented — Phase 3 verifies it satisfies User Story 2.

**Post-review addition**: the architecture-reviewer flagged that `CashFlowActivitySource.Name`/`InvestmentActivitySource.Name`/`AppActivitySource.Name` intentionally hardcode their own literal (per D1's decoupling) with nothing enforcing they stay equal to `ObservabilitySourceNames.{CashFlow,Investment,App}`, which is what `AddSource(...)` actually subscribes to — a drift here silently drops that context's spans with no compile error. Added `Tests/Financial.Presentation.Tests/DependencyInjection/ObservabilitySourceNameConsistencyTests.cs` as a guard rail (3/3 passing) before this phase's commit.

---

## Phase 3: User Story 2 - Run the application with observability fully disabled (Priority: P1)

**Goal**: Prove the Phase 2 foundation actually delivers US2: the app starts and every existing feature works with zero dependency on Jaeger/Langfuse when disabled.

**Independent Test**: Set `Observability:Enabled=false`, ensure no observability container is running, start the app via the standard production path (`docker-compose up`), and exercise existing features — no errors, no degraded behavior.

- [X] T011 [US2] Integration test `Tests/Financial.Api.Tests/ObservabilityDisabledTests.cs` (`WebApplicationFactory` via the existing `ApiTestFactory`) asserting the API starts and `GET /api/v1/financial/sync-status` succeeds with `Observability:Enabled=false` and no telemetry endpoint reachable, and that no `TracerProvider`/`MeterProvider` is registered — depends on T008. 2/2 passing.
- [X] T012 [P] [US2] Test `Tests/Financial.Presentation.Tests/DependencyInjection/ObservabilityServiceRegistrationTests.cs` (deviation: follows this repo's existing `CashFlowServiceRegistrationTests.cs` pattern of building the composition root's `IServiceCollection` directly rather than instantiating the WPF `App`/`AppHost`, since that's the established, UI-free way this project tests WPF DI wiring) asserting CashFlow services still resolve and no `TracerProvider`/`MeterProvider` is registered with `Observability:Enabled=false` — depends on T009. 1/1 passing.
- [X] T013 [US2] Ran [quickstart.md](./quickstart.md) Scenario A: `docker compose build` + `docker compose up -d` (base `docker-compose.yml` only, no observability overlay, no Jaeger/Langfuse container running) — `GET /api/v1/financial/sync-status` returned `HTTP 200`; container stopped with `docker compose down`. Documented as this PR's Constitution Principle VIII start-up check.

**Checkpoint**: User Story 2 is independently verified — the disabled path is proven safe for day-to-day use.

---

## Phase 4: User Story 1 - Diagnose a request end to end while it's happening (Priority: P1) 🎯 MVP

**Goal**: With observability enabled and Jaeger running locally, one user action (from Web or WPF) produces a single correlated trace spanning entry point → application use case → storage, with correlated structured logs, and closes the highest-priority gaps from `logging-audit.md`.

**Independent Test**: Enable observability, start Jaeger, perform one action from a client, find one trace in the Jaeger UI containing linked spans for the entry point, the application service, and the storage operation.

### Implementation for User Story 1

- [ ] T014 [P] [US1] Start an `Activity` from the `Financial.CashFlow` `ActivitySource` at the start of each public use-case method in `Financial.CashFlow.Application/Services/*.cs`, tagging `entity.type`/`operation.name`/`operation.result`/`bounded_context` per the allow-list in [contracts/telemetry-semantic-conventions.md](./contracts/telemetry-semantic-conventions.md) — depends on T006
- [ ] T015 [P] [US1] Start an `Activity` from the `Financial.Investment` `ActivitySource` at the start of each public use-case method in `Financial.Investment.Application/Services/*.cs`, same convention as T014 — depends on T006
- [ ] T016 [P] [US1] Start an `Activity` from the `Financial.Shared.Infrastructure` `ActivitySource` around JSON load/save in `Financial.Shared.Infrastructure/Persistence/*.cs` (including `DebouncedJsonStorage`, `GoogleDriveJsonStorage`) — depends on T006
- [ ] T017 [US1] Start an `Activity` from the `Financial.App` `ActivitySource` at the beginning of each ViewModel command handler in `Financial.App/ViewModels/**/*.cs`, establishing the WPF trace root per FR-004a — depends on T006
- [ ] T018 [US1] Add trace/span-id log enrichment (via `Serilog.Sinks.OpenTelemetry` or an enricher) to the existing `UseSerilog(...)` configuration in both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs`, per [research.md](./research.md) Decision D4 — depends on T002
- [ ] T019 [US1] Log the caught exception (with its `error.type`, no raw message that embeds a financial value) in `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs` before translating it to a `ProblemDetails` response — the top remediation priority from `logging-audit.md`
- [ ] T020 [P] [US1] Add `ILogger`-based logging to the WPF `catch (Exception ex)` blocks in `Financial.App/ViewModels/CashFlow/MonthlyViewModel.cs`, `ReservaViewModel.cs`, `ControleMaeViewModel.cs`, and `MensaisViewModel.cs` before the existing `MessageBox.Show(...)` call, per `logging-audit.md`'s second-highest-value gap
- [ ] T021 [US1] Create `docker-compose.observability.yml` at the repo root with a `jaeger` Compose profile (Jaeger all-in-one image, OTLP gRPC receiver on 4317, UI on 16686), per [research.md](./research.md) Decision D7 — never referenced by the base `docker-compose.yml`

### Tests for User Story 1

- [ ] T022 [US1] Integration test in `Tests/Financial.Api.Tests/` using an in-memory `ActivityListener`-based span-capture test double to assert one correlated trace spans controller → CashFlow application service → JSON storage for a sample request (e.g. create an expense), per [quickstart.md](./quickstart.md) Scenario B and [data-model.md](./data-model.md)'s Trace/Span validation rule — depends on T014, T016, T018
- [ ] T023 [P] [US1] Unit test in `Tests/Financial.Shared.Infrastructure.Tests/Observability/` asserting that captured span/log attributes for a sample instrumented operation never include a denied FR-014 attribute (amount, balance, account holder name, credentials) — depends on T014, T016

**Checkpoint**: User Story 1 fully functional — a developer can view one connected trace end to end via Jaeger for either client, with correlated logs and the top logging-audit gaps closed.

---

## Phase 5: User Story 3 - Toggle and choose an observability backend via configuration only (Priority: P2)

**Goal**: Switch between Jaeger and Langfuse (or disabled ↔ enabled) using configuration alone, with no code change, per FR-007/FR-008/SC-003.

**Independent Test**: With the app stopped, change only `Observability:Backend` from `Jaeger` to `Langfuse` (plus its keys) and restart — telemetry now appears in Langfuse instead, with zero source changes.

- [ ] T024 [US3] Implement the `Backend`-driven OTLP exporter configuration switch (Langfuse Basic Auth header built from `PublicKey`/`SecretKey`, vs. plain endpoint for Jaeger) in `Financial.Shared.Infrastructure/Observability/ServiceCollectionExtensions.cs`, per [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md) — depends on T005
- [ ] T025 [US3] Add fail-fast validation in the same file: an unrecognized `Observability:Backend` value (or a missing Langfuse key when `Backend=Langfuse`) throws a clear startup exception, per [data-model.md](./data-model.md)'s validation rule — depends on T024
- [ ] T026 [US3] Add a `langfuse` Compose profile to `docker-compose.observability.yml`, referencing Langfuse's official minimal self-hosted stack with ephemeral/local-only volumes, per [research.md](./research.md) Decision D7 — depends on T021
- [ ] T027 [P] [US3] Unit test in `Tests/Financial.Shared.Infrastructure.Tests/Observability/` asserting the exporter is configured with a Basic Auth header when `Backend=Langfuse` and without one when `Backend=Jaeger` — depends on T024
- [ ] T028 [US3] Run and record [quickstart.md](./quickstart.md) Scenario C (switch `Backend=Jaeger` → `Langfuse` with a single restart, no code change) as a documented verification in the PR — depends on T024, T026

**Checkpoint**: Both backends are usable and swappable purely via configuration.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Resiliency, documentation, and remaining `logging-audit.md` remediation not already covered by US1.

- [ ] T029 [P] Integration test in `Tests/Financial.Api.Tests/` for the backend-unreachable edge case: `Observability:Enabled=true`, no container running, app still starts and serves requests without delay — [quickstart.md](./quickstart.md) Scenario D / SC-006 — depends on T008
- [ ] T030 [P] Add an "Observability" section to `README.md` covering how to enable it locally, choose a backend, and where `logging-audit.md`'s findings live — depends on T021, T026
- [ ] T031 Run `dotnet build --configuration Release`, `dotnet test`, and `docker-compose up` (base file, unchanged) and record the results in the PR description per Constitution Principle VIII — depends on all prior tasks
- [ ] T032 [P] Apply the remaining `logging-audit.md` remediation items not already covered by T019/T020 — use-case start/success logging in Application services (priority 2) and retry/fallback engagement logging in `TransientRetryPolicy`/`FallbackFinanceService` (priority 3), across both bounded contexts — depends on T014, T015

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories
- **User Story 2 (Phase 3)**: depends on Foundational only — can run before or in parallel with US1's implementation tasks, since it only *tests* what Phase 2 already built
- **User Story 1 (Phase 4)**: depends on Foundational — this is the MVP slice
- **User Story 3 (Phase 5)**: depends on Foundational (specifically T005); does not depend on US1's per-service instrumentation tasks, but is easiest to verify meaningfully once US1's trace pipeline exists
- **Polish (Phase 6)**: depends on the user stories it touches (see per-task dependencies above)

### User Story Dependencies

- **User Story 2 (P1)**: independent of US1/US3 — only exercises Phase 2's disabled-path behavior
- **User Story 1 (P1)**: independent of US2/US3 — the MVP; delivers value on its own once Phase 2 + Jaeger overlay (T021) exist
- **User Story 3 (P2)**: builds on Phase 2's exporter wiring (T005); independently testable once T024–T026 land, regardless of whether US1's per-service instrumentation (T014–T017) is complete, since the exporter switch is orthogonal to which spans exist

### Parallel Opportunities

- T001–T003 (Setup) can all run in parallel
- T006, T007 (Foundational) can run in parallel with each other, but both must precede T008/T009
- T014, T015, T016 (per-project instrumentation in US1) touch disjoint files and can run in parallel
- T020, T023 (US1) can run in parallel with the above
- T027 (US3) can run in parallel with T026
- T029, T030, T032 (Polish) can run in parallel

---

## Parallel Example: User Story 1

```bash
# Once Foundational (Phase 2) is complete, these touch disjoint files:
Task: "Start an Activity from the Financial.CashFlow ActivitySource in Financial.CashFlow.Application/Services/*.cs"
Task: "Start an Activity from the Financial.Investment ActivitySource in Financial.Investment.Application/Services/*.cs"
Task: "Start an Activity from the Financial.Shared.Infrastructure ActivitySource in Financial.Shared.Infrastructure/Persistence/*.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 + its Foundational dependency)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (this alone already delivers User Story 2 — validate it via Phase 3 before moving on, since it's the cheapest, highest-safety checkpoint)
3. Complete Phase 4: User Story 1 (the MVP: one correlated trace, end to end, via Jaeger)
4. **STOP and VALIDATE**: run [quickstart.md](./quickstart.md) Scenario B
5. Deploy/demo if ready — US1 + US2 together are a complete, deployable, reviewable increment

### Incremental Delivery

1. Setup + Foundational → Foundation ready, US2 already provable
2. Add User Story 1 → validate independently → deploy/demo (MVP)
3. Add User Story 3 → validate independently (Langfuse swap) → deploy/demo
4. Polish (resiliency test, docs, remaining logging-audit remediation) → final deploy

### Notes

- [P] tasks touch different files with no unmet dependency
- Commit after each task or logical group, per this repo's branch-per-feature workflow
- Every increment above must independently satisfy Constitution Principle VIII (build/test/start-up under `docker-compose up`) before merging — the default `Observability:Enabled=false` guarantees this for every phase, since no phase changes that default
