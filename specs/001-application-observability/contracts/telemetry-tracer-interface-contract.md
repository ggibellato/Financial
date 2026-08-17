# Contract: `ITelemetryTracer` / `ITelemetrySpan`

This is the *only* observability surface any project other than `Integrations/Observability` may reference — it is the seam FR-006 requires. Defined in `Financial.Shared.Abstractions` (a dependency-free project — no package references, no project references), so both Application and Infrastructure code can depend on it directly.

## Interface shape

```csharp
namespace Financial.Shared.Abstractions;

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
   using var span = _tracer.StartSpan("CashFlow.ExpenseService.CreateExpense");
   ```
3. **Attribute values MUST come from the allow-list** in [telemetry-semantic-conventions.md](./telemetry-semantic-conventions.md) — no raw financial values or PII (FR-014).
4. **`RecordException` MUST NOT pass through a message that may embed a financial value.** Prefer recording `exception.GetType().Name` as an attribute over the raw `Exception` in cases where the message is known to interpolate domain data (see `logging-audit.md`'s note on `CardStatementService`'s warning message).
5. **Span naming follows `{Component}.{Operation}`** (see semantic-conventions contract) — every direct consumer follows this convention for consistency in the trace viewer.

## Who implements it

Only `Integrations/Observability`, with exactly two implementations:

- `OpenTelemetryTracer` — used when `Observability:Enabled=true`. Wraps a `System.Diagnostics.ActivitySource` internally; the OpenTelemetry SDK (registered in the same project) subscribes to that source.
- `NoOpTelemetryTracer` — used when `Observability:Enabled=false`. Returns a cached, allocation-free no-op `ITelemetrySpan` singleton.

## Who consumes it — everyone directly, no indirection

Unlike an earlier design that used a reflection-based decorator to avoid Application code changes (reverted — see research.md Decision D1/D3), this revision has **every** consumer call `ITelemetryTracer` explicitly:

- **`Financial.CashFlow.Application`/`Financial.Investment.Application` service classes** — injected via constructor exactly like `ILogger<T>` already is in `CardStatementService`; `StartSpan` called at the top of each traced use-case method, spanning the same scope the method itself does.
- **`Financial.Shared.Infrastructure/Persistence/*.cs`** (JSON load/save, Google Drive I/O) — the "storage operation" spans required by FR-004.
- **`Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs`** — records exceptions before translating them to HTTP responses (the top logging-audit remediation).
- **`Financial.App`'s ViewModel/command layer** — establishes the WPF trace root (FR-004a).

Every one of these projects gets a new `ProjectReference` to `Financial.Shared.Abstractions` — never to `Integrations/Observability` (except `Financial.Api`/`Financial.App`, which reference it once each, purely for the `AddObservability(...)` DI-registration call).
