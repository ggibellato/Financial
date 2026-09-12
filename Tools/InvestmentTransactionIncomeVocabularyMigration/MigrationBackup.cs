namespace Financial.Investment.TransactionIncomeVocabularyMigration;

/// <summary>
/// Backs up a data file before a migration writes to it in place, following the same naming
/// convention CashFlow's migration tooling uses - this is not shared code (Constitution Principle
/// II: bounded contexts stay isolated), only the same shape.
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
        var backupPath = Path.Combine(directory, $"{name}.backup-migration-{timestamp}{extension}");

        var suffix = 1;
        while (File.Exists(backupPath))
        {
            backupPath = Path.Combine(directory, $"{name}.backup-migration-{timestamp}-{suffix}{extension}");
            suffix++;
        }

        File.Copy(dataPath, backupPath);
        return backupPath;
    }
}
