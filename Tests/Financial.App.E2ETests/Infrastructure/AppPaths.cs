namespace Financial.App.E2ETests.Infrastructure;

internal static class AppPaths
{
    private const string ExeName = "Financial.Presentation.App.exe";

#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    public static string TempRoot { get; } = Path.Combine(Path.GetTempPath(), "financial-app-e2e");

    public static string TestDataDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "TestData");

    public static string ArtifactsRoot => Path.Combine(RepoRoot(), "TestResults", "e2e-artifacts");

    public static string ExePath()
    {
        var configured = Environment.GetEnvironmentVariable("FINANCIAL_APP_EXE");
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(RepoRoot(), "Financial.App", "bin", Configuration, "net10.0-windows", ExeName)
            : configured;

        return File.Exists(path)
            ? path
            : throw new FileNotFoundException($"Financial.App executable not found at '{path}'. Build Financial.App ({Configuration}) or set FINANCIAL_APP_EXE.");
    }

    public static void EnsureInsideTempRoot(string path)
    {
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(TempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to launch against data outside {TempRoot}: {full}");
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Financial.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (Financial.slnx not found in any ancestor directory).");
    }
}
