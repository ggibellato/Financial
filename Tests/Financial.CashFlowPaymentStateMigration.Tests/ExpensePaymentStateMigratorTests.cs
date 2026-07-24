using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowPaymentStateMigration;
using FluentAssertions;

namespace Financial.CashFlowPaymentStateMigration.Tests;

public class ExpensePaymentStateMigratorTests
{
    private static CashFlowData DataWithStatement(CreditCard card, int year, int month, bool isPaid)
    {
        var data = CashFlowData.Create();
        var statement = CardStatement.Create(card, year, month);
        if (isPaid)
        {
            statement.MarkPaid();
        }

        data.AddCardStatement(statement);
        return data;
    }

    private static Expense LegacyCardExpense(CashFlowData data, PaymentSource bank, CreditCard card, DateOnly date)
    {
        // Legacy records carry both a bank and a card tag; that shape can no longer be produced
        // through Expense.Create, so build it the way deserialization does: charge + settle.
        var expense = Expense.Create(date, "Legacy", 30m, Category.Mercado, null, card);
        expense.Settle(bank, date);
        typeof(Expense).GetProperty(nameof(Expense.SettledAt))!
            .SetMethod!.Invoke(expense, new object?[] { null });
        return AddExpense(data, expense);
    }

    private static Expense AddExpense(CashFlowData data, Expense expense)
    {
        data.AddExpense(expense);
        return expense;
    }

    [Fact]
    public void Migrate_ExpenseWithoutCardTag_IsLeftUntouched()
    {
        var data = CashFlowData.Create();
        var expense = AddExpense(data, Expense.Create(new DateOnly(2026, 5, 10), "Groceries", 20m, Category.Mercado, PaymentSource.Barclays, null));

        var summary = ExpensePaymentStateMigrator.Migrate(data);

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.ImmediatePayment);
        expense.PaymentSource.Should().Be(PaymentSource.Barclays);
        expense.SettledAt.Should().BeNull();
        summary.ImmediatePaymentCount.Should().Be(1);
    }

    [Fact]
    public void Migrate_LegacyCardExpenseWithPaidStatement_BecomesSettledWithMonthEndDate()
    {
        var data = DataWithStatement(CreditCard.ChaseMaster4023, 2026, 2, isPaid: true);
        var expense = LegacyCardExpense(data, PaymentSource.Chase, CreditCard.ChaseMaster4023, new DateOnly(2026, 2, 10));

        var summary = ExpensePaymentStateMigrator.Migrate(data);

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
        expense.PaymentSource.Should().Be(PaymentSource.Chase);
        expense.SettledAt.Should().Be(new DateOnly(2026, 2, 28));
        expense.CardTag.Should().Be(CreditCard.ChaseMaster4023);
        summary.NewlySettledCount.Should().Be(1);
    }

    [Fact]
    public void Migrate_SettledExpenseWithRealSettlementDate_IsLeftUntouched()
    {
        var data = DataWithStatement(CreditCard.BaAmex, 2026, 7, isPaid: true);
        var expense = AddExpense(data, Expense.Create(new DateOnly(2026, 7, 5), "Settled via F02", 10m, Category.Extras, null, CreditCard.BaAmex));
        expense.Settle(PaymentSource.Trading212, new DateOnly(2026, 7, 24));

        var summary = ExpensePaymentStateMigrator.Migrate(data);

        expense.SettledAt.Should().Be(new DateOnly(2026, 7, 24));
        expense.PaymentSource.Should().Be(PaymentSource.Trading212);
        summary.AlreadySettledCount.Should().Be(1);
        summary.NewlySettledCount.Should().Be(0);
    }

    [Fact]
    public void Migrate_LegacyCardExpenseWithUnpaidStatement_BecomesChargeWithClearedBank()
    {
        var data = DataWithStatement(CreditCard.BarclaysPlatinumVisa8003, 2026, 3, isPaid: false);
        var expense = LegacyCardExpense(data, PaymentSource.Barclays, CreditCard.BarclaysPlatinumVisa8003, new DateOnly(2026, 3, 15));

        var summary = ExpensePaymentStateMigrator.Migrate(data);

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        expense.PaymentSource.Should().BeNull();
        expense.SettledAt.Should().BeNull();
        expense.CardTag.Should().Be(CreditCard.BarclaysPlatinumVisa8003);
        summary.NewlyChargeCount.Should().Be(1);
    }

    [Fact]
    public void Migrate_LegacyCardExpenseWithNoMatchingStatement_BecomesChargeAndIsFlaggedForReview()
    {
        var data = CashFlowData.Create();
        var expense = LegacyCardExpense(data, PaymentSource.Barclays, CreditCard.PaypalCredit, new DateOnly(2026, 4, 15));

        var summary = ExpensePaymentStateMigrator.Migrate(data);

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        expense.PaymentSource.Should().BeNull();
        summary.NewlyChargeCount.Should().Be(1);
        summary.MissingStatementExpenses.Should().ContainSingle().Which.Id.Should().Be(expense.Id);
    }

    [Fact]
    public void Migrate_ChargeWithNullBankAndPaidStatement_IsLeftAsChargeSinceNoBankIsKnown()
    {
        var data = DataWithStatement(CreditCard.BaAmex, 2026, 6, isPaid: true);
        var expense = AddExpense(data, Expense.Create(new DateOnly(2026, 6, 5), "Charge", 10m, Category.Extras, null, CreditCard.BaAmex));

        var summary = ExpensePaymentStateMigrator.Migrate(data);

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        expense.PaymentSource.Should().BeNull();
        summary.AlreadyChargeCount.Should().Be(1);
    }

    [Fact]
    public void Migrate_SecondRunOverFirstRunsOutput_ChangesNothing()
    {
        var data = DataWithStatement(CreditCard.ChaseMaster4023, 2026, 2, isPaid: true);
        var paidExpense = LegacyCardExpense(data, PaymentSource.Chase, CreditCard.ChaseMaster4023, new DateOnly(2026, 2, 10));
        var unpaidData = CardStatement.Create(CreditCard.BaAmex, 2026, 2);
        data.AddCardStatement(unpaidData);
        var unpaidExpense = LegacyCardExpense(data, PaymentSource.Barclays, CreditCard.BaAmex, new DateOnly(2026, 2, 12));
        var immediate = AddExpense(data, Expense.Create(new DateOnly(2026, 2, 14), "Immediate", 5m, Category.Casa, PaymentSource.Trading212, null));

        ExpensePaymentStateMigrator.Migrate(data);
        var afterFirst = data.Expenses
            .Select(e => (e.Id, e.PaymentSource, e.CardTag, e.SettledAt, e.PaymentStatus))
            .ToList();

        var secondSummary = ExpensePaymentStateMigrator.Migrate(data);

        data.Expenses
            .Select(e => (e.Id, e.PaymentSource, e.CardTag, e.SettledAt, e.PaymentStatus))
            .Should().Equal(afterFirst);
        secondSummary.NewlySettledCount.Should().Be(0);
        secondSummary.NewlyChargeCount.Should().Be(0);
        secondSummary.AlreadySettledCount.Should().Be(1);
        secondSummary.AlreadyChargeCount.Should().Be(1);
        secondSummary.ImmediatePaymentCount.Should().Be(1);
        immediate.PaymentStatus.Should().Be(ExpensePaymentStatus.ImmediatePayment);
        paidExpense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
        unpaidExpense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
    }
}
