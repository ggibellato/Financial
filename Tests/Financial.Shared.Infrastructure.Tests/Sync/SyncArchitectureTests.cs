using Financial.Shared.Infrastructure.Sync;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Sync;

public class SyncArchitectureTests
{
    [Fact]
    public void SharedInfrastructure_Should_Not_Reference_Either_Bounded_Context()
    {
        var referencedAssemblyNames = typeof(SyncStatus).Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name);

        referencedAssemblyNames.Should().NotContain(name =>
            name!.Contains("CashFlow", StringComparison.Ordinal) ||
            name!.Contains("Investment", StringComparison.Ordinal));
    }
}
