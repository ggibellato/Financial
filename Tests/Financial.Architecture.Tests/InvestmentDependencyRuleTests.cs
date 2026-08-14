using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public class InvestmentDependencyRuleTests
{
    private static readonly Assembly DomainAssembly = ProjectAssembly.Load("Financial.Investment.Domain");
    private static readonly Assembly ApplicationAssembly = ProjectAssembly.Load("Financial.Investment.Application");

    [Fact]
    public void Domain_Should_Not_Reference_Application()
    {
        ProjectAssembly.GetReferencedAssemblyNames(DomainAssembly)
            .Should().NotContain("Financial.Investment.Application");
    }

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure()
    {
        ProjectAssembly.GetReferencedAssemblyNames(DomainAssembly)
            .Should().NotContain("Financial.Investment.Infrastructure");
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure()
    {
        ProjectAssembly.GetReferencedAssemblyNames(ApplicationAssembly)
            .Should().NotContain("Financial.Investment.Infrastructure");
    }
}
