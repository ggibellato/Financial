using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

/// <summary>
/// Mechanically enforces the isolation F06/F07/F09 (shared-domain-structure refactor) established:
/// CashFlow.Infrastructure, Investment.Infrastructure, and their Integrations/* projects reach
/// shared contracts only through Financial.Shared.Abstractions, never through
/// Financial.Shared.Infrastructure directly.
/// </summary>
public class SharedInfrastructureIsolationRuleTests
{
    public static TheoryData<string> IsolatedProjects => new()
    {
        "Financial.CashFlow.Infrastructure",
        "Financial.Investment.Infrastructure",
        "Financial.Integrations.GoogleFinancialSupport",
        "Financial.Integrations.WebPageParser",
    };

    [Theory]
    [MemberData(nameof(IsolatedProjects))]
    public void Project_Should_Not_Reference_Financial_Shared_Infrastructure(string assemblyName)
    {
        var referencedAssemblyNames = ProjectAssembly.GetReferencedAssemblyNames(Assembly.Load(assemblyName));

        referencedAssemblyNames.Should().NotContain(
            "Financial.Shared.Infrastructure",
            "only the composition roots (Financial.Api, Financial.App) may reference the concrete shared implementation - everything else depends on Financial.Shared.Abstractions alone");
    }
}
