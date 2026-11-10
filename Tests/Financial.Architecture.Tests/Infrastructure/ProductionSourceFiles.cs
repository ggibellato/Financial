using Financial.TestUtilities;

namespace Financial.Architecture.Tests.Infrastructure;

internal static class ProductionSourceFiles
{
    private static readonly string[] BuildOutputFolders = ["bin", "obj"];

    private static readonly string[] WebSourceExtensions = [".ts", ".tsx"];

    private static readonly string[] WebTestFolders = ["__tests__", "test", "test-utils"];

    public static IEnumerable<(string RelativePath, string FullPath)> Backend()
    {
        var root = RepoRoot.Find();
        return Directory.EnumerateDirectories(root, "Financial.*")
            .Where(directory => !Path.GetFileName(directory).EndsWith(".Web", StringComparison.Ordinal))
            .SelectMany(directory => FilesUnder(directory, BuildOutputFolders, path => path.EndsWith(".cs", StringComparison.Ordinal)))
            .Select(path => (Path.GetRelativePath(root, path), path));
    }

    public static IEnumerable<(string RelativePath, string FullPath)> Web()
    {
        var root = RepoRoot.Find();
        return FilesUnder(Path.Combine(root, "Financial.Web", "src"), WebTestFolders, IsWebProductionSource)
            .Select(path => (Path.GetRelativePath(root, path), path));
    }

    private static bool IsWebProductionSource(string path) =>
        WebSourceExtensions.Contains(Path.GetExtension(path)) && !Path.GetFileName(path).Contains(".test.", StringComparison.Ordinal);

    private static IEnumerable<string> FilesUnder(string directory, string[] skippedFolders, Func<string, bool> include) =>
        Directory.EnumerateFiles(directory).Where(include).Concat(
            Directory.EnumerateDirectories(directory)
                .Where(child => !skippedFolders.Contains(Path.GetFileName(child)))
                .SelectMany(child => FilesUnder(child, skippedFolders, include)));
}
