using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public class CashFlowDependencyRuleTests
{
    private static readonly Assembly DomainAssembly = ProjectAssembly.Load("Financial.CashFlow.Domain");
    private static readonly Assembly ApplicationAssembly = ProjectAssembly.Load("Financial.CashFlow.Application");

    [Fact]
    public void Domain_Should_Not_Reference_Application()
    {
        ProjectAssembly.GetReferencedAssemblyNames(DomainAssembly)
            .Should().NotContain("Financial.CashFlow.Application");
    }

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure()
    {
        ProjectAssembly.GetReferencedAssemblyNames(DomainAssembly)
            .Should().NotContain("Financial.CashFlow.Infrastructure");
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure()
    {
        ProjectAssembly.GetReferencedAssemblyNames(ApplicationAssembly)
            .Should().NotContain("Financial.CashFlow.Infrastructure");
    }
}
