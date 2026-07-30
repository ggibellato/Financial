> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# DI Modules (`*ServiceCollectionExtensions.cs`)

Examples: `CashFlowInfrastructureServiceCollectionExtensions`, `GoogleFinancialSupportServiceCollectionExtensions`.

## What to test

- Default behavior when no provider/config is set (e.g., "no provider configured → defaults to LocalJson")
- Misconfiguration is caught at resolution time with a clear error (e.g., unsupported provider string throws `InvalidOperationException` naming the bad value)
- Each configured provider branch actually resolves to the expected implementation type

## Layer assignment

Unit — but "unit" here means building a **real** `IServiceCollection`/`IConfiguration` and a real `ServiceProvider`, then calling `GetRequiredService<T>()`. This is not mocked in any way; the DI container itself is the thing under test. A missing registration, wrong lifetime, or wrong default only surfaces at runtime otherwise — this test catches it at build/test time instead.

## Setup pattern

```csharp
public class CashFlowInfrastructureServiceCollectionExtensionsTests
{
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

        act.Should().Throw<InvalidOperationException>().WithMessage("*NotARealProvider*is not supported*");
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_NoProviderConfigured_DefaultsToLocalJson()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?> { ["CashFlow:DataJsonFile"] = missingPath });

        provider.GetRequiredService<ICashFlowRepository>().Should().NotBeNull();
    }

    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddFinancialCashFlowInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
```

Note: resolution is deliberately lazy here (`GetRequiredService` throws, not `AddFinancialCashFlowInfrastructure` itself) — test the throw at the resolution call, not the registration call, matching the real failure point a misconfigured deployment would hit.

## When to skip

- Don't duplicate every constructor/service branch already covered by that service's own unit tests (`artifacts/application-services.md`, `artifacts/infrastructure-persistence.md`) — this test proves *wiring*, not the service's internal logic

## Examples from project

- `CashFlowInfrastructureServiceCollectionExtensionsTests` — default-provider + unsupported-provider branches
- `GoogleFinancialSupportServiceCollectionExtensionsTests` — same pattern for the Investment-side Google integration registration
