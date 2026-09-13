using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

/// <summary>
/// Enforces the vendor-SDK-isolation convention (P49 F01) for the Frankfurter integration:
/// it depends only on the shared kernel, never on either bounded context.
/// </summary>
public class FrankfurterIsolationRuleTests
{
    private static readonly Assembly FrankfurterAssembly =
        ProjectAssembly.Load("Financial.Integrations.Frankfurter");

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-03")]
    public void Frankfurter_Should_Not_Reference_Either_Bounded_Context()
    {
        var referencedAssemblyNames = ProjectAssembly.GetReferencedAssemblyNames(FrankfurterAssembly);

        referencedAssemblyNames.Should().NotContain(
            name => name.StartsWith("Financial.CashFlow.", StringComparison.Ordinal) ||
                    name.StartsWith("Financial.Investment.", StringComparison.Ordinal),
            "the Frankfurter vendor integration must reach only Financial.Shared.Abstractions, matching every other Integrations/* project");
    }

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-02")]
    public void CashFlow_Application_Should_Not_Declare_A_Local_ExchangeRateProvider_Interface()
    {
        var cashFlowApplication = Assembly.Load("Financial.CashFlow.Application");

        cashFlowApplication.GetTypes().Should().NotContain(
            type => type.Name == "IExchangeRateProvider",
            "IExchangeRateProvider must live only in Financial.Shared.Abstractions, not as a second CashFlow-local copy");
    }
}
