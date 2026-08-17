# Implementation Plan: Application Observability

**Branch**: `feat/application-observability` (spec directory: `001-application-observability`) | **Date**: 2026-08-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-application-observability/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add an opt-in observability capability (structured logs, runtime metrics, distributed traces) to the existing .NET backend (`Financial.Api`, `Financial.App`) and both bounded contexts, using the OpenTelemetry .NET SDK as an infrastructure-layer concern isolated from Domain/Application business logic — Application code marks trace boundaries only via the framework-neutral `System.Diagnostics.ActivitySource`/`Activity` BCL types, never the OpenTelemetry SDK itself. A single config toggle (`Observability:Enabled`) fully disables the capability with zero dependency on any collector; when enabled, a single OTLP exporter path routes telemetry to exactly one of two optional, locally containerized backends (Jaeger or Langfuse) selected by configuration. `Financial.Web` requires no direct OpenTelemetry instrumentation — the trace root for a web-originated request is established server-side by ASP.NET Core's own auto-instrumentation at the API entry point, which already satisfies the spec's "web frontend → controller → application service → storage" correlation requirement. The plan also produces a concrete, evidence-based logging audit ([logging-audit.md](./logging-audit.md)) as an input to the structured-logging design, grounded in a full-codebase search performed during this planning pass.

## Technical Context

**Language/Version**: C#/.NET 10 (`Financial.Api`, `Financial.App`, `Financial.Shared.Infrastructure`, both bounded contexts' Domain/Application/Infrastructure projects). TypeScript/React (`Financial.Web`) is in scope only as the *originator* of traced requests, not as an instrumentation target (see [research.md](./research.md) Decision D2).

**Primary Dependencies**: `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime` (new, confined to `Financial.Shared.Infrastructure`); `Serilog.Sinks.OpenTelemetry` or an equivalent trace/span-id enricher (new, alongside the already-present `Serilog.AspNetCore`/`Serilog.Sinks.File`); `System.Diagnostics.DiagnosticSource` (BCL, already implicitly available — no new package needed for Application-layer `ActivitySource` usage).

**Storage**: N/A for application data (no change to the existing single-JSON-document persistence model). Telemetry storage is entirely owned by whichever backend container (Jaeger or Langfuse) is running locally and is explicitly ephemeral/container-scoped (FR-009) — this feature does not persist telemetry itself.

**Testing**: xUnit + FluentAssertions (existing convention, no mocking framework). New tests use an in-memory `ActivityListener`/exporter test double (a hand-written fake, consistent with `Financial.TestUtilities` conventions) to assert parent/child span correlation and attribute redaction without requiring a live Jaeger/Langfuse container.

**Target Platform**: Unchanged — `Financial.Api` in a Linux container via Docker, `Financial.App` as a Windows desktop app, `Financial.Web` served as static files by `Financial.Api`. New optional local containers (Jaeger and/or Langfuse) run only when a developer explicitly opts in.

**Project Type**: Existing multi-project .NET solution + SPA; this feature is a cross-cutting infrastructure capability layered onto that existing structure, not a new project type.

**Performance Goals**: No formal throughput target — this is a single-user personal tool (Constitution Principle IV). When disabled, overhead must be negligible (`ActivitySource.StartActivity()` returns `null` at effectively zero cost when no listener is registered — documented .NET behavior). When enabled, telemetry export must not introduce user-perceptible latency in interactive use.

**Constraints**: MUST NOT require Jaeger/Langfuse to be reachable when disabled (FR-002); MUST NOT include raw financial values or PII in any telemetry attribute (FR-014); exactly one backend active at a time (FR-007/FR-008); toggling and backend selection MUST be configuration-only, no rebuild (FR-001); Domain/Application MUST NOT take a compile-time dependency on the OpenTelemetry SDK (FR-006).

