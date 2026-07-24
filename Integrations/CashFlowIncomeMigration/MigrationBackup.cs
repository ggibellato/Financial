namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowIncomeMigration;

/// <summary>
/// Copies the data file to a timestamped sibling before the migration touches it,
/// so a failed or interrupted run can always be recovered from.
/// </summary>
public static class MigrationBackup
{
    private const string BackupTimestampFormat = "yyyyMMdd-HHmmss";

    public static string Create(string dataPath)
    {
        if (!File.Exists(dataPath))
        {
            throw new FileNotFoundException($"Data file not found at '{dataPath}'.", dataPath);
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataPath))!;
        var name = Path.GetFileNameWithoutExtension(dataPath);
        var extension = Path.GetExtension(dataPath);
        var timestamp = DateTime.Now.ToString(BackupTimestampFormat);
        var backupPath = Path.Combine(directory, $"{name}.backup-income-migration-{timestamp}{extension}");

        var suffix = 1;
        while (File.Exists(backupPath))
        {
            backupPath = Path.Combine(directory, $"{name}.backup-income-migration-{timestamp}-{suffix}{extension}");
            suffix++;
        }

        File.Copy(dataPath, backupPath);
        return backupPath;
    }
}
