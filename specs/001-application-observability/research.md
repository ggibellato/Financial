# Phase 0 Research: Application Observability (revised)

This supersedes the research produced for the first (reverted) implementation attempt. The prior attempt isolated the OpenTelemetry SDK from Domain/Application using the BCL `System.Diagnostics.ActivitySource` type directly in Application code — technically avoiding a *vendor* SDK reference, but not what the user meant by "isolated as an infrastructure/integration concern": they wanted a dedicated `Integrations/<Name>` project (matching `Integrations/GoogleFinancialSupport`) as the sole owner of the OpenTelemetry SDK, with everything else reaching it through a first-party interface. This document reflects that correction. Decisions D2, D6, D7, and the shape of D8 are carried forward unchanged from the original research since the underlying facts (frontend architecture, Langfuse's self-hosted footprint, the logging-audit findings) haven't changed.

## D1: `ITelemetryTracer` interface in the shared abstraction layer, implementation in a new Integrations project

**Decision**: Mirror the existing Google Drive isolation pattern exactly:

| Google Drive (existing) | Observability (this feature) |
|---|---|
| `IRemoteFileClientFactory` in `Financial.Shared.Infrastructure/Persistence/` | `ITelemetryTracer`/`ITelemetrySpan` in `Financial.Shared.Infrastructure/Observability/` |
| `GoogleFileClientFactory` in `Integrations/GoogleFinancialSupport` | `OpenTelemetryTracer` in `Integrations/Observability` |
| `AddGoogleDriveFileClient()` in `Integrations/GoogleFinancialSupport` | `AddObservability(configuration, serviceName)` in `Integrations/Observability` |
| `Financial.Api`/`Financial.App` reference `Integrations/GoogleFinancialSupport` directly for DI wiring | `Financial.Api`/`Financial.App` reference `Integrations/Observability` directly for DI wiring |

`ITelemetryTracer` is a small interface (`StartSpan(name) : ITelemetrySpan`, where `ITelemetrySpan : IDisposable` also exposes `SetAttribute`/`RecordException`). It has no dependency on OpenTelemetry types. The concrete `OpenTelemetryTracer` implementation (in `Integrations/Observability`) internally uses a `System.Diagnostics.ActivitySource` and returns a wrapper `Activity`-backed `ITelemetrySpan`; the OpenTelemetry SDK, registered in the same project, subscribes to that `ActivitySource` by name via `AddSource(...)`.

**Rationale**: this is the isolation the user explicitly asked for (FR-005/FR-006) — verified structurally (SC-008: exactly one project references the OpenTelemetry SDK), not just by a naming convention.

**Alternatives considered**: keeping the reverted attempt's BCL-`ActivitySource`-in-Application approach — explicitly rejected by the user; it satisfied "no vendor SDK reference" but not "confined to a dedicated Integrations project reached via an interface."

## D2: Web frontend is not directly instrumented (unchanged)

**Decision**: `Financial.Web` requires no OpenTelemetry JS/browser SDK. The trace root for a web-originated request is created server-side by `OpenTelemetry.Instrumentation.AspNetCore`'s automatic instrumentation — registered inside `Integrations/Observability`, invisible to `Financial.Api`'s own code — the moment the HTTP request reaches the API. FR-004 only requires correlation from "the backend controller" onward.

**Rationale**: unchanged from the original research — adding a browser-side SDK is more moving parts than this phase's FR-004 requires, and against Constitution Principle IV for a personal, single-user tool.

## D3: Application-layer "use case" spans via a generic decorator (`DispatchProxy`), not explicit calls in Application code

**Decision**: FR-004 requires a span for "the application-layer use case(s)" a request invokes — but Application cannot depend on `Financial.Shared.Infrastructure` (Clean Architecture's one-way dependency direction: Application depends on Domain only), so Application code cannot call `ITelemetryTracer` directly without breaking that rule, and duplicating the interface into each bounded context's own Application project would violate DRY and still require Application code changes the user's isolation goal argues against ("keep observability infrastructure isolated from application business logic").

Instead, a single generic decorator, `TracingDispatchProxy<TInterface>`, lives in `Financial.Shared.Infrastructure/Observability/` and is built on `System.Reflection.DispatchProxy` — part of the .NET shared framework since .NET Core 3.0, **not a new NuGet dependency**. Each bounded context's own Infrastructure project (which already legitimately depends on both its own Application project and `Financial.Shared.Infrastructure`) wraps its Application-layer service registrations with this proxy in one place: at the end of `CashFlowInfrastructureServiceCollectionExtensions.AddFinancialCashFlowInfrastructure(...)` and `InvestmentInfrastructureServiceCollectionExtensions.AddFinancialInfrastructure(...)`, after `AddFinancialCashFlowApplication()`/`AddFinancialApplication()` have already registered the real services (both composition roots already call these methods in that order). The proxy intercepts each method call on the wrapped interface, starts a span named `{BoundedContext}.{InterfaceName}.{MethodName}`, invokes the real implementation, and closes the span (recording an exception if the call throws).

**Consequence, stated as a hard constraint in this plan**: `Financial.CashFlow.Application`, `Financial.Investment.Application`, and both Domain projects require **zero code changes** for this feature. This is a stronger and cleaner realization of "isolate observability from business logic" than the reverted attempt achieved.

**Rationale**: satisfies FR-004 without violating Clean Architecture's dependency direction and without touching Application code at all. `DispatchProxy` is already part of the runtime (zero footprint), consistent with Constitution Principle IV's "no new dependency where the BCL already provides the mechanism."

**Alternatives considered**:
- *Explicit `ITelemetryTracer` calls hand-written in each Application service* — rejected: requires Application to depend on `Financial.Shared.Infrastructure` (breaks Principle I), or requires duplicating the interface per context (breaks DRY), and touches ~30 files across both contexts, in direct tension with the ~5-file-per-PR target.
- *A third-party decoration library (Scrutor)* — rejected: adds a new NuGet dependency for something the BCL's `DispatchProxy` already does, against Principle IV.
- *No use-case-level span at all, only controller + storage spans* — rejected: does not satisfy FR-004's explicit requirement that the trace include "the application-layer use case(s)."

**Flagged for user confirmation**: this is the one genuinely new pattern introduced by this feature (dynamic proxies aren't used elsewhere in this codebase). See plan.md's Summary note — confirm before `/speckit-tasks` proceeds, or redirect to the explicit-calls alternative if the reflection-based approach isn't wanted despite the file-count/layering tradeoffs.

## D4: Structured log correlation stays on Serilog, OTel export wired only inside Integrations/Observability

**Decision**: Application/Infrastructure code continues to use `Microsoft.Extensions.Logging.ILogger<T>` exactly as it does today (e.g. `CardStatementService`) — this is already a first-party, vendor-neutral abstraction (FR-006 is satisfied by `ILogger<T>` for logging; no new interface is needed for logs). `Financial.Api`/`Financial.App`'s existing `UseSerilog(...)` configuration gains an additional sink, but that sink is added via a call into `Integrations/Observability` (e.g. `loggerConfiguration.WriteToObservability(configuration)`), not by referencing `Serilog.Sinks.OpenTelemetry` from `Program.cs`/`App.xaml.cs` directly — keeping FR-005's "exactly one project" constraint intact even for the logging pipeline.

**Rationale**: reuses the existing, already-correct logging abstraction instead of inventing a parallel one; keeps the OTel-specific Serilog sink package confined to `Integrations/Observability` per FR-005.

**Alternatives considered**: migrating to `Microsoft.Extensions.Logging`'s native OTel logging provider, dropping Serilog — rejected, unnecessary churn, not required by any FR.

## D5: No-op `ITelemetryTracer` via the Null Object pattern, not "no registration"

**Decision**: `AddObservability(configuration, serviceName)` **always** registers `ITelemetryTracer` — with `NoOpTelemetryTracer` when `Observability:Enabled=false`, or `OpenTelemetryTracer` (and the full OTel SDK pipeline) when `true`. `GetRequiredService<ITelemetryTracer>()` never throws regardless of configuration (FR-006a). The no-op span returned by `NoOpTelemetryTracer.StartSpan(...)` is a cached singleton (`IDisposable.Dispose()` a no-op), so the disabled path allocates nothing per call.

**Rationale**: this is a cleaner realization of the original "no-op when disabled" goal (previously: don't register the SDK at all, and let `ActivitySource.StartActivity` return `null`) — now that everything goes through an interface, the interface itself can guarantee "always resolvable, always safe," which is simpler for every caller (the `TracingDispatchProxy` and any direct `ITelemetryTracer` consumer never need a null-check).

## D6: Logging-audit finding (unchanged — grounds FR-011 / User Story 4)

**Decision**: carried forward unchanged from the original research. Exactly one class in the entire solution (`CardStatementService` in `Financial.CashFlow.Application`) injects `ILogger<T>`, with exactly one `LogWarning` call; none of the 62 solution-wide `catch` blocks log the exception they catch. See [logging-audit.md](./logging-audit.md) (unchanged from the original pass — the codebase has not changed in the relevant ways since).

## D7: Langfuse local stack sourced from upstream, not reinvented (unchanged)

**Decision**: carried forward unchanged. `docker-compose.observability.yml` references Langfuse's official minimal self-hosted compose definition as an opt-in `langfuse` profile (ephemeral local volumes); Jaeger gets its own trivial `jaeger` profile (single all-in-one image, built-in OTLP receiver).

## D8: Observability configuration shape — now self-contained inside Integrations/Observability

**Decision**: `ObservabilityOptions`/`ObservabilityBackend` (the `Enabled`/`Backend`/`Endpoint`/`Langfuse` keys) move into `Integrations/Observability` itself rather than `Financial.Shared.Infrastructure`, since only that project ever reads them — keeping `Financial.Shared.Infrastructure/Observability/` down to just the interface + decorator (no configuration surface at all). The JSON shape and environment-variable convention are otherwise unchanged from the original research:

```json
"Observability": {
  "Enabled": false,
  "Backend": "Jaeger",
  "Endpoint": "http://localhost:4317",
  "Langfuse": { "PublicKey": "", "SecretKey": "" }
}
```

See [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md).
