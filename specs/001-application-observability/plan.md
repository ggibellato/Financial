# Implementation Plan: Application Observability

**Branch**: `feat/application-observability` (spec directory: `001-application-observability`) | **Date**: 2026-08-17 (revision 3) | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-application-observability/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add an opt-in observability capability (structured logs, runtime metrics, distributed traces) with the OpenTelemetry SDK confined to one new project, `Integrations/Observability`, mirroring exactly how `Integrations/GoogleFinancialSupport` isolates the Google APIs client. The first-party interface everything else depends on, `ITelemetryTracer`/`ITelemetrySpan`, lives in a **new, dependency-free `Financial.Shared.Abstractions` project** — a "shared kernel" of pure contracts (no framework/package references at all) that sits parallel to Domain in the dependency graph, so **Application code can reference it directly** without breaking Clean Architecture's one-way dependency rule. Application services call `ITelemetryTracer.StartSpan(...)` explicitly at meaningful points — no reflection-based decorator, no dynamic proxy. Structured logs reuse the existing `Microsoft.Extensions.Logging.ILogger<T>` abstraction already used in this codebase; OTel-based log export/correlation is wired at the Serilog-sink level, entirely inside `Integrations/Observability`. Metrics are fully automatic (ASP.NET Core/HttpClient/.NET runtime auto-instrumentation only, per FR-015) and need no interface at all. A single config toggle (`Observability:Enabled`) fully disables the capability; `ITelemetryTracer` still resolves when disabled, backed by a no-op implementation (FR-006a).

