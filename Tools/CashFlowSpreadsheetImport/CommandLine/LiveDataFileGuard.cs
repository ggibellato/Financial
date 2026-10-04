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

        var liveFile = Path.GetFullPath(Path.Combine(repositoryRoot, LiveDataRelativePath));
        return string.Equals(Path.GetFullPath(outputPath), liveFile, StringComparison.OrdinalIgnoreCase);
    }

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
