using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class ExpenseTests
{
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
    }

    [Fact]
    public void Create_TwoExpenses_HaveDifferentIds()
    {
        var first = Expense.Create(new DateOnly(2026, 7, 1), "A", 1m, Category.Casa, PaymentSource.Chase, null);
        var second = Expense.Create(new DateOnly(2026, 7, 1), "B", 2m, Category.Casa, PaymentSource.Chase, null);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithCardTag_SetsCardTag()
    {
        var expense = Expense.Create(
            new DateOnly(2026, 7, 1),
            "Amazon",
            10m,
            Category.Extras,
            PaymentSource.Barclays,
            CreditCard.BarclaysPlatinumVisa8003);

        expense.CardTag.Should().Be(CreditCard.BarclaysPlatinumVisa8003);
    }

    [Fact]
    public void UpdateDetails_MutatesEveryFieldWithoutChangingId()
    {
        var expense = Expense.Create(new DateOnly(2026, 7, 1), "Original", 10m, Category.Casa, PaymentSource.Chase, null);
        var originalId = expense.Id;
        var newDate = new DateOnly(2026, 8, 1);

        expense.UpdateDetails(newDate, "Updated", 20m, Category.Mercado, PaymentSource.Barclays, CreditCard.ChaseMaster4023);

        expense.Id.Should().Be(originalId);
        expense.Date.Should().Be(newDate);
        expense.Description.Should().Be("Updated");
        expense.Value.Should().Be(20m);
        expense.Category.Should().Be(Category.Mercado);
        expense.PaymentSource.Should().Be(PaymentSource.Barclays);
        expense.CardTag.Should().Be(CreditCard.ChaseMaster4023);
    }
}
