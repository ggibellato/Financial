# Feature Specification: Application Observability

**Feature Branch**: `feat/application-observability`

**Created**: 2026-08-17

**Status**: Draft

**Input**: User description: "Establish an observability capability for the existing Financial application. The application currently consists of a C#/.NET backend and core components, a WPF desktop frontend, and a React + TypeScript web frontend. This application, although used by its developer to track personal financial information, is also used as a vehicle to learn how to implement things using good practices. Observability is one such capability that will be present in the application, but the goal is more about learning how to do it properly than delivering a full 24/7 production-grade observability feature. The observability capability must provide logs, metrics, and distributed traces while keeping observability infrastructure isolated from application business logic. OpenTelemetry (and any specific backend SDK/client library it requires) MUST be confined to a single new dedicated integration project, following this repository's existing `Integrations/<Name>` pattern — a new `Integrations/Observability` project. That project MUST be the only project in the solution that references the OpenTelemetry SDK or any observability-backend-specific package. Every other part of the application MUST reach tracing/metrics/logging capabilities only through a first-party interface, injected via dependency injection, with the concrete OpenTelemetry-backed implementation supplied only by the new Integrations/Observability project. This mirrors exactly how Google Drive access is already isolated in this codebase. The application must be able to run with observability enabled or disabled through configuration/environment settings. When observability is disabled, the application must continue to operate normally and must not require Jaeger, Langfuse, or other observability infrastructure. When enabled, observability should provide structured logs, application/runtime metrics, and distributed traces. Tracing should allow a request originating from the web frontend to be correlated through the backend controller, application services, and persistence/storage operations. The solution should support local/containerized observability infrastructure including Jaeger and Langfuse, one of them used at a time when observability is enabled. Observability data should use only what is necessary at the container level; there is no need for long-term storage. The system should also establish whether the current logging volume and levels are appropriate, and identify excessive, insufficient, duplicated, or missing logging. This is an existing application, so the solution must respect the current architecture documented in the repository. Implementation must proceed as very small, tightly-scoped vertical slices — target roughly 5 source/config code files per pull request (excluding documentation and test files). Do not implement anything — first produce a specification and identify ambiguities and questions that must be resolved before technical planning."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Diagnose a request end to end while it's happening (Priority: P1)

The developer performs an action in a client (e.g. saves an expense in the web app, or triggers a sync from the WPF app) and wants to see, as one connected story, everything the system did to fulfil that action: which controller/entry point handled it, which application use case ran, and which storage operation(s) it triggered — without adding print statements or attaching a debugger.

**Why this priority**: This is the core value of "observability" as distinct from plain logging — it's the capability that makes the feature worth building at all, and the primary thing the developer is trying to learn to do well.

**Independent Test**: With observability enabled and a trace backend running locally, perform one user action end to end and confirm a single trace exists that includes a span for the entry point, a span for the application-layer use case, and a span for the storage/persistence operation, all linked under one trace identifier.

**Acceptance Scenarios**:

1. **Given** observability is enabled and the chosen trace backend is running, **When** the developer performs an action from a client that calls the backend, **Then** a single trace is produced covering the request from entry point through application service to storage, viewable as one connected timeline in the trace backend's UI.
2. **Given** a trace is being viewed, **When** the developer inspects an individual span, **Then** the span identifies which layer/component it belongs to (e.g. controller, application service, storage) without requiring the developer to already know the code structure.

---

### User Story 2 - Run the application with observability fully disabled (Priority: P1)

The developer runs the application day-to-day (as a personal finance tool) without any observability infrastructure present, and the application must start, serve requests, and behave exactly as it does today — with zero dependency on Jaeger, Langfuse, or any collector being reachable.

**Why this priority**: The application's primary purpose is personal finance tracking, not observability; day-to-day use must never be put at risk by an optional, learning-oriented capability. This is a hard constraint, not a nice-to-have.

**Independent Test**: Set the observability configuration to disabled, do not start any observability infrastructure (no Jaeger/Langfuse containers running), and confirm the application starts via the standard production startup path and all existing functionality (API, WPF, Web) continues to work with no errors, timeouts, or degraded behavior related to observability.

**Acceptance Scenarios**:

1. **Given** observability is disabled in configuration, **When** the application starts via the standard startup path, **Then** it starts successfully with no observability backend running and logs no errors related to failed telemetry export.
2. **Given** observability is disabled, **When** a developer exercises existing features (investment tracking, cash flow tracking), **Then** behavior, response times, and output are unchanged from the current (pre-observability) application.

