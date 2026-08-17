# Implementation Plan: Application Observability

**Branch**: `feat/application-observability` (spec directory: `001-application-observability`) | **Date**: 2026-08-17 (revised) | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-application-observability/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add an opt-in observability capability (structured logs, runtime metrics, distributed traces) with the OpenTelemetry SDK confined to one new project, `Integrations/Observability`, mirroring exactly how `Integrations/GoogleFinancialSupport` isolates the Google APIs client from the rest of the solution. Every other project reaches tracing only through a first-party interface, `ITelemetryTracer` (defined in `Financial.Shared.Infrastructure/Observability/`, the same project that already hosts the Google Drive storage abstraction `IRemoteFileClientFactory`), never through the OpenTelemetry SDK directly. Structured logs reuse the existing `Microsoft.Extensions.Logging.ILogger<T>` abstraction already used in this codebase (e.g. `CardStatementService`) rather than inventing a new logging interface — OTel-based log export/correlation is wired at the Serilog-sink level, entirely inside `Integrations/Observability`. Metrics are fully automatic (ASP.NET Core/HttpClient/.NET runtime auto-instrumentation only, per FR-015) and need no interface at all. Application-layer "use case" spans (the middle hop the trace must cover, per FR-004) are produced by a generic tracing decorator — implemented with the BCL's `System.Reflection.DispatchProxy` (no new NuGet dependency) — wrapping each bounded context's already-registered Application service interfaces from that context's own Infrastructure project, so **Domain and Application code is not modified at all** by this feature; the isolation goal from the original prompt ("keep observability infrastructure isolated from application business logic") is met as literally as possible. A single config toggle (`Observability:Enabled`) fully disables the capability; `ITelemetryTracer` still resolves when disabled, backed by a no-op implementation (FR-006a), so calling code never branches on whether observability is on.

**⚠️ Flag for review-plan gate**: the decorator/`DispatchProxy` mechanism for Application-layer span creation (Decision D3 in [research.md](./research.md)) is the one genuinely new pattern in this codebase. The alternative — explicit `ITelemetryTracer` calls hand-written inside each Application service — was rejected because Application cannot depend on `Financial.Shared.Infrastructure` (Clean Architecture direction) and duplicating the interface per bounded context is worse than a single reusable decorator. Please confirm this approach (or redirect) before `/speckit-tasks` runs.

## Technical Context

**Language/Version**: C#/.NET 10 (`Financial.Api`, `Financial.App`, `Financial.Shared.Infrastructure`, both bounded contexts' Domain/Application/Infrastructure projects, and the new `Integrations/Observability`). `Financial.Web` is in scope only as the *originator* of traced requests, not as an instrumentation target (unchanged from the original research: the trace root for a Web-originated request is created server-side by ASP.NET Core auto-instrumentation).

**Primary Dependencies**:
- `Integrations/Observability` (new project) only: `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime`, `Serilog.Sinks.OpenTelemetry`.
- `Financial.Shared.Infrastructure`: no new package — `ITelemetryTracer`/`ITelemetrySpan` are plain interfaces; the generic decorator uses `System.Reflection.DispatchProxy`, part of the .NET shared framework since .NET Core 3.0 (no NuGet package needed).
- No other project takes a new dependency.

**Storage**: N/A for application data (no change to the JSON persistence model). Telemetry storage is owned entirely by whichever backend container (Jaeger or Langfuse) is running locally, explicitly ephemeral/container-scoped (FR-009).

**Testing**: xUnit + FluentAssertions (existing convention, no mocking framework). `ITelemetryTracer`/`ITelemetrySpan` are trivial to fake by hand (a `RecordingTelemetryTracer` test double capturing span names/attributes), consistent with `Financial.TestUtilities` conventions — arguably *easier* to test against than the SDK-direct approach from the reverted attempt, since tests no longer need any OpenTelemetry package reference at all outside `Integrations/Observability`'s own test project.

**Target Platform**: Unchanged — `Financial.Api` in a Linux container via Docker, `Financial.App` as a Windows desktop app, `Financial.Web` served as static files by `Financial.Api`. New optional local containers (Jaeger and/or Langfuse) run only when a developer explicitly opts in.

**Project Type**: Existing multi-project .NET solution + SPA; this feature adds one new project (`Integrations/Observability`) following the established `Integrations/<Name>` pattern.

**Performance Goals**: No formal throughput target (single-user personal tool, Constitution Principle IV). When disabled, the no-op `ITelemetrySpan` must be effectively free (a cached singleton no-op instance, no allocation per call). When enabled, telemetry export must not introduce user-perceptible latency in interactive use.

