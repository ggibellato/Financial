using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public class SharedInfrastructureDependencyRuleTests
{
    private static readonly Assembly SharedInfrastructureAssembly =
        ProjectAssembly.Load("Financial.Shared.Infrastructure");

    [Fact]
    public void SharedInfrastructure_Should_Not_Reference_CashFlow_Or_Investment_Assemblies()
    {
        var referencedAssemblyNames = ProjectAssembly.GetReferencedAssemblyNames(SharedInfrastructureAssembly);

        referencedAssemblyNames.Should().NotContain(name =>
            name.Contains("CashFlow", StringComparison.Ordinal) ||
            name.Contains("Investment", StringComparison.Ordinal));
    }
}
