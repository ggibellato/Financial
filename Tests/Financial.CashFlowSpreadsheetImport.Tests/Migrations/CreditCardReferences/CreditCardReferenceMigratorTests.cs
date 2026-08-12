using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.CreditCardReferences;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.CreditCardReferences;

public class CreditCardReferenceMigratorTests
{
    private static readonly Guid ExpenseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CardStatementId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SeededCardId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ViagemCategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // Category is already seeded and referenced by CategoryId here, not by a legacy Category
    // string: chronologically, P30's Category migration only ever runs after P29's CreditCard
    // migration in this codebase's history, so any file still needing this CardTag/Card rewrite
    // has already been through Category migration.
    private static string LegacyFixtureJson(string expenseCardName = "BA Amex", string statementCardName = "Chase Master 4023") => $$"""
        {
          "Banks": [], "IncomeSources": [], "InvestmentAccounts": [], "ReserveBuckets": [],
          "ReserveMovements": [], "RecurringBills": [], "MaeLedgerEntries": [],
          "Incomes": [], "Transfers": [], "BalanceAdjustments": [], "InvestmentSnapshots": [],
          "Categories": [
            { "Id": "{{ViagemCategoryId}}", "Name": "Viagem", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "{{ExpenseId}}", "Date": "2026-07-05", "Description": "Flight", "Value": 200.0, "CategoryId": "{{ViagemCategoryId}}", "PaymentSourceBankId": null, "CardTag": "{{expenseCardName}}" }
          ],
          "CardStatements": [
            { "Id": "{{CardStatementId}}", "Card": "{{statementCardName}}", "Year": 2026, "Month": 7, "IsPaid": false }
          ]
        }
        """;

    private static string LegacyFixtureJsonWithSeededCard() => $$"""
        {
          "Banks": [], "IncomeSources": [], "InvestmentAccounts": [], "ReserveBuckets": [],
          "ReserveMovements": [], "RecurringBills": [], "MaeLedgerEntries": [],
          "Incomes": [], "Transfers": [], "BalanceAdjustments": [], "InvestmentSnapshots": [],
          "CreditCards": [
            { "Id": "{{SeededCardId}}", "Name": "BA Amex", "IsActive": true, "NextInvoiceDueDate": null }
          ],
          "Categories": [
            { "Id": "{{ViagemCategoryId}}", "Name": "Viagem", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "{{ExpenseId}}", "Date": "2026-07-05", "Description": "Flight", "Value": 200.0, "CategoryId": "{{ViagemCategoryId}}", "PaymentSourceBankId": null, "CardTag": "BA Amex" }
          ],
          "CardStatements": [
            { "Id": "{{CardStatementId}}", "Card": "BA Amex", "Year": 2026, "Month": 7, "IsPaid": false }
          ]
        }
        """;

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-cardref-{Guid.NewGuid():N}.json");
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
    public void Migrate_LegacyFileWithNoSeededCards_BootstrapsTheCanonicalFiveAndRewritesExpenseAndStatement()
    {
        var path = CreateTempFile(LegacyFixtureJson());

        try
        {
            var summary = CreditCardReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.AlreadyCurrentShape.Should().BeFalse();
                summary.CardsBootstrappedCount.Should().Be(5);
                summary.ExpensesMigratedCount.Should().Be(1);
                summary.CardStatementsMigratedCount.Should().Be(1);
            }

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            using (new AssertionScope())
            {
                rewritten.CreditCards.Should().HaveCount(5);
                var expense = rewritten.Expenses.Should().ContainSingle().Which;
                expense.Id.Should().Be(ExpenseId);
                expense.CreditCard!.Name.Should().Be("BA Amex");
                var statement = rewritten.CardStatements.Should().ContainSingle().Which;
                statement.Id.Should().Be(CardStatementId);
                statement.CreditCard.Name.Should().Be("Chase Master 4023");
            }
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_LegacyFileWithCardsAlreadySeeded_RewritesUsingTheExistingCardInstance()
    {
        var path = CreateTempFile(LegacyFixtureJsonWithSeededCard());

        try
        {
            var summary = CreditCardReferenceMigrator.Migrate(path);

            summary.CardsBootstrappedCount.Should().Be(0);
            summary.ExpensesMigratedCount.Should().Be(1);
            summary.CardStatementsMigratedCount.Should().Be(1);

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            rewritten.CreditCards.Should().ContainSingle().Which.Id.Should().Be(SeededCardId);
            rewritten.Expenses.Should().ContainSingle().Which.CreditCard!.Id.Should().Be(SeededCardId);
            rewritten.CardStatements.Should().ContainSingle().Which.CreditCard.Id.Should().Be(SeededCardId);
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
            CreditCardReferenceMigrator.Migrate(path);
            var contentAfterFirstRun = File.ReadAllText(path);

            var secondSummary = CreditCardReferenceMigrator.Migrate(path);

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
            CreditCardReferenceMigrator.Migrate(path);

            Directory.GetFiles(directory, $"{nameWithoutExtension}.backup-migration-*").Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_ExpenseWithUnresolvableCardName_AbortsWithoutWritingOrBackingUpTheFile()
    {
        var path = CreateTempFile(LegacyFixtureJson(expenseCardName: "NotACard"));
        var originalContent = File.ReadAllText(path);

        try
        {
            var act = () => CreditCardReferenceMigrator.Migrate(path);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{ExpenseId}*NotACard*");

            using (new AssertionScope())
            {
                File.ReadAllText(path).Should().Be(originalContent);
                Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}.backup-migration-*").Should().BeEmpty();
            }
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_CardStatementWithUnresolvableCardName_AbortsWithoutWritingOrBackingUpTheFile()
    {
        var path = CreateTempFile(LegacyFixtureJson(statementCardName: "NotACard"));
        var originalContent = File.ReadAllText(path);

        try
        {
            var act = () => CreditCardReferenceMigrator.Migrate(path);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{CardStatementId}*NotACard*");

            using (new AssertionScope())
            {
                File.ReadAllText(path).Should().Be(originalContent);
                Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}.backup-migration-*").Should().BeEmpty();
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
        var currentShapeJson = """
            {
              "Expenses": [], "ReserveMovements": [], "CardStatements": [], "RecurringBills": [],
              "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
              "Banks": [], "IncomeSources": [], "ReserveBuckets": [], "CreditCards": [],
              "Incomes": [], "Transfers": [], "BalanceAdjustments": []
            }
            """;
        var path = CreateTempFile(currentShapeJson);

        try
        {
            var summary = CreditCardReferenceMigrator.Migrate(path);

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
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-cardref-missing-{Guid.NewGuid():N}.json");

        var summary = CreditCardReferenceMigrator.Migrate(missingPath);

        summary.AlreadyCurrentShape.Should().BeTrue();
    }
}
