namespace Financial.Shared.Infrastructure.Persistence;

public static class PathResolution
{
    public static string ResolveRelativeToBaseDirectory(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
