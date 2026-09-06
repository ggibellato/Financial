> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Dependency-Injection Modules (`*/DependencyInjection/*ServiceCollectionExtensions.cs`, `Financial.Api/Program.cs`, `Financial.App/App.xaml.cs`)

## What to test

- `GetRequiredService<T>()` succeeds for every interface the module promises
  (`CashFlowServiceRegistrationTests.ResolvesAllTwelveCashFlowServices` lists them one by one —
  add a line when you add a service).
- **Provider selection branches**: `CashFlow:Repository:Provider` / `Investment:Repository:Provider`
  unset → `LocalJson`; `"GoogleDrive"` → remote storage; `"NotARealProvider"` →
  `InvalidOperationException` with `*is not supported*` on resolution, not on registration.
- **Configured-library contracts**: `AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>`
  sets `BaseAddress` to `https://api.frankfurter.app/`; `AddObservability` binds
  `ObservabilityOptions` defaults (`Enabled=false`, `Backend=Jaeger`, `Endpoint=http://localhost:4317`).
- **Composition-root invariants**: `IJsonStorageFactory` and `ITelemetryTracer` are registered
  by `Program.cs` / `App.xaml.cs` before the `Add*Infrastructure` calls; the DI test mirrors that
  (see the comment block in `CashFlowInfrastructureServiceCollectionExtensionsTests.BuildServiceProvider`).
- Hosted services: `ShutdownFlushHostedService<ICashFlowRepository>` is registered
  (`ShutdownFlushHostedServiceRegistrationTests`).
- Negative: the unsupported-provider branch and a missing required setting.

## Layer assignment

- **Integration** — a real `ServiceCollection` + `ConfigurationBuilder().AddInMemoryCollection`
  + `BuildServiceProvider()`; nothing faked except the data path (a temp file). This is the
  fundamentals' "module/DI wiring" row: compile-time cannot catch a missing registration.
- The API-level version (the whole `Program`) is exercised by every `ApiEndpointTests`
  subclass; the WPF version by `Tests/Financial.Presentation.Tests/DependencyInjection/`.
- No Unit layer (nothing to isolate), no E2E of its own.

## Setup pattern

Copied from `Tests/Financial.CashFlow.Infrastructure.Tests/DependencyInjection/CashFlowInfrastructureServiceCollectionExtensionsTests.cs`:

```csharp
private static IServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(settings)
        .Build();

    var services = new ServiceCollection();
    services.AddSingleton<Financial.Shared.Abstractions.Observability.ITelemetryTracer>(
        Financial.Shared.Abstractions.Observability.NoOpTelemetryTracer.Instance);
    services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
    services.AddFinancialCashFlowInfrastructure(configuration);
    return services.BuildServiceProvider();
}

[Fact]
public void AddFinancialCashFlowInfrastructure_UnsupportedProvider_ThrowsOnRepositoryResolution()
{
    var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
    var provider = BuildServiceProvider(new Dictionary<string, string?>
    {
        ["CashFlow:Repository:Provider"] = "NotARealProvider",
        ["CashFlow:DataJsonFile"] = missingPath
    });

    Action act = () => provider.GetRequiredService<ICashFlowRepository>();

    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*NotARealProvider*is not supported*");
}
```

Point `DataJsonFile` at a `Guid`-named temp path (or `TestDataPaths.DataJsonFile` for
Investment, which is read-only) — never at `data/`.

## When to skip

- Re-resolving every service in every Infrastructure test — one registration test per module.
- Asserting lifetimes (`Singleton` vs `Transient`) unless a bug depended on it.

## Examples from project

- `Tests/Financial.CashFlow.Infrastructure.Tests/DependencyInjection/CashFlowInfrastructureServiceCollectionExtensionsTests.cs` — Integration; provider branches.
- `Tests/Financial.Investment.Infrastructure.Tests/DependencyInjection/InvestmentInfrastructureServiceCollectionExtensionsTests.cs` — Integration; same shape for Investment.
- `Tests/Financial.Presentation.Tests/DependencyInjection/CashFlowServiceRegistrationTests.cs` and `ObservabilityServiceRegistrationTests.cs` — Integration; the WPF composition root's promises.
- `Tests/Financial.GoogleIntegrations.Tests/GoogleDriveServiceCollectionExtensionsTests.cs` — Integration; `AddGoogleDriveFileClient` registers `GoogleFileClientFactory` as `IRemoteFileClientFactory`.
- `Tests/Financial.Api.Tests/ShutdownFlushHostedServiceRegistrationTests.cs` — Integration; hosted-service registration through the real host.
