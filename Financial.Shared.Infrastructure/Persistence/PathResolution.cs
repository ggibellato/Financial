namespace Financial.Shared.Infrastructure.Persistence;

public static class PathResolution
{
    /// <summary>Resolves a possibly-relative path against the app's base directory, leaving an already-rooted path untouched.</summary>
    public static string ResolveRelativeToBaseDirectory(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
