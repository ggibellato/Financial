using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBucketReferences;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.ReserveBucketReferences;

public class ReserveBucketReferenceMigratorTests
{
    private static readonly Guid MovementId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SeededBucketId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static string LegacyFixtureJson(string bucketName = "Investimento") => $$"""
        {
          "Expenses": [], "CardStatements": [], "RecurringBills": [],
          "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
          "ReserveMovements": [
            { "Id": "{{MovementId}}", "Bucket": "{{bucketName}}", "Amount": 100.0, "Date": "2026-07-01", "Description": "Split" }
          ],
          "Incomes": [], "IncomeSources": [], "Transfers": [], "BalanceAdjustments": [], "Banks": []
        }
        """;

    private static string LegacyFixtureJsonWithSeededBucket() => $$"""
        {
          "Expenses": [], "CardStatements": [], "RecurringBills": [],
          "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
          "ReserveBuckets": [
            { "Id": "{{SeededBucketId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 33.33 }
          ],
          "ReserveMovements": [
            { "Id": "{{MovementId}}", "Bucket": "Investimento", "Amount": 100.0, "Date": "2026-07-01", "Description": "Split" }
          ],
          "Incomes": [], "IncomeSources": [], "Transfers": [], "BalanceAdjustments": [], "Banks": []
        }
        """;

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-bucketref-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    private static void DeleteBackups(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        foreach (var backup in Directory.GetFiles(directory, $"{nameWithoutExtension}.backup-migration-*"))
        {
            File.Delete(backup);
        }
    }

    [Fact]
    public void Migrate_LegacyFileWithNoSeededBuckets_BootstrapsTheCanonicalFourAndRewritesTheMovement()
    {
        var path = CreateTempFile(LegacyFixtureJson());

        try
        {
            var summary = ReserveBucketReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.AlreadyCurrentShape.Should().BeFalse();
                summary.BucketsBootstrappedCount.Should().Be(4);
                summary.MovementsMigratedCount.Should().Be(1);
                summary.UnresolvedMovements.Should().BeEmpty();
            }

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            using (new AssertionScope())
            {
                rewritten.ReserveBuckets.Should().HaveCount(4);
                var movement = rewritten.ReserveMovements.Should().ContainSingle().Which;
                movement.Id.Should().Be(MovementId);
                movement.Bucket.Name.Should().Be("Investimento");
            }
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_LegacyFileWithBucketsAlreadySeeded_RewritesUsingTheExistingBucketInstance()
    {
        var path = CreateTempFile(LegacyFixtureJsonWithSeededBucket());

        try
        {
            var summary = ReserveBucketReferenceMigrator.Migrate(path);

            summary.BucketsBootstrappedCount.Should().Be(0);
            summary.MovementsMigratedCount.Should().Be(1);

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            rewritten.ReserveBuckets.Should().ContainSingle().Which.Id.Should().Be(SeededBucketId);
            rewritten.ReserveMovements.Should().ContainSingle().Which.Bucket.Id.Should().Be(SeededBucketId);
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
        var path = CreateTempFile(LegacyFixtureJson());

        try
        {
            ReserveBucketReferenceMigrator.Migrate(path);
            var contentAfterFirstRun = File.ReadAllText(path);

            var secondSummary = ReserveBucketReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                secondSummary.AlreadyCurrentShape.Should().BeTrue();
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
    public void Migrate_CreatesABackupBeforeWriting()
    {
        var path = CreateTempFile(LegacyFixtureJson());
        var directory = Path.GetDirectoryName(path)!;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(path);

        try
        {
            ReserveBucketReferenceMigrator.Migrate(path);

            Directory.GetFiles(directory, $"{nameWithoutExtension}.backup-migration-*").Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_MovementWithUnresolvableBucketName_IsFlaggedAndOmittedFromTheRewrittenFile()
    {
        var path = CreateTempFile(LegacyFixtureJson(bucketName: "NotABucket"));

        try
        {
            var summary = ReserveBucketReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.MovementsMigratedCount.Should().Be(0);
                summary.UnresolvedMovements.Should().ContainSingle().Which.Id.Should().Be(MovementId);
            }

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));
            rewritten.ReserveMovements.Should().BeEmpty();
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
        var currentShapeJson = """
            {
              "Expenses": [], "ReserveMovements": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "Banks": [], "IncomeSources": [], "ReserveBuckets": [],
              "Incomes": [], "Transfers": [], "BalanceAdjustments": []
            }
            """;
        var path = CreateTempFile(currentShapeJson);

        try
        {
            var summary = ReserveBucketReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.AlreadyCurrentShape.Should().BeTrue();
                File.ReadAllText(path).Should().Be(currentShapeJson);
                Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}.backup-migration-*").Should().BeEmpty();
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
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-bucketref-missing-{Guid.NewGuid():N}.json");

        var summary = ReserveBucketReferenceMigrator.Migrate(missingPath);

        summary.AlreadyCurrentShape.Should().BeTrue();
    }
}
