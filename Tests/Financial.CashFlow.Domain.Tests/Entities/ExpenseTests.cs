using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class ExpenseTests
{
    private static Expense CreateImmediateExpense() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Immediate", 10m, Category.Casa, PaymentSource.Chase, null);

    private static Expense CreateCardCharge() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Charge", 10m, Category.Extras, null, CreditCard.ChaseMaster4023);

    private static Expense CreateSettledExpense()
    {
        var expense = CreateCardCharge();
        expense.Settle(PaymentSource.Barclays, new DateOnly(2026, 7, 31));
        return expense;
    }

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 15);

        var expense = Expense.Create(date, "Weekly groceries", 54.32m, Category.Mercado, PaymentSource.Barclays, null);

        expense.Id.Should().NotBeEmpty();
        expense.Date.Should().Be(date);
        expense.Description.Should().Be("Weekly groceries");
        expense.Value.Should().Be(54.32m);
        expense.Category.Should().Be(Category.Mercado);
        expense.PaymentSource.Should().Be(PaymentSource.Barclays);
        expense.CardTag.Should().BeNull();
        expense.SettledAt.Should().BeNull();
    }

    [Fact]
    public void Create_TwoExpenses_HaveDifferentIds()
    {
        var first = Expense.Create(new DateOnly(2026, 7, 1), "A", 1m, Category.Casa, PaymentSource.Chase, null);
        var second = Expense.Create(new DateOnly(2026, 7, 1), "B", 2m, Category.Casa, PaymentSource.Chase, null);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithPaymentSourceOnly_ComputesImmediatePayment()
    {
        var expense = CreateImmediateExpense();

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.ImmediatePayment);
    }

    [Fact]
    public void Create_WithCardTagOnly_ComputesCreditCardCharge()
    {
        var expense = CreateCardCharge();

        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        expense.PaymentSource.Should().BeNull();
        expense.SettledAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithNeitherPaymentSourceNorCardTag_Throws()
    {
        var act = () => Expense.Create(new DateOnly(2026, 7, 1), "Invalid", 10m, Category.Casa, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("*payment source or a card tag*");
    }

    [Fact]
    public void Create_WithBothPaymentSourceAndCardTag_Throws()
    {
        var act = () => Expense.Create(
            new DateOnly(2026, 7, 1),
            "Invalid",
            10m,
            Category.Extras,
            PaymentSource.Barclays,
            CreditCard.BarclaysPlatinumVisa8003);

        act.Should().Throw<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public void UpdateDetails_MutatesEveryFieldWithoutChangingId()
    {
        var expense = CreateImmediateExpense();
        var originalId = expense.Id;
        var newDate = new DateOnly(2026, 8, 1);

        expense.UpdateDetails(newDate, "Updated", 20m, Category.Mercado, null, CreditCard.ChaseMaster4023);

        expense.Id.Should().Be(originalId);
        expense.Date.Should().Be(newDate);
        expense.Description.Should().Be("Updated");
        expense.Value.Should().Be(20m);
        expense.Category.Should().Be(Category.Mercado);
        expense.PaymentSource.Should().BeNull();
        expense.CardTag.Should().Be(CreditCard.ChaseMaster4023);
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
    }

    [Fact]
    public void UpdateDetails_WithNeitherPaymentSourceNorCardTag_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.UpdateDetails(expense.Date, "Updated", 20m, Category.Casa, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("*payment source or a card tag*");
    }

    [Fact]
    public void UpdateDetails_WithBothPaymentSourceAndCardTag_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.UpdateDetails(
            expense.Date, "Updated", 20m, Category.Casa, PaymentSource.Chase, CreditCard.BaAmex);

        act.Should().Throw<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public void UpdateDetails_OnSettledExpense_WithUnchangedPaymentFields_KeepsSettlement()
    {
        var expense = CreateSettledExpense();

        expense.UpdateDetails(new DateOnly(2026, 7, 2), "Renamed", 25m, Category.Mercado, expense.PaymentSource, expense.CardTag);

        expense.Description.Should().Be("Renamed");
        expense.Value.Should().Be(25m);
        expense.PaymentSource.Should().Be(PaymentSource.Barclays);
        expense.CardTag.Should().Be(CreditCard.ChaseMaster4023);
        expense.SettledAt.Should().Be(new DateOnly(2026, 7, 31));
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
    }

    [Fact]
    public void UpdateDetails_OnSettledExpense_ChangingPaymentFields_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.UpdateDetails(expense.Date, "Renamed", 25m, Category.Mercado, PaymentSource.Chase, expense.CardTag);

        act.Should().Throw<ArgumentException>().WithMessage("*unmark its card statement paid*");
    }

    [Fact]
    public void Settle_OnCardCharge_SetsPaymentSourceAndSettledAt()
    {
        var expense = CreateCardCharge();
        var settledAt = new DateOnly(2026, 7, 24);

        expense.Settle(PaymentSource.Trading212, settledAt);

        expense.PaymentSource.Should().Be(PaymentSource.Trading212);
        expense.SettledAt.Should().Be(settledAt);
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
    }

    [Fact]
    public void Settle_OnImmediatePayment_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.Settle(PaymentSource.Barclays, new DateOnly(2026, 7, 24));

        act.Should().Throw<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void Settle_OnAlreadySettledExpense_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.Settle(PaymentSource.Barclays, new DateOnly(2026, 7, 24));

        act.Should().Throw<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void Unsettle_OnSettledExpense_ClearsPaymentSourceAndSettledAt()
    {
        var expense = CreateSettledExpense();

        expense.Unsettle();

        expense.PaymentSource.Should().BeNull();
        expense.SettledAt.Should().BeNull();
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
    }

    [Fact]
    public void Unsettle_OnUnsettledExpense_Throws()
    {
        var expense = CreateCardCharge();

        var act = () => expense.Unsettle();

        act.Should().Throw<ArgumentException>().WithMessage("*settled credit card expense*");
    }
}