**Constraints**: MUST NOT require Jaeger/Langfuse to be reachable when disabled (FR-002); MUST NOT include raw financial values or PII in any telemetry attribute (FR-014); exactly one backend active at a time (FR-007/FR-008); toggling and backend selection MUST be configuration-only, no rebuild (FR-001); **exactly one project references the OpenTelemetry SDK** (FR-005, verified by SC-008); every other project reaches telemetry only through `ITelemetryTracer`/`ILogger<T>` (FR-006/FR-006a); Domain and Application MUST NOT be modified to add observability (stronger than FR-006 requires, but the natural consequence of the decorator approach — see Decision D3).

**Scale/Scope**: Single developer/operator, low request volume, no high-cardinality metrics or long-term retention needed. In scope: new `Integrations/Observability` project, `Financial.Shared.Infrastructure/Observability/` (interface + decorator infrastructure), `Financial.CashFlow.Infrastructure`/`Financial.Investment.Infrastructure` (decorator wiring only — one call each), `Financial.Api`/`Financial.App` (one composition call each), local docker-compose observability overlay. Explicitly out of scope, and verified by this plan to require **zero changes**: `Financial.CashFlow.Domain`, `Financial.Investment.Domain`, `Financial.CashFlow.Application`, `Financial.Investment.Application`, `Financial.Web`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|---|---|
| I. Clean Architecture, Strictly Layered | PASS — stronger than the reverted attempt. Domain and Application are untouched. `Integrations/Observability` is the only project referencing the OpenTelemetry SDK (FR-005), mirroring `Integrations/GoogleFinancialSupport`'s isolation of the Google APIs client. `ITelemetryTracer` lives in `Financial.Shared.Infrastructure` (the established "shared abstraction layer" per FR-006, same project as `IRemoteFileClientFactory`), consumed only by Infrastructure/Presentation. |
| II. Bounded Context Isolation | PASS. The decorator lives in each context's own Infrastructure project (`Financial.CashFlow.Infrastructure`, `Financial.Investment.Infrastructure`), each independently wired; no reference is introduced between the two contexts. |
| III. WPF/Web Feature Parity | PASS by design. `Financial.App` establishes a WPF trace root via `ITelemetryTracer` directly (Presentation → Shared.Infrastructure is an existing, legal dependency), giving WPF-originated operations the same span coverage as Web-originated ones (FR-004a). |
| IV. Right-Sized Engineering | PASS. `DispatchProxy` is a zero-dependency BCL mechanism, not a new library (Scrutor/Castle DynamicProxy were considered and rejected — see research.md D3). One OTLP exporter path serves both Jaeger and Langfuse (endpoint/headers differ, not the mechanism). |
| V. Test-Backed Changes | PASS (planned). `ITelemetryTracer`/`ITelemetrySpan` are hand-fakeable (no mocking framework needed) — simpler to test than the reverted attempt's direct `ActivitySource`/OTel-SDK-in-tests approach. |
| VI. Evidence-Based, Spec-Driven Change | PASS. `logging-audit.md`'s findings (below) are carried forward unchanged from the original research pass — the codebase hasn't changed in the relevant ways since. |
| VII. Incremental Vertical Delivery | PASS, with a materially different shape than the reverted attempt: the user's explicit ~5-source-file-per-PR target (Assumptions in spec.md) drives `/speckit-tasks` to split what was one "Foundational" phase into several smaller PRs (e.g. interface + no-op decorator scaffolding as PR 1; `Integrations/Observability` project + real implementation as PR 2; Api/App composition wiring as PR 3; CashFlow decorator wiring as PR 4; Investment decorator wiring as PR 5; etc.). |
| VIII. Production Deployability After Every Merge | PASS. Default `Observability:Enabled=false` ships in `appsettings.json`; `ITelemetryTracer` resolves to a no-op regardless, so nothing new is required to start. |

No violations identified. **Complexity Tracking table is not needed** — the one new mechanism (`DispatchProxy`) is a BCL feature, not an added dependency, and is justified in research.md D3.

## Project Structure

### Documentation (this feature)

