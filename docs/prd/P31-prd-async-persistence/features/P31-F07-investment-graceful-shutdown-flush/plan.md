# Implementation Plan: F07. Investment Graceful Shutdown Flush

**Prerequisites:**
- F05 (Investment Debounced Wiring), already implemented — provides the `ISyncStatusProvider.FlushAsync()` capability this feature wires into a lifecycle hook
- F06 (CashFlow Graceful Shutdown Flush), already implemented — this feature mirrors its exact pattern for Investment

### Stage 1: Hosted Service

**1. Package Reference** - Add the `Microsoft.Extensions.Hosting.Abstractions` package reference to `Financial.Investment.Infrastructure`, mirroring F06.
**2. InvestmentShutdownFlushHostedService** - Add the hosted service, delegating `StopAsync` to the resolved `IRepository`'s `ISyncStatusProvider.FlushAsync()` when applicable, per the spec.
**3. DI Registration** - Register the hosted service inside `AddFinancialInfrastructure`, the single extension both `Financial.Api` and `Financial.App` already call.

### Stage 2: Tests

**4. Hosted Service Branching Coverage** - Add `InvestmentShutdownFlushHostedServiceTests` covering both the `ISyncStatusProvider` and non-`ISyncStatusProvider` repository cases, per the spec's testing strategy.
**5. DI Registration Coverage** - Extend `InfrastructureServiceCollectionExtensionsTests` to prove the hosted service is actually registered, per the spec's testing strategy.
