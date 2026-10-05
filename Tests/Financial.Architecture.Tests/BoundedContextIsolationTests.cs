using FluentAssertions;
using Financial.Architecture.Tests.Infrastructure;

namespace Financial.Architecture.Tests;

[Trait("Category", "Unit")]
public class BoundedContextIsolationTests
{
    public static TheoryData<string, string, string> ContextLayers => new()
    {
        { "CashFlow", "Investment", "Domain" },
        { "CashFlow", "Investment", "Application" },
        { "CashFlow", "Investment", "Infrastructure" },
        { "Investment", "CashFlow", "Domain" },
        { "Investment", "CashFlow", "Application" },
        { "Investment", "CashFlow", "Infrastructure" },
    };

    [Theory]
    [MemberData(nameof(ContextLayers))]
    public void Assembly_Should_Not_Reference_The_Other_BoundedContext(string context, string otherContext, string layer)
    {
        var assembly = ProjectAssembly.Load($"Financial.{context}.{layer}");

        ProjectAssembly.GetReferencedAssemblyNames(assembly)
            .Where(name => name.StartsWith($"Financial.{otherContext}.", StringComparison.Ordinal))
            .Should().BeEmpty($"Financial.{context}.{layer} must stay isolated from the {otherContext} bounded context (CLAUDE.md invariant 3)");
    }
}
