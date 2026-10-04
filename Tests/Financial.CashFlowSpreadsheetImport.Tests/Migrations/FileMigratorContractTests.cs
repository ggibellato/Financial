using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations;

public abstract class FileMigratorContractTests
{
    protected abstract string LegacyFileJson();

    protected abstract string CurrentShapeJson();

    protected abstract bool MigrateReportsAlreadyCurrentShape(string path);

    protected static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-migration-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    protected static string[] BackupsOf(string path) =>
        Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}.backup-migration-*");

    protected static void DeleteBackups(string path)
    {
        foreach (var backup in BackupsOf(path))
        {
            File.Delete(backup);
        }
    }

    [Fact]
    public void Migrate_CreatesABackupBeforeWriting()
    {
        var path = CreateTempFile(LegacyFileJson());

        try
        {
            MigrateReportsAlreadyCurrentShape(path);

            BackupsOf(path).Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_SecondRunOnAlreadyMigratedFile_MakesNoFurtherChanges()
    {
        var path = CreateTempFile(LegacyFileJson());

        try
        {
            MigrateReportsAlreadyCurrentShape(path);
            var contentAfterFirstRun = File.ReadAllText(path);

            var secondRunWasNoOp = MigrateReportsAlreadyCurrentShape(path);

            using (new AssertionScope())
            {
                secondRunWasNoOp.Should().BeTrue();
                File.ReadAllText(path).Should().Be(contentAfterFirstRun);
            }
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_FileAlreadyInCurrentShape_ReturnsNoOpSummaryAndTouchesNothing()
    {
        var currentShapeJson = CurrentShapeJson();
        var path = CreateTempFile(currentShapeJson);

        try
        {
            var wasNoOp = MigrateReportsAlreadyCurrentShape(path);

            using (new AssertionScope())
            {
                wasNoOp.Should().BeTrue();
                File.ReadAllText(path).Should().Be(currentShapeJson);
                BackupsOf(path).Should().BeEmpty();
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Migrate_FileDoesNotExist_ReturnsNoOpSummary()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-migration-missing-{Guid.NewGuid():N}.json");

        MigrateReportsAlreadyCurrentShape(missingPath).Should().BeTrue();
    }
}
