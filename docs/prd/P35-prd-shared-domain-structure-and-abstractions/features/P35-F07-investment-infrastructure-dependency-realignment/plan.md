# Implementation Plan: F07. Investment Infrastructure Dependency Realignment

**Prerequisites:**
- F01, F02, F04, and F06 merged to `main` (F01/F02/F04 are F07's direct PRD dependencies; F06 established the pattern and the composition-root pull-forward this feature extends)
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting

### Stage 1: Realign Investment.Infrastructure's own dependency

**1. Drop the ProjectReference** - Remove `Financial.Investment.Infrastructure`'s `ProjectReference` to `Financial.Shared.Infrastructure`; confirm `Financial.Shared.Abstractions` stays reachable transitively via the existing `Financial.Investment.Application` reference.

**2. Constructor-inject IJsonStorageFactory** - Change `InvestmentRepositoryFactory`'s constructor to take `IJsonStorageFactory` in place of its current `IRemoteFileClientFactory?`/`ITelemetryTracer?`/`ILogger?` parameters, and have `CreateStorage` call the injected factory directly instead of instantiating one inline.

**3. Update the DI extension method** - `InvestmentInfrastructureServiceCollectionExtensions.AddFinancialInfrastructure` resolves `IJsonStorageFactory` from the container instead of constructing storage inline, and stops registering `ShutdownFlushHostedService<IInvestmentRepository>` itself.

### Stage 2: Give the composition roots their own reference and finish the wiring

**4. Add the explicit ProjectReference** - `Financial.Api.csproj` and `Financial.App.csproj` each gain an explicit `ProjectReference` to `Financial.Shared.Infrastructure`, since Investment.Infrastructure was the last project providing that path to them transitively.

**5. Register the Investment hosted service** - `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` each register `ShutdownFlushHostedService<IInvestmentRepository>` after the `AddFinancialInfrastructure` call. `IJsonStorageFactory`'s own registration already exists from F06 and needs no change.

### Stage 3: Fix up every affected test

**6. Factory and DI-module tests** - Update `InvestmentRepositoryFactoryTests` to construct the factory with a real `JsonStorageFactory`; update `InvestmentInfrastructureServiceCollectionExtensionsTests` to register `IJsonStorageFactory` in its minimal container; remove the now-obsolete hosted-service-registration test.

**7. Extend the relocated hosted-service coverage** - Add a second fact to `ShutdownFlushHostedServiceRegistrationTests` (added in F06) asserting `ShutdownFlushHostedService<IInvestmentRepository>` is also registered in the real host.

### Stage 4: Full verification

**8. Full verification** - Run a full solution build and the full test suite (with coverage settings), confirming `Financial.Investment.Infrastructure` builds standalone with no `Financial.Shared.Infrastructure` reference and no project's behavior regressed.
