# Phase 0 Research: Application Observability (revision 3)

This is the third pass at this research. Revision 2 isolated OpenTelemetry into `Integrations/Observability` correctly, but put the `ITelemetryTracer` interface in `Financial.Shared.Infrastructure` and used a `System.Reflection.DispatchProxy` decorator to reach Application-layer code without Application depending on an Infrastructure project. That was built, tested, and passed architecture review — then reverted once the user, after asking how the decorator worked, preferred a simpler design. This revision reflects that. D2, D6, D7, and D8's shape are unchanged from revision 2.

## D1 (revised): `ITelemetryTracer` lives in a new, dependency-free `Financial.Shared.Abstractions` project

**Decision**: Add `Financial.Shared.Abstractions` — a project with **zero** package references and **zero** project references, sitting parallel to each bounded context's Domain project in the dependency graph rather than above it. `ITelemetryTracer`/`ITelemetrySpan`/`TelemetryAttributeKeys` move there from `Financial.Shared.Infrastructure`. Because it's dependency-free, **Application projects can reference it directly** — the same way they already depend on their own Domain project — without violating Clean Architecture's "Application depends on Domain only" rule, since depending on a pure, context-agnostic contract is not the same as depending on Infrastructure.

`Integrations/Observability` still exists exactly as revision 2 designed it (the sole OpenTelemetry SDK owner), but now implements an interface defined in `Financial.Shared.Abstractions` instead of `Financial.Shared.Infrastructure`.

**Rationale**: this is the more correct fix for the problem revision 2's decorator was working around. The decorator existed *only* because the interface was in the wrong project; once the interface moves to a project Application can legitimately depend on, there's no remaining Clean-Architecture obstacle to Application calling it directly, and the reflection-based interception mechanism — a genuinely new pattern for this codebase — becomes unnecessary.

**Alternatives considered**:
- *Keep `ITelemetryTracer` in `Financial.Shared.Infrastructure`, use `TracingDispatchProxy`* (revision 2's design) — rejected by the user after review: achieves "zero Application code changes" but does so by hiding *where* spans get created behind reflection, which is less illustrative for a project whose stated goal is learning observability specifically, and introduces async/exception-unwrapping subtleties (verified correct via 5 unit tests, but non-trivial to get right — see the reverted PR's commit history on this branch for the two real bugs caught by architecture review).
- *Duplicate the interface into each bounded context's own Application project* — rejected: violates DRY for no benefit once a shared, dependency-free project is available.

## D2: Web frontend is not directly instrumented (unchanged)

**Decision**: `Financial.Web` requires no OpenTelemetry JS/browser SDK. The trace root for a web-originated request is created server-side by `OpenTelemetry.Instrumentation.AspNetCore`'s automatic instrumentation, registered inside `Integrations/Observability`, the moment the HTTP request reaches `Financial.Api`.

**Rationale**: unchanged — FR-004 only requires correlation from "the backend controller" onward; a browser SDK is more moving parts than required, against Constitution Principle IV.

## D3 (revised): Application-layer "use case" spans via explicit `ITelemetryTracer.StartSpan(...)` calls

**Decision**: Application-layer service methods call `_tracer.StartSpan("CashFlow.CreateExpense")` (or similar) explicitly, at the start of the method body, using a `using` statement so the span closes when the method returns (or throws). `ITelemetryTracer` is injected into each Application service constructor exactly like `ILogger<T>` already is in `CardStatementService` today — a familiar, already-established DI pattern in this codebase, not a new one.

**Consequence**: Application code *does* change for this feature — roughly one `using var span = ...` line (plus the constructor parameter) per traced use case, ~30 call sites total across both bounded contexts. This is spread across several small PRs (one bounded context, or a handful of services, per PR) to stay within the ~5-file target, the same way the decorator's wiring was already going to be split.

**Rationale**: once `ITelemetryTracer` lives in a project Application can depend on (D1), explicit calls are the simplest, most transparent, and most standard way to add tracing — it's how virtually every OpenTelemetry getting-started guide teaches the concept, which matters for a project whose explicit goal is learning to do this properly. It also gives each call site full control over span attributes (e.g. `entity.id` for the specific expense being created), which the decorator's fixed generic attribute set could not do without further extension.

**Alternatives considered**: the `TracingDispatchProxy`/`DispatchProxy` decorator from revision 2 (rejected — see D1) and Scrutor-based decoration (rejected in revision 2 already, for adding a dependency the BCL already made unnecessary — now moot since there's no decorator at all).

## D4: Structured log correlation stays on Serilog, OTel export wired only inside Integrations/Observability (unchanged)

**Decision**: unchanged from revision 2. `ILogger<T>` remains the logging abstraction; `Financial.Api`/`Financial.App`'s `UseSerilog(...)` gains a sink via a call into `Integrations/Observability` (e.g. `loggerConfiguration.WriteToObservability(configuration)`), keeping `Serilog.Sinks.OpenTelemetry` confined to that project per FR-005.

## D5: No-op `ITelemetryTracer` via the Null Object pattern (unchanged)

**Decision**: unchanged. `AddObservability(...)` always registers `ITelemetryTracer` — `NoOpTelemetryTracer` when disabled, `OpenTelemetryTracer` when enabled — so `GetRequiredService<ITelemetryTracer>()` never throws (FR-006a), and Application code calling `_tracer.StartSpan(...)` never needs to branch on whether observability is on.

## D6: Logging-audit finding (unchanged — grounds FR-011 / User Story 4)

**Decision**: carried forward unchanged. See [logging-audit.md](./logging-audit.md).

## D7: Langfuse local stack sourced from upstream, not reinvented (unchanged)

**Decision**: carried forward unchanged from revision 2.

## D8: Observability configuration shape — self-contained inside Integrations/Observability (unchanged)

**Decision**: unchanged from revision 2 — `ObservabilityOptions`/`ObservabilityBackend` live in `Integrations/Observability`, not `Financial.Shared.Abstractions` (which stays pure contracts with no configuration surface). See [contracts/observability-configuration-contract.md](./contracts/observability-configuration-contract.md).

## D9 (new): `Financial.Architecture.Tests` gets a new dependency-rule assertion

**Decision**: add `SharedAbstractionsDependencyRuleTests` asserting `Financial.Shared.Abstractions`'s compiled assembly has zero referenced project assemblies (only BCL/framework references, if any at all). This documents and mechanically enforces the "dependency-free floor" property D1 relies on — without it, nothing stops a future change from quietly adding a dependency to `Financial.Shared.Abstractions` and breaking the reason Application is allowed to reference it.

**Rationale**: the existing `Application_Should_Not_Reference_Infrastructure` tests only check that Application doesn't reference *bounded-context* Infrastructure projects — they don't currently check `Financial.Shared.Infrastructure` at all (a pre-existing gap noted in the Constitution as a manual-review responsibility). Rather than closing that unrelated gap, this feature adds a positive-space guarantee about the new project instead, which is the property this feature's whole design actually depends on.
