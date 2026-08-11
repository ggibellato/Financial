using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Banks;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.Banks;

public class BankMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllThreeBanksWithCorrectRoundUpFlags()
    {
        var data = CashFlowData.Create();

        var summary = BankMigrator.Migrate(data);

        summary.BanksSeededCount.Should().Be(3);
        summary.BanksAlreadyPresentCount.Should().Be(0);
        data.Banks.Should().ContainSingle(b => b.Name == "Barclays" && !b.RoundUpEnabled);
        data.Banks.Should().ContainSingle(b => b.Name == "Trading212" && b.RoundUpEnabled);
        data.Banks.Should().ContainSingle(b => b.Name == "Chase" && b.RoundUpEnabled);
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRun()
    {
        var data = CashFlowData.Create();
        BankMigrator.Migrate(data);

        var secondSummary = BankMigrator.Migrate(data);

        secondSummary.BanksSeededCount.Should().Be(0);
        secondSummary.BanksAlreadyPresentCount.Should().Be(3);
        data.Banks.Should().HaveCount(3);
    }

    [Fact]
    public void Migrate_WithSomeBanksAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));

        var summary = BankMigrator.Migrate(data);

        summary.BanksSeededCount.Should().Be(2);
        summary.BanksAlreadyPresentCount.Should().Be(1);
        data.Banks.Should().HaveCount(3);
    }

    [Fact]
    public void Migrate_ExpenseWithBankTag_CountsAsResolvedAndLeavesValueUntouched()
    {
        var data = CashFlowData.Create();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Groceries", 20m, Category.Mercado, bank, null);
        data.AddExpense(expense);

        var summary = BankMigrator.Migrate(data);

        summary.ExpensesResolvedCount.Should().Be(1);
        summary.UnresolvedExpenses.Should().BeEmpty();
        expense.PaymentSourceBank.Should().Be(bank);
    }

    [Fact]
    public void Migrate_ExpenseWithCardTagAndNoBank_CountsAsNotApplicable()
    {
        var data = CashFlowData.Create();
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Charge", 20m, Category.Extras, null, CashFlow.Domain.Enums.CreditCard.ChaseMaster4023);
        data.AddExpense(expense);

        var summary = BankMigrator.Migrate(data);

        summary.ExpensesNotApplicableCount.Should().Be(1);
        summary.ExpensesResolvedCount.Should().Be(0);
        summary.UnresolvedExpenses.Should().BeEmpty();
    }

    [Fact]
    public void Migrate_SecondRunOverFirstRunsOutput_ChangesNothing()
    {
        var data = CashFlowData.Create();
        var bank = Bank.Create("Chase", roundUpEnabled: true);
        var resolved = Expense.Create(new DateOnly(2026, 7, 1), "Groceries", 20m, Category.Mercado, bank, null);
        var charge = Expense.Create(new DateOnly(2026, 7, 2), "Charge", 5m, Category.Extras, null, CashFlow.Domain.Enums.CreditCard.ChaseMaster4023);
        data.AddExpense(resolved);
        data.AddExpense(charge);

        BankMigrator.Migrate(data);
        var secondSummary = BankMigrator.Migrate(data);

        secondSummary.BanksSeededCount.Should().Be(0);
        secondSummary.ExpensesResolvedCount.Should().Be(1);
        secondSummary.ExpensesNotApplicableCount.Should().Be(1);
        secondSummary.UnresolvedExpenses.Should().BeEmpty();
        resolved.PaymentSourceBank.Should().Be(bank);
    }
}
