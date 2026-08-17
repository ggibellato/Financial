# Contract: `ITelemetryTracer` / `ITelemetrySpan`

This is the *only* observability surface any project other than `Integrations/Observability` may reference — it is the seam FR-006 requires. Defined in `Financial.Shared.Infrastructure/Observability/`.

## Interface shape

```csharp
namespace Financial.Shared.Infrastructure.Observability;

public interface ITelemetryTracer
{
    ITelemetrySpan StartSpan(string name);
}

public interface ITelemetrySpan : IDisposable
{
    void SetAttribute(string key, string value);
    void RecordException(Exception exception);
}
```

## Contract rules

1. **Never null, never throws.** `StartSpan` always returns a usable `ITelemetrySpan`, whether observability is enabled or not (FR-006a). Callers MUST NOT check `Observability:Enabled` themselves — that branch lives only inside `Integrations/Observability`'s DI registration.
2. **`using` is the expected usage pattern**:
   ```csharp
   using var span = _tracer.StartSpan("CashFlow.CreateExpense");
   // ... do work ...
   ```
3. **Attribute values MUST come from the allow-list** in [telemetry-semantic-conventions.md](./telemetry-semantic-conventions.md) — no raw financial values or PII (FR-014). This is a caller responsibility for any *direct* `ITelemetryTracer` consumer (e.g. `Financial.Shared.Infrastructure`'s persistence code, WPF ViewModels); the `TracingDispatchProxy` decorator only ever sets a small, fixed set of structural attributes (interface/method name, success/failure), so it cannot itself violate this rule.
4. **`RecordException` MUST NOT pass through a message that may embed a financial value.** Prefer recording `exception.GetType().Name` as an attribute over the raw `Exception` in cases where the message is known to interpolate domain data (see `logging-audit.md`'s note on `CardStatementService`'s warning message).
5. **Consumers own the span name; the decorator owns its own naming convention.** Any direct `ITelemetryTracer` consumer follows the same `{Component}.{Operation}` convention as the decorator (see semantic-conventions contract) for consistency in the trace viewer, but nothing in the interface itself enforces a naming format.

## Who implements it

Only `Integrations/Observability`, with exactly two implementations:

- `OpenTelemetryTracer` — used when `Observability:Enabled=true`. Wraps a `System.Diagnostics.ActivitySource` internally; the OpenTelemetry SDK (registered in the same project) subscribes to that source.
- `NoOpTelemetryTracer` — used when `Observability:Enabled=false`. Returns a cached, allocation-free no-op `ITelemetrySpan` singleton.

## Who consumes it directly (not via the decorator)

- `Financial.Shared.Infrastructure/Persistence/*.cs` (JSON load/save, Google Drive I/O — "storage operation" spans required by FR-004).
- `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs` (records exceptions before translating them to HTTP responses — the top logging-audit remediation).
- `Financial.App`'s ViewModel/command layer (establishes the WPF trace root — FR-004a).

## Who consumes it indirectly, via `TracingDispatchProxy<TInterface>`

- Every Application-layer service interface in `Financial.CashFlow.Application`/`Financial.Investment.Application` (e.g. `IExpenseService`, `IIncomeService`) — decorated at DI-registration time in each context's own Infrastructure project. **Application code itself never references `ITelemetryTracer`.**
