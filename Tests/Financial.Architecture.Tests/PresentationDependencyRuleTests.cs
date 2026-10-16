using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

/// <summary>
/// Financial.Api is the composition root and is expected to reference both bounded contexts'
/// Application and Infrastructure layers (see CLAUDE.md). Nothing previously stopped it from
/// picking up an arbitrary new Financial.* project reference unnoticed, so this pins it to its
/// current, reviewed set — an addition here must be a deliberate edit to the
/// allowlist, not a silent side effect of an unrelated change.
/// </summary>
[Trait("Category", "Unit")]
public class PresentationDependencyRuleTests
{
    private static readonly IReadOnlyCollection<string> ApiAllowedFinancialAssemblies = new[]
    {
        "Financial.CashFlow.Application",
        "Financial.CashFlow.Infrastructure",
        "Financial.Investment.Application",
        "Financial.Investment.Domain",
        "Financial.Investment.Infrastructure",
        "Financial.Shared.Abstractions",
        "Financial.Shared.Infrastructure",
        "Financial.Integrations.Observability",
        "Financial.Integrations.GoogleDrive",
        "Financial.Integrations.GoogleCalendar",
    };

    [Fact]
    public void Api_Should_Only_Reference_The_Allowed_Financial_Assemblies()
    {
        var referencedAssemblyNames = ProjectAssembly.GetReferencedAssemblyNames(ProjectAssembly.Load("Financial.Api"));

        referencedAssemblyNames
            .Where(name => name.StartsWith("Financial.", StringComparison.Ordinal))
            .Should().BeSubsetOf(
                ApiAllowedFinancialAssemblies,
                "Financial.Api is a composition root and may reference both bounded contexts' Application/Infrastructure, " +
                "but any other Financial.* reference is a deliberate architecture decision, not an accident");
    }
}