---

### User Story 3 - Toggle and choose an observability backend via configuration only (Priority: P2)

The developer wants to turn observability on or off, and when on, choose which backend (Jaeger or Langfuse) receives the telemetry, purely through configuration/environment settings — no code changes or recompilation required.

**Why this priority**: This is what makes the capability safe to leave in the codebase permanently and usable for learning/experimentation without it becoming a maintenance burden or a risk to the primary application.

**Independent Test**: Starting from observability disabled, change only configuration/environment values to enable observability pointed at Jaeger, restart the application, confirm telemetry appears in Jaeger; repeat pointed at Langfuse instead, with no code changes between runs.

**Acceptance Scenarios**:

1. **Given** the application is stopped, **When** the developer sets configuration to enable observability with Jaeger as the target and restarts, **Then** logs, metrics, and traces appear in Jaeger with no source code changes.
2. **Given** the application is stopped, **When** the developer changes only the target backend setting from Jaeger to Langfuse and restarts, **Then** telemetry appears in Langfuse instead, with no other configuration or code changes required.

---

### User Story 4 - Assess whether existing logging is fit for purpose (Priority: P3)

The developer wants a clear, evidence-based assessment of the application's current logging: where it's excessive (noisy, low-value), insufficient (missing coverage for meaningful events/errors), duplicated (the same event logged more than once across layers), or simply missing (an important state change or failure with no log statement at all) — so that structured logging introduced by this capability improves on today's baseline instead of just adding a new pipe to the same problems.

**Why this priority**: This is a prerequisite piece of analysis that informs what "structured logs" should actually contain once observability is enabled, but it does not block the tracing/metrics capability from being designed and is lower risk than the runtime toggle behavior.

**Independent Test**: Produce a written assessment covering both bounded contexts (Investment, CashFlow) and all Presentation layers (Api, App, Web), citing specific log call sites, and categorizing each finding as excessive, insufficient, duplicated, or missing, independent of whether OpenTelemetry instrumentation has been implemented yet.

**Acceptance Scenarios**:

1. **Given** the current codebase, **When** the assessment is performed, **Then** it produces a list of specific findings (file/call-site level) categorized as excessive, insufficient, duplicated, or missing, for each layer that currently contains logging.
2. **Given** the assessment findings, **When** technical planning for this feature begins, **Then** the findings are available as an input to decide what the new structured-logging approach should preserve, change, or add.

---

### Edge Cases

