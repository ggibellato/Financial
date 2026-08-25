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

    [Fact]
    public void WebPageParser_Should_Not_Reference_Investment_Domain()
    {
        var webPageParserAssembly = ProjectAssembly.Load("Financial.Integrations.WebPageParser");

        ProjectAssembly.GetReferencedAssemblyNames(webPageParserAssembly)
            .Should().NotContain(
                "Financial.Investment.Domain",
                "Integrations/ projects carry no bounded-context types (CLAUDE.md); WebPageParser returns its own WebAssetQuote/WebDividendRecord and Financial.Investment.Infrastructure maps them into Domain value objects");
    }
}
