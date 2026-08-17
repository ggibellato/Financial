# Contract: Telemetry Semantic Conventions

This is the naming/attribute contract every span (direct `ITelemetryTracer` call or `TracingDispatchProxy`-generated) MUST follow — it's what makes FR-014 (no PII/financial values in telemetry) enforceable in review rather than aspirational, and keeps the trace viewer's span names self-explanatory.

## Span naming — `{Component}.{Operation}`

| Origin | Naming pattern | Example |
|---|---|---|
| `TracingDispatchProxy<TInterface>` (Application use cases) | `{BoundedContext}.{InterfaceName-without-I}.{MethodName}` | `CashFlow.ExpenseService.CreateExpense` |
| `Financial.Shared.Infrastructure` persistence code | `{StorageComponent}.{Operation}` | `JsonStorage.Load`, `GoogleDrive.Upload` |
| `Financial.Api` middleware | `Api.{Operation}` | `Api.DomainExceptionMapped` |
| `Financial.App` WPF trace roots | `App.{ViewModelName}.{CommandName}` | `App.MonthlyViewModel.SaveExpense` |

`Financial.Api`'s controller-level spans and any HTTP-instrumentation-equivalent spans come from `OpenTelemetry.Instrumentation.AspNetCore`/`Http` auto-instrumentation (registered inside `Integrations/Observability`) and need no naming decision from application code.

## Attribute allow-list (enforces FR-014)

Attributes set via `ITelemetrySpan.SetAttribute` (directly, or by `TracingDispatchProxy`) MUST be drawn only from this allow-list:

**Allowed**:
- `entity.id` — an entity's `Guid`/ID, never a human-readable name
- `entity.type` — e.g. `"Expense"`, `"Transfer"`, `"CardStatement"`
- `bounded_context` — `"CashFlow"` or `"Investment"`
- `operation.name` — the use case / storage operation name
- `operation.result` — `"success"` / `"failed"` / `"rejected"`
- `error.type` — the .NET exception type name (`exception.GetType().Name`), not its message
- `http.route`, `http.method`, `http.status_code` — from ASP.NET Core auto-instrumentation, already safe by construction

**Explicitly denied** (non-exhaustive, illustrative — the allow-list above is the actual rule):
- Any monetary amount or balance (`Value`, `OutstandingTotal`, account balances)
- Account/broker/bank holder names or identifiers (`CreditCardName`, bank account numbers)
- Free-text exception messages that may embed a value (see `logging-audit.md`'s note on `CardStatementService`'s warning string, which currently embeds a statement's invoice period)
- Langfuse `SecretKey`/`PublicKey` or any other credential

## `TracingDispatchProxy`'s own attribute set (fixed, cannot violate the allow-list)

The decorator sets exactly: `bounded_context`, `operation.name` (interface + method name), and `operation.result` — never a method argument or return value. Any richer attribute (e.g. `entity.id` for a specific expense) requires the calling code to be a *direct* `ITelemetryTracer` consumer instead, which is a deliberate scope limit for this feature (see plan.md's "no changes to Application" constraint) — a future feature could extend the decorator to safely extract ID-shaped arguments if that's ever wanted.

## Metric instrumentation surface (enforces FR-015)

Only OpenTelemetry's standard `AspNetCoreInstrumentation`, `HttpClientInstrumentation`, and `RuntimeInstrumentation` are registered, entirely inside `Integrations/Observability`. No custom `Meter`/`Counter`/`Histogram` — nothing outside that project could add one even if it tried, since nothing outside it references the OpenTelemetry SDK.
