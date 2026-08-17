# Phase 1 Data Model: Application Observability

This feature has no business-domain data model — it introduces no new entity in either bounded context's JSON document and does not change `data-cashflow.json`/`data-investment.json`. The "entities" below are the configuration, abstraction, and telemetry-signal concepts introduced by this capability.

## ITelemetryTracer / ITelemetrySpan (the first-party abstraction — FR-006)

Defined in `Financial.Shared.Infrastructure/Observability/`. This is the *only* type most of the codebase ever touches for tracing.

| Member | Notes |
|---|---|
| `ITelemetryTracer.StartSpan(string name)` | Returns an `ITelemetrySpan`. Never returns `null` and never throws, regardless of `Observability:Enabled` (FR-006a) — backed by `NoOpTelemetryTracer` when disabled. |
| `ITelemetrySpan : IDisposable` | `Dispose()` ends the span. |
| `ITelemetrySpan.SetAttribute(string key, string value)` | Value MUST come only from the allow-list in [contracts/telemetry-semantic-conventions.md](./contracts/telemetry-semantic-conventions.md) (FR-014). |
| `ITelemetrySpan.RecordException(Exception exception)` | Marks the span as errored; MUST NOT record `exception.Message` verbatim if it may embed a financial value (see logging-audit.md and the semantic-conventions contract). |

**Lifecycle**: resolved from DI as a singleton (mirrors how `IRemoteFileClientFactory` is registered). `StartSpan` may be called concurrently from multiple requests/threads — each call returns an independent `ITelemetrySpan`.

## TracingDispatchProxy\<TInterface\> (the decorator — Decision D3)

Not user-facing configuration, but a structural piece of the data model worth documenting: a generic `System.Reflection.DispatchProxy` subclass, also in `Financial.Shared.Infrastructure/Observability/`, parameterized by the Application service interface it wraps (e.g. `TracingDispatchProxy<IExpenseService>`). Holds a reference to the real implementation and an `ITelemetryTracer`. On every intercepted method call: starts a span named `{BoundedContext}.{InterfaceName}.{MethodName}`, invokes the real method, records an exception on the span if the call throws, then disposes the span.

## Observability Configuration (moved into `Integrations/Observability` — Decision D8)

| Field | Type | Default | Validation |
|---|---|---|---|
| `Enabled` | bool | `false` | none — always a valid value |
| `Backend` | enum `{Jaeger, Langfuse}` | `Jaeger` | Only read when `Enabled=true`; an unrecognized value fails fast at startup (strongly-typed enum binding) |
| `Endpoint` | string (URL) | `http://localhost:4317` | Required (non-empty) when `Enabled=true`; not validated for reachability at startup (FR-010) |
| `Langfuse.PublicKey` / `Langfuse.SecretKey` | string | `""` | Required when `Enabled=true` and `Backend=Langfuse`; never logged, never a span/log attribute |
| `ServiceName` | string | derived, not user-configurable | Passed as a parameter to `AddObservability(configuration, serviceName)` by each composition root (`"Financial.Api"` / `"Financial.App"`) |

**Lifecycle**: read once at process startup, same as every other configuration value in this application.

## Trace / Span (external OTel concepts — unchanged shape from the reverted attempt's design, now produced only from inside `Integrations/Observability`)

| Concept | Notes |
|---|---|
| Trace | One `TraceId` per logical operation. Root span: ASP.NET Core auto-instrumentation for Web-originated requests; an explicit `ITelemetryTracer.StartSpan(...)` call in the WPF ViewModel/command layer for WPF-originated operations (FR-004a). |
| Span | Every non-root span has a non-null parent, via `Activity.Current` propagation inside `OpenTelemetryTracer` — invisible to callers of `ITelemetryTracer`. |

## Metric

Per FR-015, scoped to OpenTelemetry's standard ASP.NET Core / `HttpClient` / .NET runtime auto-instrumentation only, registered entirely inside `Integrations/Observability`. No custom `Meter`/`Counter`/`Histogram`, and no interface is needed for metrics at all (nothing outside `Integrations/Observability` ever records one).

## Log Record

Unchanged shape from the existing Serilog structured-logging pipeline (`Timestamp`, `Level`, `MessageTemplate`, `Properties`), produced via the existing `ILogger<T>` (already a first-party abstraction — no new interface). When observability is enabled, `TraceId`/`SpanId` enrichment and optional OTLP export are added by a sink configured from inside `Integrations/Observability` (Decision D4) — `Financial.Api`/`Financial.App`'s own `UseSerilog(...)` code calls into that project's extension method rather than referencing `Serilog.Sinks.OpenTelemetry` directly.
