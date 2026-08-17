# Contract: Telemetry Semantic Conventions

This is the internal contract every future feature/PR touching Application or Infrastructure code MUST follow when adding a span, log property, or metric — it's what makes FR-006 (no vendor SDK dependency in Domain/Application) and FR-014 (no PII/financial values in telemetry) enforceable in review rather than aspirational.

## ActivitySource names

One `ActivitySource`, created as a `static readonly` field, per project that starts spans:

| Source name | Declared in |
|---|---|
| `Financial.CashFlow` | `Financial.CashFlow.Application` |
| `Financial.Investment` | `Financial.Investment.Application` |
| `Financial.Shared.Infrastructure` | `Financial.Shared.Infrastructure/Persistence` (JSON load/save, Google Drive I/O) |
| `Financial.App` | `Financial.App` (WPF command/ViewModel trace roots — FR-004a) |

`Financial.Api`'s controller-level spans and `Financial.App`'s HTTP-instrumentation-equivalent spans come from `OpenTelemetry.Instrumentation.AspNetCore`/`Http` auto-instrumentation and need no custom `ActivitySource`.

The OTel SDK registration in `Financial.Shared.Infrastructure/Observability/ServiceCollectionExtensions.cs` MUST `AddSource(...)` every name above so nothing is silently dropped.

## Span naming

`{Component}.{Operation}` — matches the existing class/method vocabulary so a span name is recognizable without reading code:

- Application use case: `CashFlow.CreateExpense`, `Investment.RecordTransaction`, etc. — `{BoundedContext}.{UseCaseName}`, using the same verb-noun naming already used for service methods.
- Storage operation: `JsonStorage.Load`, `JsonStorage.Save`, `GoogleDrive.Upload` — `{StorageComponent}.{Operation}`.

## Attribute allow-list (enforces FR-014)

Telemetry attributes (span attributes, structured-log properties, when observability is enabled) MUST be drawn only from this allow-list. Anything not on this list MUST NOT be attached as an attribute — when in doubt, leave it out and put it in the (non-telemetry) exception message only if truly necessary for debugging locally.

**Allowed**:
- `entity.id` — an entity's `Guid`/ID, never a human-readable name
- `entity.type` — e.g. `"Expense"`, `"Transfer"`, `"CardStatement"`
- `bounded_context` — `"CashFlow"` or `"Investment"`
- `operation.name` — the use case / storage operation name
- `operation.result` — `"success"` / `"failed"` / `"rejected"`
- `error.type` — the .NET exception type name (not its message, which may embed a value — see below)
- `http.route`, `http.method`, `http.status_code` — from ASP.NET Core auto-instrumentation, already safe by construction

**Explicitly denied** (non-exhaustive, illustrative — the allow-list above is the actual rule):
- Any monetary amount or balance (`Value`, `OutstandingTotal`, account balances)
- Account/broker/bank holder names or identifiers (`CreditCardName`, bank account numbers)
- Free-text fields that may embed a value (e.g. do not attach an exception's `Message` verbatim if it was built by string-interpolating a domain value — see `CardStatementService`'s `warning` string in logging-audit.md for a concrete example of a message that currently embeds a statement's invoice period but not a value; still worth reviewing message templates case by case as logging is added)
- Langfuse `SecretKey`/`PublicKey` or any other credential

## Metric instrumentation surface (enforces FR-015)

Only OpenTelemetry's standard `AspNetCoreInstrumentation`, `HttpClientInstrumentation`, and `RuntimeInstrumentation` are registered. No custom `Meter`/`Counter`/`Histogram` is added by this feature — a future spec that explicitly wants business-domain metrics (e.g. "expenses created per day") would extend this contract, not silently add one via this feature's plumbing.
