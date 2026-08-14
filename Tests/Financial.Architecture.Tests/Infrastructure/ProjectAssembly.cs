using System.Reflection;

namespace Financial.Architecture.Tests.Infrastructure;

/// <summary>Resolves project assemblies by simple name so tests can assert on their actual references.</summary>
internal static class ProjectAssembly
{
    public static Assembly Load(string simpleAssemblyName) => Assembly.Load(simpleAssemblyName);

    public static IReadOnlyCollection<string> GetReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(referencedAssembly => referencedAssembly.Name!)
            .ToArray();
}
