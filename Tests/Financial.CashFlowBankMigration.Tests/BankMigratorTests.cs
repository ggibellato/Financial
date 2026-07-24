using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowBankMigration;
using FluentAssertions;

namespace Financial.CashFlowBankMigration.Tests;

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
    public void Migrate_ExpenseWithMatchingBankTag_CountsAsResolvedAndLeavesValueUntouched()
    {
        var data = CashFlowData.Create();
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Groceries", 20m, Category.Mercado, "Barclays", null);
        data.AddExpense(expense);

        var summary = BankMigrator.Migrate(data);

        summary.ExpensesResolvedCount.Should().Be(1);
        summary.UnresolvedExpenses.Should().BeEmpty();
        expense.PaymentSource.Should().Be("Barclays");
    }

    [Fact]
    public void Migrate_ExpenseWithCardTagAndNoBank_CountsAsNotApplicable()
    {
        var data = CashFlowData.Create();
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Charge", 20m, Category.Extras, null, CreditCard.ChaseMaster4023);
        data.AddExpense(expense);

        var summary = BankMigrator.Migrate(data);

        summary.ExpensesNotApplicableCount.Should().Be(1);
        summary.ExpensesResolvedCount.Should().Be(0);
        summary.UnresolvedExpenses.Should().BeEmpty();
    }

    [Fact]
    public void Migrate_ExpenseWithUnresolvableBankTag_IsFlaggedForManualReviewAndLeftUntouched()
    {
        var data = CashFlowData.Create();
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Mystery", 20m, Category.Extras, "NotABank", null);
        data.AddExpense(expense);

        var summary = BankMigrator.Migrate(data);

        summary.UnresolvedExpenses.Should().ContainSingle().Which.Id.Should().Be(expense.Id);
        summary.ExpensesResolvedCount.Should().Be(0);
        expense.PaymentSource.Should().Be("NotABank");
    }

    [Fact]
    public void Migrate_SecondRunOverFirstRunsOutput_ChangesNothing()
    {
        var data = CashFlowData.Create();
        var resolved = Expense.Create(new DateOnly(2026, 7, 1), "Groceries", 20m, Category.Mercado, "Chase", null);
        var unresolved = Expense.Create(new DateOnly(2026, 7, 2), "Mystery", 5m, Category.Extras, "NotABank", null);
        data.AddExpense(resolved);
        data.AddExpense(unresolved);

        BankMigrator.Migrate(data);
        var secondSummary = BankMigrator.Migrate(data);

        secondSummary.BanksSeededCount.Should().Be(0);
        secondSummary.ExpensesResolvedCount.Should().Be(1);
        secondSummary.UnresolvedExpenses.Should().ContainSingle().Which.Id.Should().Be(unresolved.Id);
        resolved.PaymentSource.Should().Be("Chase");
        unresolved.PaymentSource.Should().Be("NotABank");
    }
}
