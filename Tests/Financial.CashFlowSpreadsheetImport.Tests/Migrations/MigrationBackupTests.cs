using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations;

public class MigrationBackupTests
{
    [Fact]
    public void Create_CopiesTheDataFileToATimestampedSibling()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var dataPath = Path.Combine(directory.FullName, "data-cashflow.json");
            File.WriteAllText(dataPath, "{\"Expenses\":[]}");

            var backupPath = MigrationBackup.Create(dataPath);

            File.Exists(backupPath).Should().BeTrue();
            Path.GetDirectoryName(backupPath).Should().Be(directory.FullName);
            Path.GetFileName(backupPath).Should().StartWith("data-cashflow.backup-migration-").And.EndWith(".json");
            File.ReadAllText(backupPath).Should().Be("{\"Expenses\":[]}");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Create_CalledTwice_ProducesDistinctBackups()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var dataPath = Path.Combine(directory.FullName, "data-cashflow.json");
            File.WriteAllText(dataPath, "{}");

            var first = MigrationBackup.Create(dataPath);
            var second = MigrationBackup.Create(dataPath);

            second.Should().NotBe(first);
            File.Exists(first).Should().BeTrue();
            File.Exists(second).Should().BeTrue();
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Create_WhenSourceMissing_Throws()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        var act = () => MigrationBackup.Create(missingPath);

        act.Should().Throw<FileNotFoundException>();
    }
}
