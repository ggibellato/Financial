# Implementation Plan: F05. Observability Namespace Reorganization

**Prerequisites:**
- F01, F02, F03, and F04 merged to `main` (F05 completes Wave 1)
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting

### Stage 1: Move the six Observability files

**1. New Observability namespace** - Create an `Observability/` folder under `Financial.Shared.Abstractions` and move `ITelemetryTracer`, `ITelemetrySpan`, `NoOpTelemetryTracer`, `TelemetryAttributeKeys` (with its sibling `TelemetryOperationResults`), `TelemetrySpanExtensions`, and `TelemetryTracerExtensions` into it unchanged. The flat `Financial.Shared.Abstractions` namespace has nothing left in it afterward.

### Stage 2: Fix up every consumer across the solution

**2. Mechanical using-statement sweep** - Update every file across `Financial.CashFlow.Application`, `Financial.Investment.Application`, both bounded contexts' Infrastructure projects, `Financial.Shared.Infrastructure`, `Financial.Api`, `Financial.App`, `Integrations/*`, and their test projects so they resolve the relocated types from the new namespace. No logic changes anywhere.

**3. Fully-qualified reference fix** - Update the two test files that reference `ITelemetryTracer`/`NoOpTelemetryTracer` by fully-qualified name instead of a `using` directive.

### Stage 3: Full verification

**4. Full verification** - Run a full solution build and the full test suite (with coverage settings), confirming `ObservabilityIsolationRuleTests` passes unmodified and no project's behavior changed. This closes out Wave 1 of the PRD.
