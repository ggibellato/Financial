using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.EntityReferences;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.EntityReferences;

public class EntityReferenceMigratorTests
{
    private static readonly Guid IncomeSourceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InvestmentAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid IncomeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ExpenseId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TransferId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid BalanceAdjustmentId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid InvestmentSnapshotId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private static string LegacyFixtureJson(string unresolvedBankName = "Barclays") => $$"""
        {
          "Banks": [
            { "Name": "Barclays", "RoundUpEnabled": false, "OpeningBalance": 100.0, "OpeningBalanceDate": "2026-01-01" },
            { "Name": "Trading212", "RoundUpEnabled": true }
          ],
          "IncomeSources": [
            { "Id": "{{IncomeSourceId}}", "Name": "Gleison", "IsActive": true, "Group": "Salary" }
          ],
          "InvestmentAccounts": [
            { "Id": "{{InvestmentAccountId}}", "Name": "ChaseSave", "IsActive": true, "IsLiability": false, "Aliases": [] }
          ],
          "ReserveMovements": [], "CardStatements": [], "RecurringBills": [], "MaeLedgerEntries": [],
          "Incomes": [
            { "Id": "{{IncomeId}}", "Date": "2026-07-01", "IncomeSource": "Gleison", "GrossValue": 3200.0, "NetValue": 2450.0, "Bank": "{{unresolvedBankName}}" }
          ],
          "Expenses": [
            { "Id": "{{ExpenseId}}", "Date": "2026-07-05", "Description": "Groceries", "Value": 50.0, "Category": "Mercado", "PaymentSource": "Barclays", "CardTag": null }
          ],
          "Transfers": [
            { "Id": "{{TransferId}}", "Date": "2026-07-10", "SourceBank": "Barclays", "DestinationBank": "Trading212", "Amount": 100.0, "Note": null }
          ],
          "BalanceAdjustments": [
            { "Id": "{{BalanceAdjustmentId}}", "Date": "2026-07-15", "Bank": "Barclays", "TargetBalance": 500.0, "Delta": 10.0, "Note": null }
          ],
          "InvestmentSnapshots": [
            { "Id": "{{InvestmentSnapshotId}}", "Account": "ChaseSave", "Year": 2026, "Month": 7, "Value": 1000.0 }
          ]
        }
        """;

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-entityref-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Migrate_LegacyShapedFile_AssignsBankIdsAndRewritesEveryReferencingEntity()
    {
        var path = CreateTempFile(LegacyFixtureJson());

        try
        {
            var summary = EntityReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.AlreadyCurrentShape.Should().BeFalse();
                summary.BanksIdAssignedCount.Should().Be(2);
                summary.IncomesMigratedCount.Should().Be(1);
                summary.ExpensesMigratedCount.Should().Be(1);
                summary.TransfersMigratedCount.Should().Be(1);
                summary.BalanceAdjustmentsMigratedCount.Should().Be(1);
                summary.InvestmentSnapshotsMigratedCount.Should().Be(1);
            }

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            using (new AssertionScope())
            {
                var barclays = rewritten.Banks.Should().ContainSingle(b => b.Name == "Barclays").Subject;
                barclays.Id.Should().NotBeEmpty();
                barclays.OpeningBalance.Should().Be(100.0m);
                var trading212 = rewritten.Banks.Should().ContainSingle(b => b.Name == "Trading212").Subject;
                trading212.Id.Should().NotBeEmpty();
                trading212.Id.Should().NotBe(barclays.Id);

                var income = rewritten.Incomes.Should().ContainSingle().Which;
                income.Id.Should().Be(IncomeId);
                income.Bank.Id.Should().Be(barclays.Id);
                income.IncomeSource.Id.Should().Be(IncomeSourceId);

                var expense = rewritten.Expenses.Should().ContainSingle().Which;
                expense.Id.Should().Be(ExpenseId);
                expense.PaymentSourceBank!.Id.Should().Be(barclays.Id);

                var transfer = rewritten.Transfers.Should().ContainSingle().Which;
                transfer.Id.Should().Be(TransferId);
                transfer.SourceBank.Id.Should().Be(barclays.Id);
                transfer.DestinationBank.Id.Should().Be(trading212.Id);

                var adjustment = rewritten.BalanceAdjustments.Should().ContainSingle().Which;
                adjustment.Id.Should().Be(BalanceAdjustmentId);
                adjustment.Bank.Id.Should().Be(barclays.Id);

                var snapshot = rewritten.InvestmentSnapshots.Should().ContainSingle().Which;
                snapshot.Id.Should().Be(InvestmentSnapshotId);
                snapshot.Account.Id.Should().Be(InvestmentAccountId);
            }
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
            EntityReferenceMigrator.Migrate(path);
            var contentAfterFirstRun = File.ReadAllText(path);

            var secondSummary = EntityReferenceMigrator.Migrate(path);

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
            EntityReferenceMigrator.Migrate(path);

            Directory.GetFiles(directory, $"{nameWithoutExtension}.backup-migration-*").Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_LegacyBankShapeWithModernReserveBucketsAndMovements_ResolvesAndPreservesBoth()
    {
        var bucketId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var movementId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var json = $$"""
            {
              "Banks": [
                { "Name": "Barclays", "RoundUpEnabled": false }
              ],
              "IncomeSources": [], "InvestmentAccounts": [],
              "ReserveBuckets": [
                { "Id": "{{bucketId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 33.33 }
              ],
              "ReserveMovements": [
                { "Id": "{{movementId}}", "BucketId": "{{bucketId}}", "Amount": 50.0, "Date": "2026-07-01", "Description": "Deposit" }
              ],
              "CardStatements": [], "RecurringBills": [], "MaeLedgerEntries": [],
              "Incomes": [], "Expenses": [], "Transfers": [], "BalanceAdjustments": [], "InvestmentSnapshots": []
            }
            """;
        var path = CreateTempFile(json);

        try
        {
            var act = () => EntityReferenceMigrator.Migrate(path);

            act.Should().NotThrow();

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            using (new AssertionScope())
            {
                var bucket = rewritten.ReserveBuckets.Should().ContainSingle().Which;
                bucket.Id.Should().Be(bucketId);
                var movement = rewritten.ReserveMovements.Should().ContainSingle().Which;
                movement.Id.Should().Be(movementId);
                movement.Bucket.Should().BeSameAs(bucket);
            }
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_RecordWithUnresolvableBankName_IsFlaggedAndOmittedFromTheRewrittenFile()
    {
        var path = CreateTempFile(LegacyFixtureJson(unresolvedBankName: "NotABank"));

        try
        {
            var summary = EntityReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.IncomesMigratedCount.Should().Be(0);
                summary.UnresolvedIncomes.Should().ContainSingle().Which.Id.Should().Be(IncomeId);
            }

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));
            rewritten.Incomes.Should().BeEmpty();
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
              "Banks": [{ "Id": "88888888-8888-8888-8888-888888888888", "Name": "Barclays", "RoundUpEnabled": false }],
              "IncomeSources": [], "Incomes": [], "Transfers": [], "BalanceAdjustments": []
            }
            """;
        var path = CreateTempFile(currentShapeJson);

        try
        {
            var summary = EntityReferenceMigrator.Migrate(path);

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
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-entityref-missing-{Guid.NewGuid():N}.json");

        var summary = EntityReferenceMigrator.Migrate(missingPath);

        summary.AlreadyCurrentShape.Should().BeTrue();
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
}
