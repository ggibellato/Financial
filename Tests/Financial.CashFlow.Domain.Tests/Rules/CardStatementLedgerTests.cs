using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class CardStatementLedgerTests
{
    private static readonly DateOnly Today = new(2026, 12, 31);

    private readonly Account _amex = TestAccounts.CreditCard("BA Amex");
    private readonly Account _visa = TestAccounts.CreditCard("Platinum Visa 8003");
    private readonly Account _barclays = TestAccounts.Bank("Barclays");
    private readonly Category _groceries = Category.Create("Mercado");
    private readonly List<Expense> _expenses = [];
    private readonly List<CardStatement> _statements = [];
    private readonly List<Transfer> _transfers = [];

    private Expense Charge(int month, decimal value, Account? card = null)
    {
        var charge = Expense.Create(new DateOnly(2026, month, 5), "Charge", value, _groceries, null, card ?? _amex);
        _expenses.Add(charge);
        return charge;
    }

    private CardStatement StoredStatement(int month)
    {
        var statement = CardStatement.Create(_amex, 2026, month);
        _statements.Add(statement);
        return statement;
    }

    private CardStatement PaidByTransfer(int month, decimal amount)
    {
        var payment = Transfer.CreateStatementPayment(new DateOnly(2026, month, 20), _barclays, _amex, amount, Today);
        _transfers.Add(payment);
        var statement = StoredStatement(month);
        statement.MarkPaidBy(payment.Id);
        return statement;
    }

    private IReadOnlyList<CardStatementLedgerMonth> Walk(int throughMonth) =>
        CardStatementLedger.For(_amex, _expenses, _statements, _transfers, 2026, throughMonth);

    [Fact]
    public void For_CardWithoutCharges_IsEmpty()
    {
        Charge(10, 30m, _visa);
        StoredStatement(10);

        Walk(11).Should().BeEmpty();
    }

    [Fact]
    public void For_StartsAtTheFirstChargeAndReachesTheThroughMonth_EmptyMonthsAreNothingDue()
    {
        Charge(8, 30m);
        Charge(10, 12m);

        var rows = Walk(11);

        using (new AssertionScope())
        {
            rows.Select(row => row.Month).Should().Equal(8, 9, 10, 11);
            rows[1].Should().BeEquivalentTo(new { Total = 0m, CreditIn = 0m, AmountToPay = 0m, CreditCarried = 0m, Status = CardStatementLedgerStatus.NothingDue });
            rows[1].Statement.Should().BeNull();
            rows[3].Status.Should().Be(CardStatementLedgerStatus.NothingDue);
        }
    }

    [Fact]
    public void For_ChargesLaterThanTheThroughMonth_ExtendTheWalkToTheLastCharge()
    {
        Charge(9, 30m);
        Charge(12, 12m);

        Walk(10).Select(row => row.Month).Should().Equal(9, 10, 11, 12);
    }

    [Fact]
    public void For_CrossesAYearBoundary()
    {
        _expenses.Add(Expense.Create(new DateOnly(2025, 12, 5), "Charge", 10m, _groceries, null, _amex));

        Walk(1).Select(row => (row.Year, row.Month)).Should().Equal((2025, 12), (2026, 1));
    }

    [Fact]
    public void For_UnpaidMonth_PaysItsChargesAndCarriesNothing()
    {
        Charge(10, 30m);
        Charge(10, 12.5m);
        var statement = StoredStatement(10);

        var row = Walk(10).Should().ContainSingle().Subject;

        row.Should().BeEquivalentTo(new
        {
            Year = 2026,
            Month = 10,
            Statement = statement,
            Total = 42.5m,
            CreditIn = 0m,
            AmountToPay = 42.5m,
            CreditCarried = 0m,
            PaidAmount = (decimal?)null,
            Status = CardStatementLedgerStatus.Unpaid,
            IsUnpaid = true
        });
    }

    [Fact]
    public void For_MonthAtExactlyZero_IsNothingDueWithNoCredit()
    {
        Charge(10, 40m);
        Charge(10, -40m);

        var row = Walk(10).Should().ContainSingle().Subject;

        row.Should().BeEquivalentTo(new { Total = 0m, AmountToPay = 0m, CreditCarried = 0m, Status = CardStatementLedgerStatus.NothingDue });
    }

    [Fact]
    public void For_RefundAboveItsCharges_CarriesCreditThatLowersTheNextMonth()
    {
        Charge(10, 25m);
        Charge(10, -60m);
        Charge(11, 50m);

        var rows = Walk(11);

        using (new AssertionScope())
        {
            rows[0].Should().BeEquivalentTo(new { Total = -35m, CreditIn = 0m, AmountToPay = 0m, CreditCarried = 35m, Status = CardStatementLedgerStatus.NothingDue });
            rows[1].Should().BeEquivalentTo(new { Total = 50m, CreditIn = 35m, AmountToPay = 15m, CreditCarried = 0m, Status = CardStatementLedgerStatus.Unpaid });
        }
    }

    [Fact]
    public void For_CreditLargerThanTheNextMonth_KeepsCarryingTheRemainder()
    {
        Charge(9, -100m);
        Charge(10, 30m);

        var rows = Walk(12);

        using (new AssertionScope())
        {
            rows.Select(row => row.CreditCarried).Should().Equal(100m, 70m, 70m, 70m);
            rows.Select(row => row.CreditIn).Should().Equal(0m, 100m, 70m, 70m);
            rows.Should().OnlyContain(row => row.Status == CardStatementLedgerStatus.NothingDue && row.AmountToPay == 0m);
        }
    }

    [Fact]
    public void For_PaidByTransfer_PaysNothingAndCarriesWhatThePaymentAndCreditLeaveOver()
    {
        Charge(9, -10m);
        Charge(10, 30m);
        Charge(10, 15m);
        PaidByTransfer(10, 35m);
        Charge(11, -5m);
        PaidByTransfer(11, 20m);

        var rows = Walk(12);

        using (new AssertionScope())
        {
            rows[1].Should().BeEquivalentTo(new { Total = 45m, CreditIn = 10m, AmountToPay = 0m, CreditCarried = 0m, PaidAmount = 35m, Status = CardStatementLedgerStatus.Paid });
            rows[2].Should().BeEquivalentTo(new { Total = -5m, CreditIn = 0m, AmountToPay = 0m, CreditCarried = 25m, PaidAmount = 20m, Status = CardStatementLedgerStatus.Paid });
            rows[3].Should().BeEquivalentTo(new { CreditIn = 25m, CreditCarried = 25m, Status = CardStatementLedgerStatus.NothingDue });
        }
    }

    [Fact]
    public void For_PaidByTransferBelowItsChargesAndCredit_NeverCarriesAShortfall()
    {
        Charge(10, 30m);
        PaidByTransfer(10, 10m);

        Walk(10).Single().CreditCarried.Should().Be(0m);
    }

    [Fact]
    public void For_PaymentTransferMissing_CountsAsAPaymentOfZero()
    {
        Charge(9, -5m);
        Charge(10, 2m);
        var statement = StoredStatement(10);
        statement.MarkPaidBy(Guid.NewGuid());

        var row = Walk(10)[1];

        row.Should().BeEquivalentTo(new { PaidAmount = 0m, CreditCarried = 3m, Status = CardStatementLedgerStatus.Paid });
    }

    [Fact]
    public void For_LegacyPaidMonth_CarriesCreditInUnchangedAndIgnoresSettledCharges()
    {
        Charge(9, -20m);
        var settled = Charge(10, 70m);
        settled.Settle(_barclays, new DateOnly(2026, 10, 20));
        StoredStatement(10).MarkLegacyPaid();

        var row = Walk(10)[1];

        row.Should().BeEquivalentTo(new { Total = 70m, CreditIn = 20m, AmountToPay = 0m, CreditCarried = 20m, PaidAmount = 70m, Status = CardStatementLedgerStatus.Paid });
    }

    [Fact]
    public void For_UnpaidMonthWithASettledCharge_ShowsItInTheTotalButDoesNotOweIt()
    {
        var settled = Charge(10, 50m);
        settled.Settle(_barclays, new DateOnly(2026, 10, 20));
        Charge(10, 25m);

        Walk(10).Single().Should().BeEquivalentTo(new { Total = 75m, AmountToPay = 25m, Status = CardStatementLedgerStatus.Unpaid });
    }

    [Fact]
    public void For_IgnoresAnotherCardsChargesAndStatements()
    {
        Charge(10, 30m);
        Charge(10, 99m, _visa);
        var visaStatement = CardStatement.Create(_visa, 2026, 10);
        visaStatement.MarkLegacyPaid();
        _statements.Add(visaStatement);

        Walk(10).Single().Should().BeEquivalentTo(new { Total = 30m, AmountToPay = 30m, Status = CardStatementLedgerStatus.Unpaid });
    }

    [Fact]
    public void MonthOf_ReturnsThatMonth_AFutureMonthGetsARow_ABeforeFirstMonthIsNull()
    {
        Charge(10, -35m);

        using (new AssertionScope())
        {
            CardStatementLedger.MonthOf(_amex, _expenses, _statements, _transfers, 2026, 10)!.CreditCarried.Should().Be(35m);
            CardStatementLedger.MonthOf(_amex, _expenses, _statements, _transfers, 2027, 2)!
                .Should().BeEquivalentTo(new { Year = 2027, Month = 2, CreditIn = 35m, Status = CardStatementLedgerStatus.NothingDue });
            CardStatementLedger.MonthOf(_amex, _expenses, _statements, _transfers, 2026, 9).Should().BeNull();
        }
    }

    [Fact]
    public void NextToPay_SkipsPaidAndNothingDueMonths()
    {
        Charge(8, 10m);
        PaidByTransfer(8, 10m);
        Charge(9, -30m);
        Charge(10, 12m);
        Charge(11, 40m);

        CardStatementLedger.NextToPay(_amex, _expenses, _statements, _transfers)!
            .Should().BeEquivalentTo(new { Month = 11, CreditIn = 18m, AmountToPay = 22m });
    }

    [Fact]
    public void NextToPay_NothingUnpaid_IsNull()
    {
        Charge(9, -30m);

        CardStatementLedger.NextToPay(_amex, _expenses, _statements, _transfers).Should().BeNull();
    }

    private Transfer CreditRefund(int month, decimal amount, Account? card = null) =>
        Transfer.CreateCreditRefund(new DateOnly(2026, month, 15), card ?? _amex, _barclays, amount, null, Today);

    private bool RefundKeepsAmounts(int month, decimal amount) =>
        CardStatementLedger.RefundKeepsAmountsToPay(CreditRefund(month, amount), _expenses, _statements, _transfers);

    [Fact]
    public void For_CreditRefund_CountsInItsDatesMonthAndLowersTheCreditCarried()
    {
        Charge(9, -100m);
        _transfers.Add(CreditRefund(10, 60m));

        var rows = Walk(11);

        using (new AssertionScope())
        {
            rows.Select(row => row.Month).Should().Equal(9, 10, 11);
            rows[1].Should().BeEquivalentTo(new { Total = 60m, CreditIn = 100m, AmountToPay = 0m, CreditCarried = 40m, Status = CardStatementLedgerStatus.NothingDue });
            rows[1].Statement.Should().BeNull();
            rows[2].Should().BeEquivalentTo(new { Total = 0m, CreditIn = 40m, CreditCarried = 40m });
        }
    }

    [Fact]
    public void For_CreditRefundAfterTheLastCharge_ExtendsTheWalkToItsMonth()
    {
        Charge(8, -50m);
        _transfers.Add(CreditRefund(11, 20m));

        Walk(9).Select(row => row.Month).Should().Equal(8, 9, 10, 11);
    }

    [Fact]
    public void For_CreditRefundOnly_StartsAtItsMonth()
    {
        _transfers.Add(CreditRefund(10, 20m));

        Walk(10).Single().Should().BeEquivalentTo(new { Month = 10, Total = 20m, AmountToPay = 20m, Status = CardStatementLedgerStatus.Unpaid });
    }

    [Fact]
    public void For_AnotherCardsCreditRefund_IsIgnored()
    {
        Charge(9, -100m);
        _transfers.Add(CreditRefund(10, 60m, _visa));

        Walk(10)[^1].CreditCarried.Should().Be(100m);
    }

    [Fact]
    public void For_StatementPaymentOutOfABank_IsNotACreditRefund()
    {
        Charge(10, 30m);
        PaidByTransfer(10, 30m);

        Walk(10).Single().Should().BeEquivalentTo(new { Total = 30m, CreditCarried = 0m, Status = CardStatementLedgerStatus.Paid });
    }

    [Fact]
    public void For_CreditRefundRemovedFromAPaidMonth_KeepsItPaidAndCarriesTheAmount()
    {
        Charge(9, -100m);
        var refund = CreditRefund(10, 60m);
        _transfers.Add(refund);
        Charge(10, 70m);
        PaidByTransfer(10, 30m);
        Charge(11, 20m);
        var before = Walk(11);

        _transfers.Remove(refund);
        var after = Walk(11);

        using (new AssertionScope())
        {
            before[1].Should().BeEquivalentTo(new { PaidAmount = 30m, CreditCarried = 0m, Status = CardStatementLedgerStatus.Paid });
            after[1].Should().BeEquivalentTo(new { PaidAmount = 30m, CreditCarried = 60m, Status = CardStatementLedgerStatus.Paid });
            after.Zip(before, (removed, kept) => removed.AmountToPay <= kept.AmountToPay).Should().OnlyContain(lowerOrEqual => lowerOrEqual);
            after[2].Should().BeEquivalentTo(new { CreditIn = 60m, AmountToPay = 0m, Status = CardStatementLedgerStatus.NothingDue });
        }
    }

    [Fact]
    public void RefundKeepsAmountsToPay_RefundWithinTheCarriedCredit_IsTrue()
    {
        Charge(9, -100m);

        using (new AssertionScope())
        {
            RefundKeepsAmounts(10, 60m).Should().BeTrue();
            RefundKeepsAmounts(9, 100m).Should().BeTrue();
        }
    }

    [Fact]
    public void RefundKeepsAmountsToPay_RefundThatLeavesAMonthOwing_IsFalse()
    {
        Charge(9, -30m);

        RefundKeepsAmounts(10, 40m).Should().BeFalse();
    }

    [Fact]
    public void RefundKeepsAmountsToPay_DatedBeforeTheCreditArose_IsFalse()
    {
        Charge(10, -50m);

        RefundKeepsAmounts(9, 50m).Should().BeFalse();
    }

    [Fact]
    public void RefundKeepsAmountsToPay_RefundRaisingALaterUnpaidMonth_IsFalse()
    {
        Charge(9, -50m);
        Charge(10, 70m);
        StoredStatement(10);

        RefundKeepsAmounts(9, 10m).Should().BeFalse();
    }

    [Fact]
    public void RefundKeepsAmountsToPay_MonthPaidByTransferAbsorbsIt_IsFalse()
    {
        Charge(9, 40m);
        PaidByTransfer(9, 40m);
        Charge(10, -30m);

        RefundKeepsAmounts(9, 30m).Should().BeFalse();
    }

    [Fact]
    public void RefundKeepsAmountsToPay_LegacyPaidMonthAbsorbsIt_IsFalse()
    {
        Charge(8, -30m);
        Charge(9, 40m).Settle(_barclays, new DateOnly(2026, 9, 25));
        StoredStatement(9).MarkLegacyPaid();

        RefundKeepsAmounts(9, 20m).Should().BeFalse();
    }

    [Fact]
    public void RefundKeepsAmountsToPay_CardWithNoRecords_IsFalse()
    {
        RefundKeepsAmounts(10, 20m).Should().BeFalse();
    }
}
