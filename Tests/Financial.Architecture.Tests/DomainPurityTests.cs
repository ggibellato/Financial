using FluentAssertions;
using Financial.Architecture.Tests.Infrastructure;

namespace Financial.Architecture.Tests;

public class DomainPurityTests
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "Microsoft.AspNetCore",
        "System.Text.Json",
        "Financial.CashFlow.Infrastructure",
        "Financial.Investment.Infrastructure",
        "Financial.Shared.Infrastructure",
    ];

    [Theory]
    [InlineData("Financial.CashFlow.Domain")]
    [InlineData("Financial.Investment.Domain")]
    public void Domain_Should_Not_Reference_Framework_Or_Infrastructure(string domainAssemblyName)
    {
        var assembly = ProjectAssembly.Load(domainAssemblyName);

        ProjectAssembly.GetReferencedAssemblyNames(assembly)
            .Where(name => ForbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .Should().BeEmpty($"{domainAssemblyName} must not depend on a web framework, a serializer or Infrastructure (CLAUDE.md invariant 1)");
    }
}
