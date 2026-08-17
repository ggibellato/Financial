# Phase 0 Research: Application Observability

All items below were either explicit inputs from the approved spec or resolved during this planning pass by reading the current codebase (no unresolved `NEEDS CLARIFICATION` markers remain in [plan.md](./plan.md)'s Technical Context).

## D1: Trace propagation without a Domain/Application dependency on OpenTelemetry

**Decision**: Application-layer code marks trace boundaries using `System.Diagnostics.ActivitySource` and `Activity` — part of `System.Diagnostics.DiagnosticSource`, a BCL primitive, not the OpenTelemetry SDK. One `ActivitySource` per bounded context (`Financial.CashFlow`, `Financial.Investment`) plus one for shared infrastructure (`Financial.Shared.Infrastructure`) is created as a simple static field, with no package dependency beyond what .NET already ships. The OpenTelemetry SDK — registered only in `Financial.Shared.Infrastructure/Observability` and the two Presentation composition roots — subscribes to these named sources via `AddSource(...)` and does the actual export. `Activity.Current` propagates automatically via `AsyncLocal<T>`, so spans started in Application nest correctly under the ASP.NET Core (or WPF) root span with no manual context-passing.

**Rationale**: This is the standard, documented .NET pattern for exactly this isolation requirement (FR-005/FR-006) — it's how `Microsoft.AspNetCore.*` and `System.Net.Http` instrument themselves without depending on any particular observability vendor.

**Alternatives considered**: Giving Application services a direct dependency on `OpenTelemetry.Api` — rejected, it's still a vendor package and would violate FR-006's letter even though it's the "API-only" package; the BCL `ActivitySource` achieves the same isolation with zero package footprint in Domain/Application.

## D2: Web frontend is not directly instrumented

**Decision**: `Financial.Web` requires no OpenTelemetry JS/browser SDK. The trace root for a web-originated request is created server-side by `OpenTelemetry.Instrumentation.AspNetCore`'s automatic instrumentation the moment the HTTP request reaches `Financial.Api`. FR-004 only requires correlation from "the backend controller" onward — it does not require the browser itself to emit a span.

**Rationale**: Adding `@opentelemetry/sdk-trace-web` (plus a CORS-exposed `traceparent` header, a batch span processor, a second exporter target from the browser, etc.) is meaningfully more moving parts for a personal, single-user tool whose explicit goal is learning OpenTelemetry on the .NET side (Constitution Principle IV — right-sized engineering). The financial-web API client already funnels every request through one `request()` helper (`Financial.Web/src/api/financialApiClient.ts`), so if a future spec explicitly wants client-originated spans, there's a single injection point to add a `traceparent` header from — but it isn't needed to satisfy this spec's FR-004 wording.

**Alternatives considered**: Instrumenting `Financial.Web` with the OTel Web SDK for full client-to-server trace propagation — rejected for this phase; revisit only if a future spec explicitly asks for client-side spans (e.g. to measure browser-side render time as part of the same trace).

## D3: One OTLP exporter path for both Jaeger and Langfuse

**Decision**: Both Jaeger (since v1.35, OTLP is its native ingestion protocol) and Langfuse (exposes an OTLP-compatible ingestion endpoint) accept OpenTelemetry Protocol directly. The plan uses a single `OpenTelemetry.Exporter.OpenTelemetryProtocol` exporter registration; only the configured `Endpoint` and, for Langfuse, a Basic Auth header built from `PublicKey`/`SecretKey`, differ between the two backends.

**Rationale**: Avoids a second, backend-specific exporter package and branch of code — directly serves Constitution Principle IV and keeps FR-007/FR-008 ("exactly one active at a time, config-selected") a configuration concern rather than a code-branching one.

**Alternatives considered**: `OpenTelemetry.Exporter.Jaeger` (Jaeger's legacy native-protocol exporter, now deprecated upstream in favor of OTLP) — rejected as unnecessary and against upstream guidance.

## D4: Structured log correlation reuses the existing Serilog setup

**Decision**: Keep Serilog as the logging framework (already wired in both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs`, including the rolling file sink). Add trace/span-id enrichment (e.g. `Serilog.Sinks.OpenTelemetry` or an equivalent enricher) so that, when observability is enabled, every log event carries the active `TraceId`/`SpanId` and — depending on the chosen package — is optionally also exported via OTLP alongside traces/metrics.

**Rationale**: FR-003 requires "structured logs" when enabled; Serilog already produces structured (property-based) log events today. Replacing it with `Microsoft.Extensions.Logging`'s native OTel logging provider would discard the existing rolling-file-sink configuration for no requirement that asks for it.

**Alternatives considered**: Fully migrating logging to `Microsoft.Extensions.Logging` + the OpenTelemetry Logging provider, dropping Serilog — rejected, out of scope and unnecessary churn.

## D5: Disabled-by-default is a true no-op, not a stub

**Decision**: `AddFinancialObservability` does not call `.AddOpenTelemetry()` at all when `Observability:Enabled` is `false` — no SDK objects, no exporters, no background export threads are created. Elsewhere in the app, `ActivitySource.StartActivity(...)` calls are left in place unconditionally; when no listener is registered (the disabled case), .NET returns `null` from `StartActivity` at negligible cost, and calling code is written to tolerate a `null` `Activity` (a documented, standard .NET pattern — no `if (enabled)` guards need to be threaded through business code).

**Rationale**: Directly satisfies FR-002/SC-001 ("no dependency on Jaeger/Langfuse... application must continue to operate normally") without conditional compilation or scattering enabled-checks through Application code.

## D6: Logging-audit finding (grounds FR-011 / User Story 4)

**Decision**: The audit (see [logging-audit.md](./logging-audit.md)) is based on an exhaustive `ILogger`/`Log*` grep across the entire solution (excluding `bin`/`obj`/test projects) performed during this planning pass, not estimation. Headline finding: exactly **one** class in the entire solution (`CardStatementService` in `Financial.CashFlow.Application`) injects `ILogger<T>`, and it contains exactly **one** `LogWarning` call. There are **zero** other explicit `LogInformation`/`LogWarning`/`LogError`/`LogDebug`/`LogCritical` call sites anywhere in Domain, Application, Infrastructure, or Presentation code across both bounded contexts, `Financial.Api`, and `Financial.App`. Separately, of the 62 `catch` blocks found solution-wide, none log the caught exception before either rethrowing, converting it to an HTTP response (`DomainExceptionMappingMiddleware`), or showing a WPF `MessageBox` — failures are visible to the end user (as a dialog or an HTTP error body) but invisible in the log stream.

**Rationale**: This is the concrete evidence FR-011/SC-005 require; it means the audit's dominant category will be "insufficient logging" almost everywhere, but FR-011 still requires walking each layer/bounded context individually (not just stating "insufficient across the board") to catalog exactly which use cases and failure paths have zero coverage today, which is what `logging-audit.md` does.

## D7: Langfuse local stack sourced from upstream, not reinvented

**Decision**: Langfuse's official self-hosted docker-compose bundles Postgres, ClickHouse, Redis, and MinIO — considerably heavier than "use only what is necessary" (spec's stated constraint) calls for, but it's also not something this project should re-architect. `docker-compose.observability.yml` references/vendors Langfuse's official minimal compose definition as an opt-in `langfuse` profile, with its data volumes left as ephemeral/local (not committed, not required for long-term retention — consistent with FR-009). Jaeger, by contrast, ships a single all-in-one container image with an embedded in-memory store and a built-in OTLP receiver — trivial to add as a second, independent `jaeger` profile in the same overlay file.

**Rationale**: Avoids hand-rolling and maintaining a parallel copy of Langfuse's infrastructure; keeps the two backend options genuinely swappable and independent per FR-007/FR-008.

**Alternatives considered**: Standing up only Jaeger for this phase and treating Langfuse support as aspirational — rejected because the spec (FR-007) explicitly requires both to be supported (one at a time), not just designed for.

## D8: Observability configuration shape

**Decision**: A single, process-wide `Observability` configuration section (not per-bounded-context, unlike `Investment:Repository:Provider`/`CashFlow:Repository:Provider`, because telemetry export is a whole-process concern):

```json
"Observability": {
  "Enabled": false,
  "Backend": "Jaeger",
  "Endpoint": "http://localhost:4317",
  "Langfuse": { "PublicKey": "", "SecretKey": "" }
}
```

Environment-variable overrides follow the existing double-underscore convention already used for `Investment__Repository__Provider` etc. in `docker-compose.yml` (e.g. `Observability__Enabled=true`).

**Rationale**: Matches FR-013 ("same configuration mechanism already used by the application") and keeps the shape consistent with the one other config-driven provider switch already in the codebase (`Repository:Provider`).

See [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md) for the full schema.