**Scale/Scope**: Single developer/operator, low request volume, no high-cardinality metrics or long-term retention needed. In scope: `Financial.Api`, `Financial.App`, `Financial.Shared.Infrastructure`, both bounded contexts' Application/Infrastructure layers, local docker-compose observability overlay. Out of scope: any direct instrumentation of `Financial.Web` (see Decision D2), business-domain metrics (FR-015), and any change to the JSON persistence model itself.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|---|---|
| I. Clean Architecture, Strictly Layered | PASS. OpenTelemetry SDK registration is confined to `Financial.Shared.Infrastructure/Observability/` (new) and the two Presentation composition roots (`Financial.Api/Program.cs`, `Financial.App/App.xaml.cs`). Domain/Application only ever touch `System.Diagnostics.ActivitySource`/`Activity` (BCL, not a vendor SDK), which is exactly what FR-005/FR-006 require. |
| II. Bounded Context Isolation | PASS. Shared plumbing lives in `Financial.Shared.Infrastructure` (already the shared home for cross-cutting infra, e.g. Google Drive storage). Each bounded context gets its own named `ActivitySource` (`Financial.CashFlow`, `Financial.Investment`) so instrumentation does not create a new cross-context reference. |
| III. WPF/Web Feature Parity | PASS by design. FR-004a requires WPF get equal tracing treatment; the plan establishes a trace root in the WPF command/ViewModel layer mirroring ASP.NET Core's auto-instrumented root for Web, rather than treating WPF as a second-class client. |
| IV. Right-Sized Engineering | PASS. One OTLP exporter code path serves both Jaeger and Langfuse (endpoint/headers differ, not the export mechanism) — no vendor-specific exporter branching. The enable/disable toggle is a single bool; no plugin architecture or feature-flag framework is introduced. |
| V. Test-Backed Changes | PASS (planned). Unit tests cover the `AddFinancialObservability` DI extension (enabled vs. disabled wiring); an integration test uses an in-memory span exporter to assert one correlated trace across controller → application service → storage, using hand-written fakes, no mocking framework — matching existing test conventions. |
| VI. Evidence-Based, Spec-Driven Change | PASS. The logging-audit deliverable (FR-011) is grounded in an actual full-codebase search performed in this planning pass (see [logging-audit.md](./logging-audit.md)), not an invented assessment. |
| VII. Incremental Vertical Delivery | PASS (deferred to `/speckit-tasks` for exact slice boundaries). The design anticipates independently shippable slices, e.g.: (a) disabled-by-default config/toggle scaffold, (b) CashFlow tracing slice (Api → Application → Storage), (c) Investment tracing slice, (d) WPF tracing slice, (e) metrics, (f) Serilog trace/log correlation, (g) Jaeger docker-compose overlay, (h) Langfuse docker-compose overlay — each a complete, reviewable, working unit. |
| VIII. Production Deployability After Every Merge | PASS. Shipped default is `Observability:Enabled=false` in `appsettings.json`; the base `docker-compose.yml` gets no new required service. New NuGet dependencies increase build size only — they introduce no new runtime requirement for the default (disabled) path. |

No violations identified. **Complexity Tracking table is not needed.**

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
│   └── telemetry-semantic-conventions.md
├── checklists/
│   └── requirements.md
└── tasks.md                             # Phase 2 output (/speckit-tasks command — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Financial.Shared.Infrastructure/
└── Observability/                        # NEW — infra/integration concern, mirrors Persistence/GoogleDrive*
    ├── ObservabilityOptions.cs           # Enabled, Backend, Endpoint, ServiceName, Langfuse keys
    ├── ObservabilityBackend.cs           # enum: Jaeger | Langfuse
    └── ServiceCollectionExtensions.cs    # AddFinancialObservability(IServiceCollection, IConfiguration, serviceName)

Financial.Api/
├── Program.cs                            # MODIFIED — calls AddFinancialObservability; no business logic added
└── appsettings.json                      # MODIFIED — new "Observability" section, Enabled=false default

Financial.App/
└── App.xaml.cs                           # MODIFIED — same AddFinancialObservability call via the existing generic host

Financial.CashFlow.Application/
Financial.Investment.Application/
└── (existing use-case services)          # MODIFIED — start Activities via a shared per-context ActivitySource at
                                            # use-case boundaries; add the missing logging identified by
                                            # logging-audit.md; no OTel SDK reference added

Financial.CashFlow.Infrastructure/
Financial.Investment.Infrastructure/
Financial.Shared.Infrastructure/Persistence/
└── (existing repositories / JSON storage) # MODIFIED — wrap storage read/write in Activities

docker-compose.yml                         # UNCHANGED — Observability:Enabled=false by default, no new service
docker-compose.observability.yml            # NEW — optional overlay providing Jaeger (all-in-one) and/or a
                                              # Langfuse local stack, started explicitly via Compose profiles;
                                              # never required for the base `docker-compose up` path

Tests/Financial.Shared.Infrastructure.Tests/Observability/  # NEW — unit tests for AddFinancialObservability
Tests/Financial.Api.Tests/                                   # MODIFIED — integration test asserting one
                                                                # correlated trace across controller → service → storage
Tests/Financial.CashFlow.*.Tests/, Financial.Investment.*.Tests/  # MODIFIED as needed for new Activities/logging
```

**Structure Decision**: Reuse the existing solution layout; the only new project-level surface is the `Observability/` folder inside the already-existing `Financial.Shared.Infrastructure` project — the designated home for cross-cutting infra shared by both bounded contexts, matching the Google Drive precedent the spec itself names — plus one new root-level docker-compose overlay file for optional local observability infrastructure. No new projects, and no structural changes to `Financial.Web` (see [research.md](./research.md) Decision D2).

## Complexity Tracking

*No Constitution Check violations — this table is intentionally empty.*
