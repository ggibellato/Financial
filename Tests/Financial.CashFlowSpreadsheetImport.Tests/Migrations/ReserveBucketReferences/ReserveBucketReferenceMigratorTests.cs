using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBucketReferences;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.ReserveBucketReferences;

public class ReserveBucketReferenceMigratorTests : FileMigratorContractTests
{
    protected override string LegacyFileJson() => LegacyFixtureJson();

    protected override string CurrentShapeJson() => """
        {
          "Expenses": [], "ReserveMovements": [], "CardStatements": [], "RecurringBills": [],
          "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
          "Banks": [], "IncomeSources": [], "ReserveBuckets": [],
          "Incomes": [], "Transfers": [], "BalanceAdjustments": []
        }
        """;

    protected override bool MigrateReportsAlreadyCurrentShape(string path) => ReserveBucketReferenceMigrator.Migrate(path).AlreadyCurrentShape;

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

    [Fact]
    public void Migrate_LegacyFileWithNoSeededBuckets_BootstrapsTheCanonicalFiveAndRewritesTheMovement()
    {
        var path = CreateTempFile(LegacyFixtureJson());

        try
        {
            var summary = ReserveBucketReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.AlreadyCurrentShape.Should().BeFalse();
                summary.BucketsBootstrappedCount.Should().Be(5);
                summary.MovementsMigratedCount.Should().Be(1);
                summary.UnresolvedMovements.Should().BeEmpty();
            }

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            using (new AssertionScope())
            {
                rewritten.ReserveBuckets.Should().HaveCount(5);
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
}
