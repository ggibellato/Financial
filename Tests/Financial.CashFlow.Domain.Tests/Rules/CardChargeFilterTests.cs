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

    private static CardStatement Statement(Account card, int year, int month, bool isPaid)
    {
        var statement = CardStatement.Create(card, year, month);
        if (isPaid)
        {
            statement.MarkLegacyPaid();
        }

        return statement;
    }

    [Fact]
    public void ChargesFor_ReturnsPaidAndUnpaidChargesOfThatInvoiceMonth()
    {
        var unpaid = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var paid = Charge(_amex, new DateOnly(2026, 10, 5), 20m);
        paid.Settle(_barclays, new DateOnly(2026, 10, 20));

        CardChargeFilter.ChargesFor([unpaid, paid], _amex, 2026, 10).Should().Equal(unpaid, paid);
    }

    [Fact]
    public void ChargesFor_LeavesOutAnotherCardAnotherMonthAndBankExpenses()
    {
        var match = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var otherCard = Charge(_visa, new DateOnly(2026, 10, 3), 40m);
        var otherMonth = Charge(_amex, new DateOnly(2026, 11, 3), 50m);
        var bankExpense = Expense.Create(new DateOnly(2026, 10, 3), "Shop", 60m, _groceries, _barclays, null);

        CardChargeFilter.ChargesFor([match, otherCard, otherMonth, bankExpense], _amex, 2026, 10).Should().Equal(match);
    }

    [Fact]
    public void UnpaidChargesFor_UsesTheInvoiceMonthNotTheChargeDate()
    {
        var charge = Charge(_amex, new DateOnly(2026, 9, 28), 30m);
        charge.SetInvoiceDate(new DateOnly(2026, 10, 1));

        CardChargeFilter.UnpaidChargesFor([charge], [], _amex, 2026, 10).Should().Equal(charge);
        CardChargeFilter.UnpaidChargesFor([charge], [], _amex, 2026, 9).Should().BeEmpty();
    }

    [Fact]
    public void UnpaidChargesFor_LeavesOutChargesSettledToABank()
    {
        var unpaid = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var settled = Charge(_amex, new DateOnly(2026, 10, 5), 20m);
        settled.Settle(_barclays, new DateOnly(2026, 10, 20));

        CardChargeFilter.UnpaidChargesFor([unpaid, settled], [Statement(_amex, 2026, 10, isPaid: false)], _amex, 2026, 10)
            .Should().Equal(unpaid);
    }

    [Fact]
    public void UnpaidChargesFor_PaidStatement_ReturnsNothing()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.UnpaidChargesFor([charge], [Statement(_amex, 2026, 10, isPaid: true)], _amex, 2026, 10).Should().BeEmpty();
    }

    [Fact]
    public void UnpaidChargesFor_AnotherCardsPaidStatement_DoesNotHideTheCharges()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.UnpaidChargesFor([charge], [Statement(_visa, 2026, 10, isPaid: true)], _amex, 2026, 10).Should().Equal(charge);
    }

    [Fact]
    public void OutstandingTotal_SumsTheCardsUnpaidChargesOfThatInvoiceMonth()
    {
        var unpaid = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var secondUnpaid = Charge(_amex, new DateOnly(2026, 10, 9), 12.50m);
        var paid = Charge(_amex, new DateOnly(2026, 10, 5), 20m);
        paid.Settle(_barclays, new DateOnly(2026, 10, 20));
        var otherCard = Charge(_visa, new DateOnly(2026, 10, 3), 40m);

        CardChargeFilter.OutstandingTotal([unpaid, secondUnpaid, paid, otherCard], [], _amex, 2026, 10).Should().Be(42.50m);
    }

    [Fact]
    public void TotalOwed_CountsEveryUnpaidChargeWhateverItsDate()
    {
        var october = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var later = Charge(_amex, new DateOnly(2027, 3, 9), 12.50m);
        var otherCard = Charge(_visa, new DateOnly(2026, 10, 3), 40m);

        CardChargeFilter.TotalOwed(_amex, [october, later, otherCard]).Should().Be(42.50m);
    }

    [Fact]
    public void TotalOwed_NetsStatementPaymentsAndLeavesOutChargesSettledToABank()
    {
        var paidByTransfer = Charge(_amex, new DateOnly(2026, 9, 3), 100m);
        var payment = Transfer.CreateStatementPayment(new DateOnly(2026, 10, 2), _barclays, _amex, 100m, new DateOnly(2026, 10, 30));
        var settled = Charge(_amex, new DateOnly(2026, 10, 5), 20m);
        settled.Settle(_barclays, new DateOnly(2026, 10, 20));
        var unpaid = Charge(_amex, new DateOnly(2026, 10, 9), 25m);

        CardChargeFilter.TotalOwed(_amex, [paidByTransfer, payment, settled, unpaid]).Should().Be(25m);
    }

    [Fact]
    public void TotalOwed_CardInCredit_IsNegative()
    {
        var payment = Transfer.CreateStatementPayment(new DateOnly(2026, 10, 2), _barclays, _amex, 50m, new DateOnly(2026, 10, 30));

        CardChargeFilter.TotalOwed(_amex, [payment]).Should().Be(-50m);
    }

    [Fact]
    public void NextStatementToPay_IsTheOldestInvoiceMonthWithAnUnpaidChargeAndItsOutstandingTotal()
    {
        var november = Charge(_amex, new DateOnly(2026, 11, 3), 80m);
        var october = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var secondOctober = Charge(_amex, new DateOnly(2026, 10, 9), 12.50m);

        CardChargeFilter.NextStatementToPay([november, october, secondOctober], [], _amex)
            .Should().Be(new CardStatementMonth(2026, 10, 42.50m));
    }

    [Fact]
    public void NextStatementToPay_SkipsPaidMonthsSettledChargesAndEmptyUnpaidStatements()
    {
        var paidMonth = Charge(_amex, new DateOnly(2026, 8, 3), 70m);
        var settled = Charge(_amex, new DateOnly(2026, 9, 5), 20m);
        settled.Settle(_barclays, new DateOnly(2026, 9, 20));
        var due = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        var statements = new[]
        {
            Statement(_amex, 2026, 7, isPaid: false),
            Statement(_amex, 2026, 8, isPaid: true),
            Statement(_amex, 2026, 9, isPaid: false)
        };

        CardChargeFilter.NextStatementToPay([paidMonth, settled, due], statements, _amex)
            .Should().Be(new CardStatementMonth(2026, 10, 30m));
    }

    [Fact]
    public void NextStatementToPay_LeavesOutAnotherCardsChargesAndBankExpenses()
    {
        var otherCard = Charge(_visa, new DateOnly(2026, 9, 3), 40m);
        var bankExpense = Expense.Create(new DateOnly(2026, 9, 3), "Shop", 60m, _groceries, _barclays, null);
        var due = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.NextStatementToPay([otherCard, bankExpense, due], [], _amex)
            .Should().Be(new CardStatementMonth(2026, 10, 30m));
    }

    [Fact]
    public void NextStatementToPay_NoUnpaidCharge_IsNull()
    {
        var paid = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.NextStatementToPay([paid], [Statement(_amex, 2026, 10, isPaid: true)], _amex).Should().BeNull();
    }

    [Fact]
    public void OutstandingTotal_PaidStatement_IsZero()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.OutstandingTotal([charge], [Statement(_amex, 2026, 10, isPaid: true)], _amex, 2026, 10).Should().Be(0m);
    }

    [Fact]
    public void IsOnPaidStatement_ChargeOnAPaidStatement_IsTrue()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsOnPaidStatement(charge, [Statement(_amex, 2026, 10, isPaid: true)]).Should().BeTrue();
    }

    [Fact]
    public void IsOnPaidStatement_ChargeOnAnUnpaidStatement_IsFalse()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsOnPaidStatement(charge, [Statement(_amex, 2026, 10, isPaid: false), Statement(_amex, 2026, 9, isPaid: true)]).Should().BeFalse();
    }

    [Fact]
    public void IsOnPaidStatement_ChargeWithNoStatement_IsFalse()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsOnPaidStatement(charge, []).Should().BeFalse();
    }

    [Fact]
    public void IsOnPaidStatement_BankExpense_IsFalse()
    {
        var expense = Expense.Create(new DateOnly(2026, 10, 3), "Shop", 60m, _groceries, _barclays, null);

        CardChargeFilter.IsOnPaidStatement(expense, [Statement(_amex, 2026, 10, isPaid: true)]).Should().BeFalse();
    }

    [Fact]
    public void IsPaidThroughStatement_UnsettledChargeOnAPaidStatement_IsTrue()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsPaidThroughStatement(charge, [Statement(_amex, 2026, 10, isPaid: true)]).Should().BeTrue();
    }

    [Fact]
    public void IsPaidThroughStatement_ChargeSettledToABank_IsFalse()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        charge.Settle(_barclays, new DateOnly(2026, 10, 20));

        CardChargeFilter.IsPaidThroughStatement(charge, [Statement(_amex, 2026, 10, isPaid: true)]).Should().BeFalse();
    }

    [Fact]
    public void IsPaidThroughStatement_ChargeOnAnUnpaidStatement_IsFalse()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsPaidThroughStatement(charge, [Statement(_amex, 2026, 10, isPaid: false)]).Should().BeFalse();
    }

    [Fact]
    public void IsPaid_ChargeSettledToABank_IsTrue()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        charge.Settle(_barclays, new DateOnly(2026, 10, 20));

        CardChargeFilter.IsPaid(charge, []).Should().BeTrue();
    }

    [Fact]
    public void IsPaid_ChargeOnAPaidStatement_IsTrue()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsPaid(charge, [Statement(_amex, 2026, 10, isPaid: true)]).Should().BeTrue();
    }

    [Fact]
    public void IsPaid_ChargeOnAnUnpaidStatement_IsFalse()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsPaid(charge, [Statement(_amex, 2026, 10, isPaid: false), Statement(_visa, 2026, 10, isPaid: true)]).Should().BeFalse();
    }

    [Fact]
    public void IsPaid_ChargeWithNoStatement_IsFalse()
    {
        var charge = Charge(_amex, new DateOnly(2026, 10, 3), 30m);

        CardChargeFilter.IsPaid(charge, []).Should().BeFalse();
    }

    [Fact]
    public void SettledChargesFor_ReturnsOnlyChargesSettledToABankInThatInvoiceMonth()
    {
        var settled = Charge(_amex, new DateOnly(2026, 10, 3), 30m);
        settled.Settle(_barclays, new DateOnly(2026, 11, 2));
        var unpaid = Charge(_amex, new DateOnly(2026, 10, 5), 20m);
        var otherMonth = Charge(_amex, new DateOnly(2026, 9, 5), 10m);
        otherMonth.Settle(_barclays, new DateOnly(2026, 10, 2));

        CardChargeFilter.SettledChargesFor([settled, unpaid, otherMonth], _amex, 2026, 10).Should().Equal(settled);
    }
}
