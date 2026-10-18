using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CategoryReferences;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.CategoryReferences;

[Trait("Category", "Integration")]
public class CategoryReferenceMigratorTests : FileMigratorContractTests
{
    protected override string LegacyFileJson() => LegacyFixtureJson();

    protected override string CurrentShapeJson() => """
        {
          "Expenses": [], "ReserveMovements": [], "CardStatements": [], "RecurringBills": [],
          "MaeLedgerEntries": [], "InvestmentSnapshots": [], "InvestmentAccounts": [],
          "Banks": [], "IncomeSources": [], "ReserveBuckets": [], "CreditCards": [], "Categories": [],
          "Incomes": [], "Transfers": [], "BalanceAdjustments": []
        }
        """;

    protected override bool MigrateReportsAlreadyCurrentShape(string path) => CategoryReferenceMigrator.Migrate(path).AlreadyCurrentShape;

    private static readonly Guid ExpenseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SeededCategoryId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static string LegacyFixtureJson(string expenseCategoryName = "Mercado") => $$"""
        {
          "Banks": [], "IncomeSources": [], "InvestmentAccounts": [], "ReserveBuckets": [], "CreditCards": [],
          "ReserveMovements": [], "RecurringBills": [], "MaeLedgerEntries": [],
          "Incomes": [], "Transfers": [], "BalanceAdjustments": [], "InvestmentSnapshots": [],
          "Expenses": [
            { "Id": "{{ExpenseId}}", "Date": "2026-07-05", "Description": "Groceries", "Value": 42.5, "PaymentSourceBankId": null, "CreditCardId": null, "Category": "{{expenseCategoryName}}" }
          ],
          "CardStatements": []
        }
        """;

    private static string LegacyFixtureJsonWithSeededCategory() => $$"""
        {
          "Banks": [], "IncomeSources": [], "InvestmentAccounts": [], "ReserveBuckets": [], "CreditCards": [],
          "ReserveMovements": [], "RecurringBills": [], "MaeLedgerEntries": [],
          "Incomes": [], "Transfers": [], "BalanceAdjustments": [], "InvestmentSnapshots": [],
          "Categories": [
            { "Id": "{{SeededCategoryId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "{{ExpenseId}}", "Date": "2026-07-05", "Description": "Groceries", "Value": 42.5, "PaymentSourceBankId": null, "CreditCardId": null, "Category": "Mercado" }
          ],
          "CardStatements": []
        }
        """;

    [Fact]
    public void Migrate_FileWithV1Banks_KeepsEachBanksRoundUpAndOpeningBalance()
    {
        var bankId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var path = CreateTempFile(LegacyFixtureJson().Replace(
            "\"Banks\": []",
            $$"""
            "Banks": [{ "Id": "{{bankId}}", "Name": "Chase", "RoundUpEnabled": true, "OpeningBalance": 1250.70, "OpeningBalanceDate": "2026-09-01" }]
            """));

        try
        {
            CategoryReferenceMigrator.Migrate(path);

            var bank = new CashFlowSerializerAdapter().Deserialize(File.ReadAllText(path)).Accounts.Should().ContainSingle().Which;
            using (new AssertionScope())
            {
                bank.Id.Should().Be(bankId);
                bank.RoundsUp.Should().BeTrue();
                bank.OpeningBalance.Should().Be(OpeningBalance.Create(1250.70m, new DateOnly(2026, 9, 1)));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Migrate_LegacyFileWithNoSeededCategories_BootstrapsTheCanonicalFourteenAndRewritesExpense()
    {
        var path = CreateTempFile(LegacyFixtureJson());

        try
        {
            var summary = CategoryReferenceMigrator.Migrate(path);

            using (new AssertionScope())
            {
                summary.AlreadyCurrentShape.Should().BeFalse();
                summary.CategoriesBootstrappedCount.Should().Be(14);
                summary.ExpensesMigratedCount.Should().Be(1);
            }

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            using (new AssertionScope())
            {
                rewritten.Categories.Should().HaveCount(14);
                var expense = rewritten.Expenses.Should().ContainSingle().Which;
                expense.Id.Should().Be(ExpenseId);
                expense.Category.Name.Should().Be("Mercado");
            }
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_LegacyFileWithCategoriesAlreadySeeded_RewritesUsingTheExistingCategoryInstance()
    {
        var path = CreateTempFile(LegacyFixtureJsonWithSeededCategory());

        try
        {
            var summary = CategoryReferenceMigrator.Migrate(path);

            summary.CategoriesBootstrappedCount.Should().Be(0);
            summary.ExpensesMigratedCount.Should().Be(1);

            var serializer = new CashFlowSerializerAdapter();
            var rewritten = serializer.Deserialize(File.ReadAllText(path));

            rewritten.Categories.Should().ContainSingle().Which.Id.Should().Be(SeededCategoryId);
            rewritten.Expenses.Should().ContainSingle().Which.Category.Id.Should().Be(SeededCategoryId);
        }
        finally
        {
            File.Delete(path);
            DeleteBackups(path);
        }
    }

    [Fact]
    public void Migrate_ExpenseWithUnresolvableCategoryName_AbortsWithoutWritingOrBackingUpTheFile()
    {
        var path = CreateTempFile(LegacyFixtureJson(expenseCategoryName: "NotACategory"));
        var originalContent = File.ReadAllText(path);

        try
        {
            var act = () => CategoryReferenceMigrator.Migrate(path);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{ExpenseId}*NotACategory*");

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
}
