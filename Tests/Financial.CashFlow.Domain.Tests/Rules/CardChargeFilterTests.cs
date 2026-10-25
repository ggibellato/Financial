using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class CardChargeFilterTests
{
    private readonly Account _amex = TestAccounts.CreditCard("BA Amex");
    private readonly Account _visa = TestAccounts.CreditCard("Platinum Visa 8003");
    private readonly Account _barclays = TestAccounts.Bank("Barclays");
    private readonly Category _groceries = Category.Create("Mercado");

    private Expense Charge(Account card, DateOnly date, decimal value) =>
        Expense.Create(date, "Charge", value, _groceries, null, card);

    [Fact]
    public void ChargesFor_UnpaidOnly_ReturnsTheCardsUnpaidChargesOfThatInvoiceMonth()
    {
        var unpaid = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var paid = Charge(_amex, new DateOnly(2026, 10, 5), 20m);
        paid.Settle(_barclays, new DateOnly(2026, 10, 20));
        var expenses = new[] { unpaid, paid };

        CardChargeFilter.ChargesFor(expenses, _amex, 2026, 10, includePaid: false).Should().Equal(unpaid);
    }

    [Fact]
    public void ChargesFor_IncludingPaid_ReturnsPaidAndUnpaidCharges()
    {
        var unpaid = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var paid = Charge(_amex, new DateOnly(2026, 10, 5), 20m);
        paid.Settle(_barclays, new DateOnly(2026, 10, 20));
        var expenses = new[] { unpaid, paid };

        CardChargeFilter.ChargesFor(expenses, _amex, 2026, 10, includePaid: true).Should().Equal(unpaid, paid);
    }

    [Fact]
    public void ChargesFor_LeavesOutAnotherCardAnotherMonthAndBankExpenses()
    {
        var match = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var otherCard = Charge(_visa, new DateOnly(2026, 10, 3), 40m);
        var otherMonth = Charge(_amex, new DateOnly(2026, 11, 3), 50m);
        var bankExpense = Expense.Create(new DateOnly(2026, 10, 3), "Shop", 60m, _groceries, _barclays, null);
        var expenses = new[] { match, otherCard, otherMonth, bankExpense };

        CardChargeFilter.ChargesFor(expenses, _amex, 2026, 10, includePaid: true).Should().Equal(match);
    }

    [Fact]
    public void ChargesFor_UsesTheInvoiceMonthNotTheChargeDate()
    {
        var charge = Charge(_amex, new DateOnly(2026, 9, 28), 30m);
        charge.SetInvoiceDate(new DateOnly(2026, 10, 1));

        CardChargeFilter.ChargesFor([charge], _amex, 2026, 10, includePaid: false).Should().Equal(charge);
        CardChargeFilter.ChargesFor([charge], _amex, 2026, 9, includePaid: false).Should().BeEmpty();
    }
}
