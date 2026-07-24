using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class ExpenseTests
{
    private static Expense CreateImmediateExpense() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Immediate", 10m, Category.Casa, "Chase", null);

    private static Expense CreateCardCharge() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Charge", 10m, Category.Extras, null, CreditCard.ChaseMaster4023);

    private static Expense CreateSettledExpense()
    {
        var expense = CreateCardCharge();
        expense.Settle("Barclays", new DateOnly(2026, 7, 31));
        return expense;
    }

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 15);

        var expense = Expense.Create(date, "Weekly groceries", 54.32m, Category.Mercado, "Barclays", null);

        expense.Id.Should().NotBeEmpty();
        expense.Date.Should().Be(date);
        expense.Description.Should().Be("Weekly groceries");
        expense.Value.Should().Be(54.32m);
        expense.Category.Should().Be(Category.Mercado);
        expense.PaymentSource.Should().Be("Barclays");
        expense.CardTag.Should().BeNull();
        expense.SettledAt.Should().BeNull();
    }

    [Fact]
    public void Create_TwoExpenses_HaveDifferentIds()
    {
        var first = Expense.Create(new DateOnly(2026, 7, 1), "A", 1m, Category.Casa, "Chase", null);
        var second = Expense.Create(new DateOnly(2026, 7, 1), "B", 2m, Category.Casa, "Chase", null);

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
            "Barclays",
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
            expense.Date, "Updated", 20m, Category.Casa, "Chase", CreditCard.BaAmex);

        act.Should().Throw<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public void UpdateDetails_OnSettledExpense_WithUnchangedPaymentFields_KeepsSettlement()
    {
        var expense = CreateSettledExpense();

        expense.UpdateDetails(new DateOnly(2026, 7, 2), "Renamed", 25m, Category.Mercado, expense.PaymentSource, expense.CardTag);

        expense.Description.Should().Be("Renamed");
        expense.Value.Should().Be(25m);
        expense.PaymentSource.Should().Be("Barclays");
        expense.CardTag.Should().Be(CreditCard.ChaseMaster4023);
        expense.SettledAt.Should().Be(new DateOnly(2026, 7, 31));
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
    }

    [Fact]
    public void UpdateDetails_OnSettledExpense_ChangingPaymentFields_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.UpdateDetails(expense.Date, "Renamed", 25m, Category.Mercado, "Chase", expense.CardTag);

        act.Should().Throw<ArgumentException>().WithMessage("*unmark its card statement paid*");
    }

    [Fact]
    public void Settle_OnCardCharge_SetsPaymentSourceAndSettledAt()
    {
        var expense = CreateCardCharge();
        var settledAt = new DateOnly(2026, 7, 24);

        expense.Settle("Trading212", settledAt);

        expense.PaymentSource.Should().Be("Trading212");
        expense.SettledAt.Should().Be(settledAt);
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
    }

    [Fact]
    public void Settle_OnImmediatePayment_Throws()
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.Settle("Barclays", new DateOnly(2026, 7, 24));

        act.Should().Throw<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void Settle_OnAlreadySettledExpense_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.Settle("Barclays", new DateOnly(2026, 7, 24));

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

    [Theory]
    [InlineData(9.40, 0.60)]
    [InlineData(10.00, 0.00)]
    [InlineData(0.01, 0.99)]
    public void RoundUpSuggestion_ComputesDifferenceToNextWholePound(decimal value, decimal expected)
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Test", value, Category.Mercado, "Chase", null);

        expense.RoundUpSuggestion.Should().Be(expected);
    }

    [Fact]
    public void SetRoundUpAmount_OnImmediatePaymentWithinRange_Succeeds()
    {
        var expense = CreateImmediateExpense();

        expense.SetRoundUpAmount(0.60m);

        expense.RoundUpAmount.Should().Be(0.60m);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(0.99)]
    public void SetRoundUpAmount_AtRangeBoundaries_Succeeds(decimal amount)
    {
        var expense = CreateImmediateExpense();

        expense.SetRoundUpAmount(amount);

        expense.RoundUpAmount.Should().Be(amount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.00)]
    public void SetRoundUpAmount_OutsideRange_Throws(decimal amount)
    {
        var expense = CreateImmediateExpense();

        var act = () => expense.SetRoundUpAmount(amount);

        act.Should().Throw<ArgumentException>().WithMessage("*between £0.00 and £0.99*");
    }

    [Fact]
    public void SetRoundUpAmount_OnCreditCardCharge_Throws()
    {
        var expense = CreateCardCharge();

        var act = () => expense.SetRoundUpAmount(0.50m);

        act.Should().Throw<ArgumentException>().WithMessage("*not a credit-card charge*");
    }

    [Fact]
    public void SetRoundUpAmount_OnSettledExpense_Throws()
    {
        var expense = CreateSettledExpense();

        var act = () => expense.SetRoundUpAmount(0.50m);

        act.Should().Throw<ArgumentException>().WithMessage("*not a credit-card charge*");
    }

    [Fact]
    public void SetRoundUpAmount_Null_AlwaysSucceedsRegardlessOfShape()
    {
        var immediate = CreateImmediateExpense();
        var charge = CreateCardCharge();

        immediate.SetRoundUpAmount(null);
        charge.SetRoundUpAmount(null);

        immediate.RoundUpAmount.Should().BeNull();
        charge.RoundUpAmount.Should().BeNull();
    }

    [Fact]
    public void SetRoundUpAmount_Null_ClearsAPreviouslySetAmount()
    {
        var expense = CreateImmediateExpense();
        expense.SetRoundUpAmount(0.60m);

        expense.SetRoundUpAmount(null);

        expense.RoundUpAmount.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ChangingValue_LeavesRoundUpAmountUnchanged()
    {
        var expense = CreateImmediateExpense();
        expense.SetRoundUpAmount(0.60m);

        expense.UpdateDetails(expense.Date, expense.Description, 20m, expense.Category, expense.PaymentSource, expense.CardTag);

        expense.RoundUpAmount.Should().Be(0.60m);
    }
}
