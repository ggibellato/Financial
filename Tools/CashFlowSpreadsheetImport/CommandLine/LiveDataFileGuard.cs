namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;

public static class LiveDataFileGuard
{
    private const string RepositoryMarkerFile = "Financial.slnx";
    private const string LiveDataRelativePath = "data/data-cashflow.json";

    public static bool IsLiveDataFile(string outputPath, string startDirectory)
    {
        var repositoryRoot = FindRepositoryRoot(startDirectory);
        if (repositoryRoot is null)
        {
            return false;
        }

        return IsSamePath(outputPath, Path.Combine(repositoryRoot, LiveDataRelativePath));
    }

    public static bool IsSamePath(string path, string otherPath) =>
        string.Equals(Path.GetFullPath(path), Path.GetFullPath(otherPath), StringComparison.OrdinalIgnoreCase);

    private static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, RepositoryMarkerFile)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
