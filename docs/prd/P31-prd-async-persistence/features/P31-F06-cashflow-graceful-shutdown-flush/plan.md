# Implementation Plan: F06. CashFlow Graceful Shutdown Flush

**Prerequisites:**
- F04 (CashFlow Debounced Wiring), already implemented — provides the `ISyncStatusProvider.FlushAsync()` capability this feature wires into a lifecycle hook

### Stage 1: Hosted Service

**1. Package Reference** - Add the `Microsoft.Extensions.Hosting.Abstractions` package reference to `Financial.CashFlow.Infrastructure`, per the spec's version/scope decision.
**2. CashFlowShutdownFlushHostedService** - Add the hosted service, delegating `StopAsync` to the resolved `ICashFlowRepository`'s `ISyncStatusProvider.FlushAsync()` when applicable, per the spec.
**3. DI Registration** - Register the hosted service inside `AddFinancialCashFlowInfrastructure`, the single extension both `Financial.Api` and `Financial.App` already call.

### Stage 2: Tests

**4. Hosted Service Branching Coverage** - Add `CashFlowShutdownFlushHostedServiceTests` covering both the `ISyncStatusProvider` and non-`ISyncStatusProvider` repository cases, per the spec's testing strategy.
**5. DI Registration Coverage** - Extend `CashFlowInfrastructureServiceCollectionExtensionsTests` to prove the hosted service is actually registered, per the spec's testing strategy.
