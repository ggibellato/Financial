using System.Reflection;

namespace Financial.Architecture.Tests.Infrastructure;

internal interface IArchitectureQuery
{
    IReadOnlyCollection<string> TypesDependingOn(IEnumerable<Assembly> assemblies, Func<string, bool> isTarget);
}
