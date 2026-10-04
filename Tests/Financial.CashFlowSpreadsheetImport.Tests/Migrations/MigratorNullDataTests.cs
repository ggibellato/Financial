using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.BankOpeningBalance;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.IncomeSources;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Incomes;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations;

public class MigratorNullDataTests
{
    public static TheoryData<string, Action> MigratorsOverData() => new()
    {
        { nameof(BankOpeningBalanceMigrator), () => BankOpeningBalanceMigrator.Migrate(null!, new DateOnly(2026, 7, 1)) },
        { nameof(CategoryMigrator), () => CategoryMigrator.Migrate(null!) },
        { nameof(CreditCardMigrator), () => CreditCardMigrator.Migrate(null!) },
        { nameof(IncomeMigrator), () => IncomeMigrator.Migrate(null!) },
        { nameof(IncomeSourceMigrator), () => IncomeSourceMigrator.Migrate(null!) },
        { nameof(ReserveBucketMigrator), () => ReserveBucketMigrator.Migrate(null!) },
    };

    [Theory]
    [MemberData(nameof(MigratorsOverData))]
    public void Migrate_WithNullData_Throws(string migrator, Action migrate) =>
        migrate.Should().Throw<ArgumentNullException>();
}
