---
description: "Task list for Application Observability (revision 3 — Shared.Abstractions + explicit calls)"
---

# Tasks: Application Observability

**Input**: Design documents from `specs/001-application-observability/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md, logging-audit.md (all present)

**Tests**: Included — Constitution Principle V makes unit/integration tests mandatory. xUnit + FluentAssertions, no mocking framework. `ITelemetryTracer`/`ITelemetrySpan` are trivial to fake by hand.

**Organization**: Tasks are grouped by user story, further grouped into **suggested PR slices sized to the user's ~5-source/config-file target** (excluding docs and test files). Revision note: this supersedes a design that used a `TracingDispatchProxy` decorator (fully built, tested, and reverted — see git history on this branch and research.md Decision D1/D3) in favor of a dependency-free `Financial.Shared.Abstractions` project that Application code calls directly.

**Batch-size exception (added after PR #467's review, see spec.md Assumptions)**: the ~5-file target is for heterogeneous changes. Repeatable, single-pattern work — instrumenting many services with the identical `ITelemetryTracer` span-wrapping shape (T031, T034, and their analogues) — may batch up to **8** files per PR instead, since every file gets the same already-approved change and splitting further adds PR overhead without adding review safety.

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

- [X] T012 [US2] Add `ProjectReference` to `Integrations/Observability/Observability.csproj` in `Financial.Api/Financial.Api.csproj`
- [X] T013 [US2] Call `builder.Services.AddObservability(configuration, serviceName: "Financial.Api")` in `Financial.Api/Program.cs` — depends on T010, T012
- [X] T014 [US2] Add the `Observability` section (default `Enabled: false`) to `Financial.Api/appsettings.json`

**Deviation**: `Dockerfile` also required updating (not in the original 3-file estimate) — it didn't `COPY` `Financial.Shared.Abstractions` or `Integrations/Observability`, so `docker-compose up` failed to build once `Financial.Api` referenced them. Fixed as part of this PR since Constitution Principle VIII requires the app stay deployable after every merge.

### Suggested PR 3b — Financial.App wiring (3 files) [US2]

- [X] T015 [US2] Add `ProjectReference` to `Integrations/Observability/Observability.csproj` in `Financial.App/Financial.App.csproj`
- [X] T016 [US2] Call `services.AddObservability(context.Configuration, serviceName: "Financial.App")` in `Financial.App/App.xaml.cs`'s `ConfigureServices` — depends on T010, T015
- [X] T017 [US2] Add the `Observability` section (default `Enabled: false`) to `Financial.App/appsettings.json`

### Tests (not counted)

- [X] T018 [US2] Integration test in `Tests/Financial.Api.Tests/ObservabilityDisabledTests.cs`: app starts and `GET /api/v1/financial/sync-status` succeeds with `Observability:Enabled=false` and no telemetry endpoint reachable — depends on T013. Also asserts `ITelemetryTracer` resolves to a usable no-op from the real DI container. 2/2 passing.
- [X] T019 [P] [US2] Test in `Tests/Financial.Presentation.Tests/DependencyInjection/ObservabilityServiceRegistrationTests.cs`: CashFlow services still resolve and `ITelemetryTracer` resolves to a usable no-op — depends on T016. 2/2 passing.
- [X] T020 [US2] Run and record [quickstart.md](./quickstart.md) Scenario A as the PR's Constitution Principle VIII start-up check — `docker-compose up --build` succeeded (Financial.Api), `GET /api/v1/financial/sync-status` returned 200, `logs/app-*.log` had no telemetry/collector-related errors. For `Financial.App` (WPF, PR 3b), launched the built exe directly (forcing `DOTNET_ENVIRONMENT=Development` + `LocalJson` provider against a temp copy of the data files, since the packaged exe defaults to the `Production` GoogleDrive-backed config which needs real credentials outside this environment) — window opened cleanly, no startup errors in `logs/app-*.log`.

**Checkpoint**: User Story 2 fully and independently satisfied — both `Financial.Api` and `Financial.App` start with observability wired in and disabled by default; `ITelemetryTracer` resolves to a no-op in both composition roots.

---

## Phase 4: User Story 1 - Diagnose a request end to end while it's happening (Priority: P1) 🎯 MVP

**Goal**: Make tracing real, and produce one correlated trace spanning entry point → application use case → storage, via explicit `ITelemetryTracer` calls (research.md Decision D3).

**Independent Test**: Enable observability, start Jaeger, perform one action from a client, find one trace with linked spans for entry point, application service, and storage.

### Suggested PR 4a — Real OpenTelemetry wiring, still produces zero spans elsewhere (3 files) [US1]

- [X] T021 [US1] Add `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime` (all v1.17.0) package references to `Integrations/Observability/Observability.csproj` — the only project in the solution to ever get these references (FR-005/SC-008, verified: `grep -rl OpenTelemetry **/*.csproj` matches only this project)
- [X] T022 [US1] Create `Integrations/Observability/OpenTelemetryTracer.cs` — `ITelemetryTracer`/`ITelemetrySpan` implementation wrapping a `System.Diagnostics.ActivitySource` internally (source name `"Financial"`, exposed as `OpenTelemetryTracer.ActivitySourceName`) — depends on T002, T003, T021. `RecordException` only sets `error.type`/status, never the raw exception message, per the interface contract's rule 4
- [X] T023 [US1] Update `Integrations/Observability/ObservabilityServiceCollectionExtensions.cs`: when `Enabled=true`, register `OpenTelemetryTracer`; wire `.AddOpenTelemetry().WithTracing(...)` (AddSource, AspNetCore + HttpClient instrumentation, OTLP exporter) and `.WithMetrics(...)` (AspNetCore + HttpClient + Runtime instrumentation, OTLP exporter) — depends on T022. Backend-specific auth (Langfuse) is still T043/PR 5a

**Tests (not counted)**: 4 new cases added to `Tests/Financial.Observability.Tests/ObservabilityServiceCollectionExtensionsTests.cs` — `Enabled=false` still resolves `NoOpTelemetryTracer`, `Enabled=true` resolves `OpenTelemetryTracer`, and `StartSpan`/`SetAttribute`/`RecordException`/`Dispose` never throw when enabled even with no OTLP collector reachable (no `ActivityListener` attached in-process). 9/9 passing.

**Manual verification**: `docker-compose up --build` (base file, `Enabled=false` default) — clean startup, no telemetry errors. `dotnet run --project Financial.Api` with `Observability__Enabled=true` and no Jaeger container running — started cleanly, `GET /api/v1/financial/sync-status` returned 200, no telemetry-related errors logged (Scenario D's silent-failure behavior, confirmed early since this PR is what makes the enabled path real).

### Suggested PR 4b — Storage spans, shared by both contexts (3 files) [US1]

- [X] T024 [US1] Add `ProjectReference` to `Financial.Shared.Abstractions` in `Financial.Shared.Infrastructure/Financial.Shared.Infrastructure.csproj` — depends on T001
- [X] T025 [US1] Inject `ITelemetryTracer` into `Financial.Shared.Infrastructure/Persistence/DebouncedJsonStorage.cs` and wrap load/save with `StartSpan("JsonStorage.Load"/"JsonStorage.Save")` per [contracts/telemetry-semantic-conventions.md](./contracts/telemetry-semantic-conventions.md) — depends on T024. `ReadAsync` wraps the pass-through read; `SaveNowAsync` wraps the retried write (the actual save duration, including retries) — the fire-and-forget outer `WriteAsync` isn't spanned since it never blocks on the real write.
- [X] T026 [P] [US1] Same for `Financial.Shared.Infrastructure/Persistence/GoogleDriveJsonStorage.cs` (`"GoogleDrive.Upload"`/`"GoogleDrive.Download"`) — depends on T024

**Deviation (4th file)**: Both classes' `ITelemetryTracer` constructor parameter is optional (`= null`, falling back to a new private `NullTelemetryTracer` in the same folder — not `Integrations/Observability`'s `NoOpTelemetryTracer`, which this project must never reference per the contract). This is the same nullable-optional-dependency idiom already used here for `IRemoteFileClientFactory?`/`TimeProvider?`, and it means all ~20 existing test call sites needed zero changes. `NullTelemetryTracer.cs` is the 4th production file.

**Tests (not counted)**: 6 new cases (3 in `DebouncedJsonStorageTests.cs`, 3 in `GoogleDriveJsonStorageTests.cs`) using a new `Tests/Financial.TestUtilities/RecordingTelemetryTracer.cs` fake, asserting span name, `operation.result`, and `RecordException` on both the success and failure paths. 39/39 passing in `Financial.Shared.Infrastructure.Tests` (was 33).

**Important — not yet wired to a real tracer in production**: `JsonStorageFactory`/`GoogleDriveStorageFactory`/both bounded contexts' `RepositoryFactory` classes and DI extensions still construct these classes without passing a tracer, so they silently fall back to `NullTelemetryTracer` today — mirrors Phase 1/2's "capability built, nothing references it yet" pattern (see their Checkpoints). **This must be threaded through before PR 4d's Independent Test (a trace with a storage span) can pass** — tracked as a new, unplanned follow-up PR immediately after this one (not in the original file-count estimate, same as the Dockerfile fix in PR 3a).

### Follow-up PR 4b-wiring — thread the real tracer through DI (unplanned, 6 files) [US1]

Not in the original task list — required to make PR 4b's spans actually appear in production, since `JsonStorageFactory`/`GoogleDriveStorageFactory`/both `RepositoryFactory` classes/both `AddFinancial*Infrastructure` DI extensions all construct storage without passing the DI-resolved `ITelemetryTracer`.

- [X] Add an optional `ITelemetryTracer? tracer = null` parameter to `GoogleDriveStorageFactory.Create` and `JsonStorageFactory.CreateGoogleDrive`, forwarded into `GoogleDriveJsonStorage`/`DebouncedJsonStorage`
- [X] Add an optional `ITelemetryTracer? tracer = null` constructor parameter to `InvestmentRepositoryFactory` and `CashFlowRepositoryFactory`, forwarded to `JsonStorageFactory.CreateGoogleDrive`
- [X] `InvestmentInfrastructureServiceCollectionExtensions`/`CashFlowInfrastructureServiceCollectionExtensions`: pass `sp.GetService<ITelemetryTracer>()` (nullable — matches the existing `sp.GetService<IRemoteFileClientFactory>()` optional-resolution pattern, so DI setups that never call `AddObservability`, e.g. some test compositions, keep working unchanged)

**Tests (not counted)**: 2 new integration-style cases (`InvestmentRepositoryFactoryTests`/`CashFlowRepositoryFactoryTests`) using `RecordingTelemetryTracer`, proving a real tracer passed into the factory constructor produces both a `"JsonStorage.Save"` and a `"GoogleDrive.Upload"` span after the debounced write eventually flushes — confirms the wiring is genuinely live end-to-end, not just compiling.

**Checkpoint**: Storage spans (`JsonStorage.*`/`GoogleDrive.*`) now appear in real traces whenever `Observability:Enabled=true` and the `GoogleDriveJson` provider is configured, for both bounded contexts. `LocalJson`-provider deployments still produce no storage spans (out of scope per T025/T026's original design — only the debounced/cloud path is wrapped).

### Suggested PR 4c — Jaeger local overlay (1 file) [US1]

- [X] T027 [US1] Create `docker-compose.observability.yml` with a `jaeger` Compose profile per [research.md](./research.md) Decision D7 — never referenced by base `docker-compose.yml`. `app` shares the same `["jaeger"]` profile in this file, so composing the overlay in only changes anything (both the new env vars and `app` itself starting) when `--profile jaeger` is also passed — `docker compose -f docker-compose.yml -f docker-compose.observability.yml up` (no `--profile`) starts nothing from either file, avoiding a half-applied state. Jaeger runs in-memory (no `SPAN_STORAGE_TYPE`/volume set = ephemeral default), no volume declared.
- **Post-merge fix**: `jaegertracing/all-in-one:latest` turned out to resolve to Jaeger v1, which reached end-of-life 2025-12-31 and prints a startup deprecation warning. Switched the image to `jaegertracing/jaeger:latest` (Jaeger v2, the OTel-Collector-based single binary that replaced the three v1 images). No `COLLECTOR_OTLP_ENABLED` env var needed — v2 enables OTLP ingestion by default (that setting was v1-only). Re-verified live: no EOL warning, `Financial.Api` still appears in Jaeger with real traces.

**Manual verification**: `docker compose -f docker-compose.yml -f docker-compose.observability.yml --profile jaeger up --build` — both containers started, `GET /api/v1/financial/sync-status` returned 200 a few times, then `curl http://localhost:16686/api/traces?service=Financial.Api` returned real traces with ASP.NET Core auto-instrumentation spans (`http.route`, `http.request.method`, `http.response.status_code` — no PII/financial values), the first end-to-end confirmation that PR 4a's OTel wiring actually reaches Jaeger. No errors in either container's logs. Also re-verified `docker compose -f docker-compose.yml up` alone (no overlay) still starts and behaves exactly as before (Constitution Principle VIII).

### Suggested PR 4d — CashFlow's first traced use case, MVP checkpoint (2 files) [US1]

- [X] T028 [US1] Add `ProjectReference` to `Financial.Shared.Abstractions` in `Financial.CashFlow.Application/Financial.CashFlow.Application.csproj` — depends on T001
- [X] T029 [US1] Inject `ITelemetryTracer` into `Financial.CashFlow.Application/Services/ExpenseService.cs`'s constructor (required, matching the existing `ILogger<T>` pattern in `CardStatementService`) and wrap **every** public method (`AddExpenseAsync`, `UpdateExpenseAsync`, `DeleteExpenseAsync`, `GetExpensesByMonth`, `GetUnpaidCardChargesByMonth`, `GetCategoryTotalsByMonth` — the real method names; the task's `CreateExpenseAsync` doesn't exist in this codebase) with `StartSpan("CashFlow.ExpenseService.{MethodName}")`, setting `bounded_context`/`entity.type`/`operation.name` on every span and `entity.id`/`operation.result` (+`RecordException`) per outcome, all from the allow-list — depends on T028

**Deviation (test-only, not counted)**: making the tracer a required constructor parameter (not optional-with-fallback like PR 4b's storage classes) broke `Tests/Financial.Presentation.Tests/DependencyInjection/CashFlowServiceRegistrationTests.cs`, whose minimal `ServiceCollection` never called `AddObservability` — a real gap the compiler caught. Fixed by registering a `RecordingTelemetryTracer` there, matching what every real composition root already does. 62 pre-existing `ExpenseServiceTests.cs` call sites needed a single `replace_all` edit (`new ExpenseService(repository)` → `new ExpenseService(repository, Tracer)`) since they all shared one literal pattern — cheap enough that the required-parameter approach (vs. PR 4b's optional-with-fallback) was worth it here for the stronger "always wired, no silent no-op" guarantee, consistent with `ILogger<T>`.

**Tests (not counted)**: 2 new cases in `ExpenseServiceTests.cs` (success path asserts span name/attributes/`entity.id`; a validation failure asserts `operation.result=Failed` + `RecordException`), plus a `Constructor_WithNullTracer_Throws` case. 66/66 passing in `Financial.CashFlow.Application.Tests`.

**Manual verification — MVP checkpoint confirmed live**: ran `Financial.Api` locally (temp copy of the data files, never the live one) with `Observability__Enabled=true` pointed at a Jaeger container from the [PR 4c](#suggested-pr-4c--jaeger-local-overlay-1-file-us1) overlay, `POST /api/v1/financial/expenses`. Jaeger returned **one trace** with two correctly nested spans: `POST /api/v{version:apiVersion}/financial/expenses` (root) → `CashFlow.ExpenseService.AddExpense` (child, `CHILD_OF`), attributes exactly `bounded_context=CashFlow`, `entity.id=<real guid>`, `entity.type=Expense`, `operation.name=AddExpense`, `operation.result=success` — no raw amount, description, or other PII. No storage span (expected — this run used the `LocalJson` provider, which PR 4b intentionally doesn't wrap).

**Checkpoint after PR 4a–4d**: User Story 1's Independent Test is satisfiable end to end for "create an expense" — confirmed live via API. Full 3-level entry-point → service → storage tracing additionally requires the `GoogleDriveJson` provider (not exercised live here — no test credentials available in this environment — but the storage-layer wiring itself was already proven live in PR 4b's factory tests). This is the MVP.

### Suggested PR 4e onward — extend CashFlow coverage (repeatable, ~1 csproj + services per PR; up to 8 files per the exception above) [US1]

- [X] T030 [US1] Repeat T029's pattern for `IncomeService`, `BankService`, `TransferService` in `Financial.CashFlow.Application/Services/` — depends on T028. `BankService`'s entity type is `"Bank"`; `GetBankBalanceAsOf`/`GetTransfersByBank` set `entity.id` to the bank id (never the computed balance). No `ProjectReference` needed (T028 already added it to the shared `Financial.CashFlow.Application.csproj`), so this PR is 3 files (the 3 services), no csproj change.
  - **Deviation (test-only)**: `BalanceAdjustmentServiceTests.cs` also constructs `BankService` directly (a concrete dependency of `BalanceAdjustmentService`, unrelated to this PR) — its 3 call sites needed the same mechanical `tracer` argument added.
  - **Tests (not counted)**: 2 new span-behavior cases (`IncomeService` success path, `TransferService` failure path with `RecordException`) plus 3 new `Constructor_WithNullTracer_Throws` cases. `Financial.CashFlow.Application.Tests` 349/349 passing (was 66 after PR 4d).
- [X] T031 [US1] Repeat for the remaining CashFlow services (~~`ReserveService`~~, ~~`CardStatementService`~~ — extended its existing `ILogger` usage with a span too, ~~`CreditCardService`~~, ~~`CategoryService`~~, ~~`IncomeSourceService`~~, ~~`ReserveBucketService`~~, ~~`InvestmentAccountService`~~, ~~`TitheService`~~, ~~`BalanceAdjustmentService`~~, ~~`ControleMaeService`~~, ~~`MensaisService`~~, ~~`InvestmentSnapshotService`~~, ~~`AnnualSummaryService`~~) — depends on T028. **T031 complete** — every CashFlow Application service is now instrumented.
  - **Batch 1 (4 services, 0 csproj — already added in PR 4d)**: `ReserveService` (`EntityType="ReserveMovement"`, `PostIncomeSplitAsync` sets no `entity.id` since it creates multiple movements at once), `CardStatementService` (kept its existing `ILogger.LogWarning` call alongside the new span; the invoice-period warning string still never becomes a span attribute, per logging-audit.md's existing note), `CreditCardService`, `CategoryService` (had no existing test file — created `CategoryServiceTests.cs`).
  - **Batch 2 (4 services, 0 csproj)**: `IncomeSourceService`, `ReserveBucketService`, `InvestmentAccountService`, `TitheService` — all four are single-query services (one `Get*`/`GetTitheSummary` method each), same pattern.
  - **Batch 3 (5 services, 0 csproj — the full remaining set, per the batch-size exception)**: `BalanceAdjustmentService` (`EntityType="BalanceAdjustment"`, depends on `BankService` — its `GetBankBalanceAsOf` call inside `AddAdjustmentAsync`/`UpdateAdjustmentAsync` produces its own nested span), `ControleMaeService` (`EntityType="MaeLedgerEntry"`), `MensaisService` (`EntityType="RecurringBill"`), `InvestmentSnapshotService` (`EntityType="InvestmentSnapshot"`), `AnnualSummaryService` (`EntityType="AnnualSummary"`, its 5 public methods — `GetCategoryTotalsForYear`, `GetInvestmentAnnualResultForYear`, `GetIncomeSummaryForYear`, `GetCategoryTotalsAnnualForYear`, `GetHistoricSummaryAverageFromYear` — each wrapped without touching any of its many private helper methods; also reordered its constructor to `(repository, tracer, timeProvider = null)` since a required param can't follow an optional one, updating ~6 test call sites that positionally passed a `FakeTimeProvider`).
  - **Tests (not counted)**: `Financial.CashFlow.Application.Tests` 368/368 passing (was 349 after PR 4e; 357 after batch 1; 361 after batch 2) — new `Constructor_WithNullTracer_Throws` per service, plus span-behavior cases for `CategoryService`, `BalanceAdjustmentService`, and `AnnualSummaryService`.

### Suggested PR 4f — Investment's first traced use case, parity (2 files) [US1]

- [ ] T032 [US1] Add `ProjectReference` to `Financial.Shared.Abstractions` in `Financial.Investment.Application/Financial.Investment.Application.csproj` — depends on T001
- [ ] T033 [US1] Inject `ITelemetryTracer` into one representative Investment service (e.g. `AssetPriceService.cs`) and wrap its primary method, same pattern as T029 — depends on T032

### Suggested PR 4g onward — extend Investment coverage (repeatable; up to 8 files per the exception above) [US1]

- [ ] T034 [US1] Repeat T033's pattern for the remaining Investment services — depends on T032. Up to 8 same-pattern services may land together per PR rather than splitting into many small PRs.

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
| 4e+ | T030, T031 | 1 csproj (already added) + services, up to 8/PR (batch-size exception) | US1 (repeatable) |
| 4f | T032–T033 | 2 | US1 |
| 4g+ | T034 | services, up to 8/PR (batch-size exception) | US1 (repeatable) |
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
- Full service-by-service tracing coverage (4e+/4g+) is intentionally open-ended in this document — each such PR follows the exact pattern established by T029/T033, so it doesn't need its own enumerated task per service. Per the batch-size exception, these PRs are not held to the ~5-file target — batch up to 8 same-pattern services together per PR.
