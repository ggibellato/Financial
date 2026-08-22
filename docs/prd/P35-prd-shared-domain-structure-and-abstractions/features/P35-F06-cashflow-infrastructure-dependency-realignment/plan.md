# Implementation Plan: F06. CashFlow Infrastructure Dependency Realignment

**Prerequisites:**
- F01, F02, F03, F04, and F05 merged to `main` (F01/F02/F04 are F06's direct PRD dependencies; F03/F05 already landed as part of Wave 1)
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting

### Stage 1: Give JsonStorageFactory a DI-friendly constructor

**1. Optional remote-file-client parameter** - Add a `= null` default to `JsonStorageFactory`'s `remoteFileClientFactory` constructor parameter, so the built-in DI container can construct it via reflection wherever `IRemoteFileClientFactory` isn't registered.

### Stage 2: Realign CashFlow.Infrastructure's own dependency

**2. Drop the ProjectReference** - Remove `Financial.CashFlow.Infrastructure`'s `ProjectReference` to `Financial.Shared.Infrastructure`; confirm `Financial.Shared.Abstractions` stays reachable transitively via the existing `Financial.CashFlow.Application` reference.

**3. Constructor-inject IJsonStorageFactory** - Change `CashFlowRepositoryFactory`'s constructor to take `IJsonStorageFactory` in place of its current `IRemoteFileClientFactory?`/`ITelemetryTracer?`/`ILogger?` parameters, and have `CreateStorage` call the injected factory directly instead of instantiating one inline.

**4. Update the DI extension method** - `CashFlowInfrastructureServiceCollectionExtensions.AddFinancialCashFlowInfrastructure` resolves `IJsonStorageFactory` from the container instead of constructing storage inline, and stops registering `ShutdownFlushHostedService<ICashFlowRepository>` itself.

### Stage 3: Move the composition-root wiring this change now depends on

**5. Register at both composition roots** - `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` each register `IJsonStorageFactory` (before the `AddFinancialInfrastructure`/`AddFinancialCashFlowInfrastructure` calls) and `ShutdownFlushHostedService<ICashFlowRepository>` (after them) — the CashFlow-relevant slice of F08's own scope, pulled forward so `main` keeps working end-to-end once this PR merges alone.

### Stage 4: Fix up every affected test

**6. Factory and DI-module tests** - Update `CashFlowRepositoryFactoryTests` to construct the factory with a real `JsonStorageFactory`; update `CashFlowInfrastructureServiceCollectionExtensionsTests`, `CashFlowServiceRegistrationTests`, and `ObservabilityServiceRegistrationTests` to register `IJsonStorageFactory` in their minimal containers; remove the now-obsolete hosted-service-registration test from `CashFlowInfrastructureServiceCollectionExtensionsTests`.

**7. Cover the relocated hosted-service registration** - Add a new API-level test that boots the real host and asserts `ShutdownFlushHostedService<ICashFlowRepository>` is registered, replacing the coverage the removed unit test provided.

### Stage 5: Full verification

**8. Full verification** - Run a full solution build and the full test suite (with coverage settings), confirming `Financial.CashFlow.Infrastructure` builds standalone with no `Financial.Shared.Infrastructure` reference and no project's behavior regressed.
