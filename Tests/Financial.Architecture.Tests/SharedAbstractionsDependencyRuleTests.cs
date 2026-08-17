using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public class SharedAbstractionsDependencyRuleTests
{
    private static readonly Assembly SharedAbstractionsAssembly =
        ProjectAssembly.Load("Financial.Shared.Abstractions");

    [Fact]
    public void SharedAbstractions_Should_Have_No_Financial_Project_Dependencies()
    {
        var referencedAssemblyNames = ProjectAssembly.GetReferencedAssemblyNames(SharedAbstractionsAssembly);

        referencedAssemblyNames.Should().NotContain(name => name.StartsWith("Financial.", StringComparison.Ordinal));
    }
}
