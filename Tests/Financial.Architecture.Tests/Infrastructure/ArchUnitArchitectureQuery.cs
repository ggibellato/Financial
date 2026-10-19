using Assembly = System.Reflection.Assembly;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Loader;

namespace Financial.Architecture.Tests.Infrastructure;

internal sealed class ArchUnitArchitectureQuery : IArchitectureQuery
{
    public IReadOnlyCollection<string> TypesDependingOn(IEnumerable<Assembly> assemblies, Func<string, bool> isTarget)
    {
        ArgumentNullException.ThrowIfNull(isTarget);

        var architecture = new ArchLoader().LoadAssemblies([.. assemblies]).Build();

        return architecture.Types
            .Where(type => type.Dependencies.Any(dependency => TargetNames(dependency).Any(isTarget)))
            .Select(type => OutermostTypeName(type.FullName))
            .Distinct()
            .ToArray();
    }

    private static IEnumerable<string> TargetNames(ITypeDependency dependency)
    {
        yield return dependency.Target.FullName;

        if (dependency is IMemberMemberDependency memberDependency)
        {
            yield return memberDependency.TargetMember.FullName;
        }
    }

    private static string OutermostTypeName(string fullName) => fullName.Split('+', '/')[0];
}