- What happens when observability is enabled in configuration but the configured backend (Jaeger or Langfuse) is unreachable at startup or goes down while the application is running? The application must not crash, fail to start, or block user-facing operations — degraded/absent telemetry is acceptable, a broken application is not.
- What happens when observability configuration is changed while the application is running (not at startup)? Given the existing constraint that this application's configuration is read once at process startup, no runtime-reconfiguration behavior is expected — a restart is required, consistent with how other configuration changes are already handled.
- What happens to in-flight traces/spans if the application shuts down mid-request? Partial telemetry loss for that one request is acceptable; it must not delay shutdown or corrupt state.
- What happens if a WPF-originated operation is triggered by a background/automatic process rather than a direct user action (e.g. a scheduled sync)? It still needs a defined trace root per FR-004a even though there is no user-initiated UI event to anchor it to.
- What happens if code outside the new observability integration project needs to record telemetry but the integration project isn't wired up (e.g. observability disabled, or the interface has no registered implementation)? The first-party interface MUST have a safe no-op behavior in that case (see FR-006a) so calling code never has to check "is observability available" itself.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow observability to be fully enabled or fully disabled via configuration/environment settings, without requiring a code change or rebuild to switch between the two states.
- **FR-002**: When observability is disabled, the application MUST start, and all existing functionality MUST operate, with no dependency on Jaeger, Langfuse, or any other observability infrastructure being present or reachable.
- **FR-003**: When observability is enabled, the system MUST emit structured logs, application/runtime metrics, and distributed traces.
- **FR-004**: When observability is enabled, a single logical request originating from the web frontend MUST produce one correlated trace spanning the backend entry point (controller), the application-layer use case(s) it invokes, and the persistence/storage operation(s) it triggers.
- **FR-004a**: When observability is enabled, an operation originating from the WPF desktop client MUST also produce one correlated trace, at parity with the Web client (Constitution Principle III), even though WPF invokes the Application layer in-process rather than over HTTP. The trace root for a WPF-originated operation MUST be established at the point where the user-initiated action begins (e.g. the ViewModel/command handler that starts the use case), so the resulting trace still spans use case execution and the persistence/storage operation(s) it triggers.
- **FR-005**: All OpenTelemetry SDK code, and any package specific to a particular observability backend (Jaeger, Langfuse, or any future backend), MUST be confined to a single new, dedicated project (`Integrations/Observability`, following this repository's existing `Integrations/<Name>` pattern used for Google Drive/Google Sheets access). No other project in the solution — Domain, Application, Infrastructure, or Presentation, for either bounded context — may reference the OpenTelemetry SDK or any observability-backend-specific package, directly or transitively.
- **FR-006**: Every part of the system that needs to record a log, a metric, or a trace span MUST do so only through a first-party interface (owned by Application or a shared abstraction layer, not by the `Integrations/Observability` project), injected via dependency injection. The concrete, OpenTelemetry-backed implementation of that interface MUST be supplied only by the `Integrations/Observability` project. This isolation is not satisfied merely by avoiding a compile-time OpenTelemetry SDK reference in Domain/Application (e.g. via a Base Class Library type) — it requires the interface-and-adapter structure described here.
- **FR-006a**: The first-party observability interface MUST behave safely (no-op, no exception) when no concrete implementation is registered or when observability is disabled, so that calling code never needs to check "is observability available" before using it.
- **FR-007**: The system MUST support routing telemetry to Jaeger or to Langfuse, selected via configuration, with exactly one of the two active at a time when observability is enabled.
- **FR-008**: The system MUST NOT require simultaneous operation of more than one observability backend.
- **FR-009**: Observability infrastructure (collector, backend UI, etc.) MUST be runnable locally in a container, and MUST NOT require long-term/persistent data retention — data scoped to the lifetime of the local container is sufficient.
- **FR-010**: If the configured observability backend is unreachable when observability is enabled, the application MUST continue to start and serve requests; telemetry export failures MUST NOT block or fail user-facing operations.
- **FR-011**: The system MUST produce a documented assessment of current logging (across both bounded contexts and all Presentation projects) identifying excessive, insufficient, duplicated, and missing logging, to inform the structured-logging design.
- **FR-012**: The observability capability MUST respect existing bounded-context isolation — enabling/instrumenting one bounded context's code path MUST NOT create a dependency between the Investment and CashFlow contexts.
- **FR-013**: Configuration for observability (enabled/disabled, backend selection, endpoint) MUST follow the same configuration mechanism already used by the application (e.g. `appsettings`/environment variables), not a bespoke mechanism.
- **FR-014**: Telemetry (logs, trace/span attributes, metrics) MUST NOT include raw financial values or PII (e.g. account balances, transaction amounts, broker/bank account identifiers, holder names) when observability is enabled. Telemetry attributes MUST be limited to structural identifiers (e.g. entity IDs, correlation IDs) and category/type data (e.g. transaction type, operation name) sufficient to follow a trace without exposing the underlying financial data.
- **FR-015**: Application/runtime metrics MUST cover technical/runtime signals — request counts, request duration, error rates, and .NET runtime/GC metrics — via OpenTelemetry's standard auto-instrumentation surface. Business-domain metrics (e.g. counts of transactions or investments processed) are out of scope for this feature.

### Key Entities

- **Trace**: A correlated record of one logical request/operation as it moves through the system (client → controller → application service → storage), composed of one or more Spans.
- **Span**: A single unit of work within a Trace (e.g. "handle HTTP request", "execute use case", "read/write JSON document"), with a start/end time and a reference to its parent span, if any.
- **Log Record**: A structured, timestamped event emitted by the application, optionally correlated to an active Trace/Span.
- **Metric**: A numeric measurement of application or runtime behavior (e.g. request count, request duration, error count) aggregated over time, not tied to a single request.
- **Observability Configuration**: The set of settings that determine whether observability is enabled and, if so, which backend (Jaeger or Langfuse) receives telemetry.
- **Observability Abstraction (interface)**: The first-party contract (e.g. a tracer/logger/meter-shaped interface) that every non-integration part of the system depends on to record telemetry, satisfied at runtime by the `Integrations/Observability` project's OpenTelemetry-backed implementation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With observability disabled, the application starts and every existing feature continues to work with no observability infrastructure running, 100% of the time.
- **SC-002**: With observability enabled and a backend running, a developer can find and view one complete, connected trace for a single user action — from either client (Web or WPF) through to the storage operation — without consulting source code to understand which pieces belong together.
- **SC-003**: A developer can switch observability from disabled to enabled, or from one backend (Jaeger) to the other (Langfuse), using configuration changes alone, in a single restart, with no source code changes.
- **SC-004**: 100% of the existing automated test suite continues to pass, unchanged, both with observability enabled and with it disabled.
- **SC-005**: The logging assessment identifies, with specific file/call-site references, at least the categories of excessive, insufficient, duplicated, and missing logging (or explicitly states that a category has no findings), across both bounded contexts and all Presentation projects.
- **SC-006**: If the configured observability backend is stopped or unreachable while the application is enabled and running, existing (non-observability) application functionality shows no user-visible failure or delay attributable to the missing backend.
- **SC-007**: No telemetry record (log, span attribute, or metric) emitted during normal operation contains a raw financial value (balance, transaction amount) or PII (account holder name, broker/bank account identifier), verified by review of the instrumentation's attribute set.
- **SC-008**: Exactly one project in the solution references the OpenTelemetry SDK or any observability-backend-specific package, verified by inspecting project references/dependencies — every other project that records telemetry does so only through the first-party interface.

## Assumptions

- "Users" of this capability are effectively the application's developer/operator (this is a single-user, self-hosted tool per Constitution Principle IV) — there is no separate end-user-facing observability UI to build; the trace/metrics/log viewing experience is the chosen backend's own UI (Jaeger UI, or Langfuse UI).
- Only one observability backend (Jaeger or Langfuse) is expected to be active at a time; the system does not need to fan telemetry out to both simultaneously.
- No long-term retention is required; ephemeral, container-lifetime data is acceptable for both Jaeger and Langfuse in this phase.
- Existing configuration conventions (per-context `appsettings` sections, environment variable overrides, config read once at process startup) are reused for observability settings rather than introducing a new configuration mechanism.
- The logging assessment (User Story 4 / FR-011) is an analysis deliverable (a written set of findings), not itself an implementation change — remediation of specific findings is expected to happen as part of the structured-logging implementation work in a later planning phase, not as part of this specification.
- This feature does not introduce authentication, authorization, or multi-tenant concerns; that remains explicitly out of scope per Constitution Principle IV.
- WPF is in scope for distributed tracing at parity with Web (FR-004a); the exact mechanism for establishing a trace root from an in-process ViewModel/command call (as opposed to an inbound HTTP request) is a technical-planning decision, not a specification-level one.
- Telemetry redaction (FR-014) applies uniformly across logs, traces, and metrics for both bounded contexts; the specific allow-list of safe attributes (e.g. entity IDs, operation names, transaction *type* but not *amount*) is a technical-planning decision.
- "Technical/runtime metrics" (FR-015) means the metrics produced by OpenTelemetry's standard ASP.NET Core / HttpClient / runtime auto-instrumentation; no custom business-metric instrumentation is required for this feature to be considered complete.
- Where exactly the first-party interface (FR-006) lives — Application itself, or a new dependency-free shared project both Application and Infrastructure can reference — and whether Application code calls it explicitly or through some indirection, is a technical-planning decision, not a specification-level one.
- Implementation of this feature proceeds as a sequence of small pull requests, each touching roughly 5 source/config code files (not counting documentation or test files); a phase that would otherwise require more files than that is expected to be split across multiple PRs rather than delivered as one. This shapes how `/speckit-tasks` groups work, not the requirements themselves.
  - **Exception (added post-implementation, PR #467's review)**: this target assumes heterogeneous changes, where each touched file carries its own review risk. It does not apply to a batch of files that all receive the *identical*, mechanical, already-reviewed-and-approved pattern (e.g. instrumenting N Application services with the same `ITelemetryTracer` span-wrapping shape established in PR 4d/#464) — splitting those further adds PR overhead without adding review safety, since reviewing file 8 of an identical pattern costs the same as reviewing file 4. For this kind of homogeneous, repetitive-pattern work, a larger batch (e.g. all remaining services in one PR) is preferred over arbitrarily chopping at 5.