```text
specs/001-application-observability/
├── plan.md                              # This file (/speckit-plan command output)
├── research.md                          # Phase 0 output (/speckit-plan command)
├── data-model.md                        # Phase 1 output (/speckit-plan command)
├── quickstart.md                        # Phase 1 output (/speckit-plan command)
├── logging-audit.md                     # Phase 1 output — FR-011 evidence-based assessment
├── contracts/                           # Phase 1 output (/speckit-plan command)
│   ├── observability-configuration-contract.md
│   ├── telemetry-tracer-interface-contract.md
│   └── telemetry-semantic-conventions.md
├── checklists/
│   └── requirements.md
└── tasks.md                             # Phase 2 output (/speckit-tasks command — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Integrations/Observability/                       # NEW project — the ONLY project referencing OpenTelemetry
├── Integrations.Observability.csproj              # References Financial.Shared.Infrastructure (for the
│                                                    # interfaces it implements) + OpenTelemetry SDK packages
├── OpenTelemetryTracer.cs                          # ITelemetryTracer implementation, wraps ActivitySource
├── NoOpTelemetryTracer.cs                          # ITelemetryTracer implementation used when disabled
├── ObservabilityOptions.cs                         # Enabled/Backend/Endpoint/Langfuse — self-contained here,
│                                                    # not in Shared.Infrastructure, since only this project reads it
├── ObservabilityBackend.cs                         # enum: Jaeger | Langfuse
└── ObservabilityServiceCollectionExtensions.cs     # AddObservability(configuration, serviceName) — registers
                                                      # ITelemetryTracer (real or no-op), OTel SDK, Serilog OTel sink

Financial.Shared.Infrastructure/Observability/     # NEW folder — the shared abstraction layer, NO OTel reference
├── ITelemetryTracer.cs                             # StartSpan(name) : ITelemetrySpan
├── ITelemetrySpan.cs                               # IDisposable + SetAttribute/RecordException
├── TracingDispatchProxy.cs                         # Generic decorator (System.Reflection.DispatchProxy)
└── ServiceCollectionDecorationExtensions.cs        # Decorate<TInterface>(this IServiceCollection, ITelemetryTracer, ...)

Financial.Api/
├── Financial.Api.csproj                            # MODIFIED — adds ProjectReference to Integrations/Observability
├── Program.cs                                       # MODIFIED — one AddObservability(configuration, "Financial.Api") call
└── appsettings.json                                # MODIFIED — new "Observability" section, Enabled=false default

Financial.App/
├── Financial.App.csproj                            # MODIFIED — adds ProjectReference to Integrations/Observability
├── App.xaml.cs                                      # MODIFIED — one AddObservability(...) call; WPF trace roots use
│                                                     # ITelemetryTracer directly in ViewModel command handlers (FR-004a)
└── appsettings.json                                 # MODIFIED — new "Observability" section, Enabled=false default

Financial.CashFlow.Infrastructure/DependencyInjection/CashFlowInfrastructureServiceCollectionExtensions.cs
                                                      # MODIFIED — decorates each I*Service registered by
                                                      # AddFinancialCashFlowApplication() with the tracing proxy
Financial.Investment.Infrastructure/DependencyInjection/InvestmentInfrastructureServiceCollectionExtensions.cs
                                                      # MODIFIED — same, for Investment's services
Financial.Shared.Infrastructure/Persistence/*.cs     # MODIFIED (JSON load/save, Google Drive I/O) — takes
                                                      # ITelemetryTracer directly (same project, no new dependency)

docker-compose.yml                                   # UNCHANGED — Observability:Enabled=false by default
docker-compose.observability.yml                      # NEW — optional Jaeger/Langfuse overlay via Compose profiles

Tests/Integrations.Observability.Tests/              # NEW test project (mirrors Tests/*.Tests naming)
Tests/Financial.Shared.Infrastructure.Tests/Observability/  # NEW — decorator + interface tests, no OTel package needed
Tests/Financial.Api.Tests/, Tests/Financial.Presentation.Tests/  # MODIFIED as needed

specs/001-application-observability/  # (this directory)

NOT MODIFIED by this feature (verified in Technical Context above):
Financial.CashFlow.Domain/, Financial.Investment.Domain/,
Financial.CashFlow.Application/, Financial.Investment.Application/, Financial.Web/
```

**Structure Decision**: One new project, `Integrations/Observability`, added to `Financial.slnx`, following the existing `Integrations/GoogleFinancialSupport` precedent exactly (own `.csproj`, referenced only by the Presentation composition roots for DI registration, references `Financial.Shared.Infrastructure` to know which interface to implement). The interface (`ITelemetryTracer`) and the reusable decorator (`TracingDispatchProxy`) live in `Financial.Shared.Infrastructure/Observability/` — the same project that already hosts the Google Drive storage abstraction, extending an established pattern rather than inventing a new one. No changes to either bounded context's Domain or Application project, or to `Financial.Web`.

## Complexity Tracking

*No Constitution Check violations — this table is intentionally empty. The one new pattern (`DispatchProxy`-based decoration) is a BCL mechanism, not an added dependency, and is explained in research.md Decision D3 rather than listed here as a tradeoff requiring justification against a simpler alternative — see the plan Summary's flagged note for the alternative that was considered and rejected.*