**Revision note**: this supersedes a second attempt (research.md history below) that used a `System.Reflection.DispatchProxy`-based decorator to avoid touching Application code, since `ITelemetryTracer` originally lived in `Financial.Shared.Infrastructure` (an Infrastructure-layer project Application can't depend on). That attempt was fully built, tested, and architecture-reviewed (see git history on this branch), then reverted once the user asked how the decorator worked and preferred moving the interface to a dependency-free shared project instead — see [research.md](./research.md) Decision D3 for the full comparison.

## Technical Context

**Language/Version**: C#/.NET 10 (`Financial.Api`, `Financial.App`, `Financial.Shared.Abstractions`, `Financial.Shared.Infrastructure`, both bounded contexts' Domain/Application/Infrastructure projects, and the new `Integrations/Observability`). `Financial.Web` is in scope only as the *originator* of traced requests, not as an instrumentation target.

**Primary Dependencies**:
- `Financial.Shared.Abstractions` (new project): **zero** package references — pure C# interfaces/constants.
- `Integrations/Observability` (new project) only: `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime`, `Serilog.Sinks.OpenTelemetry`.
- No other project takes a new package dependency; `Financial.CashFlow.Application`, `Financial.Investment.Application`, `Financial.Shared.Infrastructure`, `Financial.Api`, and `Financial.App` each take a new **project reference** to `Financial.Shared.Abstractions` only.

**Storage**: N/A for application data (no change to the JSON persistence model). Telemetry storage is owned entirely by whichever backend container (Jaeger or Langfuse) is running locally, explicitly ephemeral/container-scoped (FR-009).

**Testing**: xUnit + FluentAssertions (existing convention, no mocking framework). `ITelemetryTracer`/`ITelemetrySpan` are trivial to fake by hand (a `RecordingTelemetryTracer` test double).

**Target Platform**: Unchanged — `Financial.Api` in a Linux container via Docker, `Financial.App` as a Windows desktop app, `Financial.Web` served as static files by `Financial.Api`. New optional local containers (Jaeger and/or Langfuse) run only when a developer explicitly opts in.

**Project Type**: Existing multi-project .NET solution + SPA; this feature adds two new projects (`Financial.Shared.Abstractions`, `Integrations/Observability`).

**Performance Goals**: No formal throughput target (single-user personal tool, Constitution Principle IV). When disabled, the no-op `ITelemetrySpan` must be effectively free (a cached singleton no-op instance, no allocation per call). When enabled, telemetry export must not introduce user-perceptible latency in interactive use.

**Constraints**: MUST NOT require Jaeger/Langfuse to be reachable when disabled (FR-002); MUST NOT include raw financial values or PII in any telemetry attribute (FR-014); exactly one backend active at a time (FR-007/FR-008); toggling and backend selection MUST be configuration-only, no rebuild (FR-001); **exactly one project references the OpenTelemetry SDK** (FR-005, verified by SC-008); every other project reaches telemetry only through `ITelemetryTracer`/`ILogger<T>` (FR-006/FR-006a); `Financial.Shared.Abstractions` MUST have zero project/package dependencies of its own (it's the dependency-free floor of the graph, alongside Domain).

**Scale/Scope**: Single developer/operator, low request volume, no high-cardinality metrics or long-term retention needed. In scope: new `Financial.Shared.Abstractions` and `Integrations/Observability` projects, both bounded contexts' Application projects (explicit `StartSpan` calls at use-case boundaries), `Financial.Shared.Infrastructure` (storage spans), `Financial.Api`/`Financial.App` (composition + WPF trace root/middleware). Explicitly out of scope: either Domain project, `Financial.Web`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|---|---|
| I. Clean Architecture, Strictly Layered | PASS. `Financial.Shared.Abstractions` is a new, dependency-free project — Application depending on it is analogous to Application depending on Domain (both are "depend on a pure abstraction below/beside you," not "depend on Infrastructure"). This is a **new allowed dependency edge** that `Financial.Architecture.Tests` must be extended to document/verify (see Phase 1). `Integrations/Observability` remains the only project referencing the OpenTelemetry SDK. |
| II. Bounded Context Isolation | PASS. Both bounded contexts' Application projects reference the same `Financial.Shared.Abstractions`, but that project has zero knowledge of either context — it's a pure, context-agnostic contract, not a cross-context reference. |
| III. WPF/Web Feature Parity | PASS by design. `Financial.App` calls `ITelemetryTracer` directly (via `Financial.Shared.Abstractions`) to establish a WPF trace root, giving WPF-originated operations the same span coverage as Web-originated ones (FR-004a). |
| IV. Right-Sized Engineering | PASS, and simpler than the reverted decorator attempt: no reflection, no dynamic proxy, no `System.Reflection.DispatchProxy` — just an interface call, which is *more* aligned with "no defensive abstractions for hypothetical future requirements" and easier to learn from (the feature's stated goal). |
| V. Test-Backed Changes | PASS (planned). `ITelemetryTracer`/`ITelemetrySpan` are hand-fakeable (no mocking framework needed); Application-layer tests can assert a fake tracer recorded the expected span without any OpenTelemetry package reference. |
| VI. Evidence-Based, Spec-Driven Change | PASS. `logging-audit.md`'s findings are carried forward unchanged. |
| VII. Incremental Vertical Delivery | PASS. The ~5-source-file-per-PR target still drives `/speckit-tasks`' phase breakdown — if anything, explicit calls make this easier to slice (one bounded context, or even one service, per PR) than the decorator did. |
| VIII. Production Deployability After Every Merge | PASS. Default `Observability:Enabled=false` ships in `appsettings.json`; `ITelemetryTracer` resolves to a no-op regardless. |

No violations identified, **with one required follow-up**: `Financial.Architecture.Tests` needs a new test class asserting `Financial.Shared.Abstractions` has zero project dependencies (keeping it a true dependency-free floor), tracked as part of Phase 1/the first implementation task. This is a documentation/enforcement addition, not a constitution violation.

## Project Structure

### Documentation (this feature)

```text
specs/001-application-observability/
├── plan.md                              # This file
├── research.md                          # Phase 0 output
├── data-model.md                        # Phase 1 output
├── quickstart.md                        # Phase 1 output
├── logging-audit.md                     # Phase 1 output — FR-011 evidence-based assessment
├── contracts/
│   ├── observability-configuration-contract.md
│   ├── telemetry-tracer-interface-contract.md
│   └── telemetry-semantic-conventions.md
├── checklists/
│   └── requirements.md
└── tasks.md                             # Phase 2 output (/speckit-tasks) — NOT created by /speckit-plan
```

### Source Code (repository root)

```text
Financial.Shared.Abstractions/                     # NEW — pure contracts, zero dependencies
├── Financial.Shared.Abstractions.csproj            # No PackageReference, no ProjectReference at all
├── ITelemetryTracer.cs
├── ITelemetrySpan.cs
└── TelemetryAttributeKeys.cs                       # + TelemetryOperationResults

Integrations/Observability/                         # NEW — the ONLY project referencing OpenTelemetry
├── Integrations.Observability.csproj                # References Financial.Shared.Abstractions + OTel SDK packages
├── OpenTelemetryTracer.cs                            # ITelemetryTracer implementation, wraps ActivitySource
├── NoOpTelemetryTracer.cs                            # ITelemetryTracer implementation used when disabled
├── ObservabilityOptions.cs                           # Enabled/Backend/Endpoint/Langfuse
├── ObservabilityBackend.cs                           # enum: Jaeger | Langfuse
└── ObservabilityServiceCollectionExtensions.cs       # AddObservability(configuration, serviceName)

Financial.CashFlow.Application/                      # MODIFIED — new ProjectReference to Shared.Abstractions;
                                                       # use-case services call ITelemetryTracer.StartSpan(...) directly
Financial.Investment.Application/                    # MODIFIED — same

Financial.Shared.Infrastructure/Persistence/*.cs     # MODIFIED — takes ITelemetryTracer directly (new
                                                       # ProjectReference to Shared.Abstractions) for storage spans

Financial.Api/
├── Financial.Api.csproj                             # MODIFIED — ProjectReference to Integrations/Observability
├── Program.cs                                        # MODIFIED — one AddObservability(...) call
└── appsettings.json                                  # MODIFIED — new "Observability" section, Enabled=false default

Financial.App/
├── Financial.App.csproj                             # MODIFIED — ProjectReference to Integrations/Observability
├── App.xaml.cs                                       # MODIFIED — one AddObservability(...) call
└── appsettings.json                                  # MODIFIED — new "Observability" section, Enabled=false default
                                                       # WPF ViewModel command handlers call ITelemetryTracer
                                                       # directly to establish the trace root (FR-004a)

docker-compose.yml                                    # UNCHANGED
docker-compose.observability.yml                       # NEW — optional Jaeger/Langfuse overlay via Compose profiles

Tests/Financial.Architecture.Tests/
└── SharedAbstractionsDependencyRuleTests.cs           # NEW — asserts Financial.Shared.Abstractions has zero
                                                        # project dependencies

Tests/Integrations.Observability.Tests/               # NEW test project
Tests/Financial.Shared.Abstractions.Tests/             # NEW test project (if any logic beyond pure interfaces needs it)
Tests/Financial.CashFlow.Application.Tests/, Financial.Investment.Application.Tests/  # MODIFIED — assert
                                                        # expected spans via a hand-written RecordingTelemetryTracer
Tests/Financial.Api.Tests/, Tests/Financial.Presentation.Tests/  # MODIFIED as needed

NOT MODIFIED by this feature:
Financial.CashFlow.Domain/, Financial.Investment.Domain/, Financial.Web/
```

**Structure Decision**: Two new projects — `Financial.Shared.Abstractions` (pure contracts, the new dependency-free floor alongside Domain) and `Integrations/Observability` (the sole OpenTelemetry SDK owner, following the `Integrations/GoogleFinancialSupport` precedent). Application code in both bounded contexts gets a new, narrow, legitimate dependency on `Financial.Shared.Abstractions` only — never on `Financial.Shared.Infrastructure`, `Integrations/Observability`, or any other Infrastructure project.

## Complexity Tracking

*No Constitution Check violations — this table is intentionally empty. Adding `Financial.Shared.Abstractions` is a new project, but it reduces complexity relative to the reverted decorator approach (no reflection, no dynamic proxy) — it is the simpler design, not a tradeoff requiring justification.*
