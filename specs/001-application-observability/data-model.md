# Phase 1 Data Model: Application Observability (revision 3)

This feature has no business-domain data model. The "entities" below are the configuration and telemetry-signal concepts introduced by this capability.

## ITelemetryTracer / ITelemetrySpan (the first-party abstraction — FR-006)

Defined in `Financial.Shared.Abstractions` — the dependency-free project both Application and Infrastructure code can reference directly.

| Member | Notes |
|---|---|
| `ITelemetryTracer.StartSpan(string name)` | Returns an `ITelemetrySpan`. Never returns `null` and never throws, regardless of `Observability:Enabled` (FR-006a) — backed by `NoOpTelemetryTracer` when disabled. |
| `ITelemetrySpan : IDisposable` | `Dispose()` ends the span. |
| `ITelemetrySpan.SetAttribute(string key, string value)` | Value MUST come only from the allow-list in [contracts/telemetry-semantic-conventions.md](./contracts/telemetry-semantic-conventions.md) (FR-014). |
| `ITelemetrySpan.RecordException(Exception exception)` | Marks the span as errored; MUST NOT record `exception.Message` verbatim if it may embed a financial value. |

**Lifecycle**: resolved from DI, typically as a constructor dependency of an Application service (same pattern as `ILogger<T>` in `CardStatementService`) or of `Financial.Shared.Infrastructure`'s persistence classes. `StartSpan` may be called concurrently — each call returns an independent `ITelemetrySpan`.

**Usage pattern** (Application service, explicit — D3):

```csharp
public sealed class ExpenseService(IExpenseRepository repository, ITelemetryTracer tracer) : IExpenseService
{
    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseDto dto)
    {
        using var span = tracer.StartSpan("CashFlow.ExpenseService.CreateExpense");
        // ... existing logic, unchanged ...
    }
}
```

## Financial.Shared.Abstractions (the project itself)

| Property | Value |
|---|---|
| Package references | none |
| Project references | none |
| Referenced by | `Financial.CashFlow.Application`, `Financial.Investment.Application`, `Financial.Shared.Infrastructure`, `Financial.Api`, `Financial.App`, `Integrations/Observability` |
| Enforced by | `Tests/Financial.Architecture.Tests/SharedAbstractionsDependencyRuleTests.cs` (research.md Decision D9) |

## Observability Configuration (self-contained in `Integrations/Observability` — Decision D8)

| Field | Type | Default | Validation |
|---|---|---|---|
| `Enabled` | bool | `false` | none — always a valid value |
| `Backend` | enum `{Jaeger, Langfuse}` | `Jaeger` | Only read when `Enabled=true`; an unrecognized value fails fast at startup (strongly-typed enum binding) |
| `Endpoint` | string (URL) | `http://localhost:4317` | Required (non-empty) when `Enabled=true`; not validated for reachability at startup (FR-010) |
| `Langfuse.PublicKey` / `Langfuse.SecretKey` | string | `""` | Required when `Enabled=true` and `Backend=Langfuse`; never logged, never a span/log attribute |
| `ServiceName` | string | derived, not user-configurable | Passed as a parameter to `AddObservability(configuration, serviceName)` by each composition root |

**Lifecycle**: read once at process startup, same as every other configuration value in this application.

## Trace / Span (external OTel concepts)

| Concept | Notes |
|---|---|
| Trace | One `TraceId` per logical operation. Root span: ASP.NET Core auto-instrumentation for Web-originated requests; an explicit `ITelemetryTracer.StartSpan(...)` call in the WPF ViewModel/command layer for WPF-originated operations (FR-004a). |
| Span | Every non-root span has a non-null parent, via `Activity.Current` propagation inside `OpenTelemetryTracer` — invisible to callers of `ITelemetryTracer`. |

## Metric

Per FR-015, scoped to OpenTelemetry's standard auto-instrumentation only, registered entirely inside `Integrations/Observability`. No custom `Meter`/`Counter`/`Histogram`, and no interface needed for metrics at all.

## Log Record

Unchanged shape from the existing Serilog structured-logging pipeline, produced via the existing `ILogger<T>`. When observability is enabled, `TraceId`/`SpanId` enrichment and optional OTLP export are added by a sink configured from inside `Integrations/Observability` (Decision D4).
