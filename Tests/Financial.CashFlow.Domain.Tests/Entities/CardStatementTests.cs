using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Financial.TestUtilities;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class CardStatementTests
{
    private static readonly Account BarclaysPlatinumVisa8003 = TestAccounts.CreditCard("BarclaysPlatinumVisa8003");
    private static readonly Account ChaseMaster4023 = TestAccounts.CreditCard("ChaseMaster4023");
    private static readonly Account BaAmex = TestAccounts.CreditCard("BaAmex");

    [Fact]
    public void Create_AssignsAllFieldsANewIdAndDefaultsIsPaidToFalse()
    {
        var statement = CardStatement.Create(BarclaysPlatinumVisa8003, 2026, 7);

        using (new AssertionScope())
        {
            statement.Id.Should().NotBeEmpty();
            statement.CreditCard.Should().Be(BarclaysPlatinumVisa8003);
            statement.Year.Should().Be(2026);
            statement.Month.Should().Be(7);
            statement.IsPaid.Should().BeFalse();
            statement.DueDate.Should().BeNull();
            statement.PaymentTransferId.Should().BeNull();
            statement.IsPaidByTransfer.Should().BeFalse();
        }
    }

    [Fact]
    public void SetDueDate_SetsAndClearsTheDueDate()
    {
        var statement = CardStatement.Create(BaAmex, 2026, 11);

        statement.SetDueDate(new DateOnly(2026, 11, 18));
        var set = statement.DueDate;
        statement.SetDueDate(null);

        using (new AssertionScope())
        {
            set.Should().Be(new DateOnly(2026, 11, 18));
            statement.DueDate.Should().BeNull();
        }
    }

    [Fact]
    public void MarkPaidBy_SetsIsPaidAndLinksTheTransfer()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);
        var transferId = Guid.Parse("7a1c2d3e-0000-4000-8000-000000000001");

        statement.MarkPaidBy(transferId);

        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeTrue();
            statement.PaymentTransferId.Should().Be(transferId);
            statement.IsPaidByTransfer.Should().BeTrue();
        }
    }

    [Fact]
    public void MarkUnpaid_AfterMarkPaidBy_ClearsIsPaidAndTheLink()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);
        statement.MarkPaidBy(Guid.Parse("7a1c2d3e-0000-4000-8000-000000000001"));

        statement.MarkUnpaid();

        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            statement.PaymentTransferId.Should().BeNull();
            statement.IsPaidByTransfer.Should().BeFalse();
        }
    }

    [Fact]
    public void MarkUnpaid_OnALegacyPaidStatement_SetsIsPaidBackToFalse()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7).MarkLegacyPaid();

        statement.MarkUnpaid();

        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            statement.IsPaidByTransfer.Should().BeFalse();
        }
    }

    [Fact]
    public void IsFor_MatchingCardYearAndMonth_ReturnsTrue()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);

        statement.IsFor(ChaseMaster4023.Id, 2026, 7).Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 8)]
    [InlineData(2027, 7)]
    public void IsFor_DifferentYearOrMonth_ReturnsFalse(int year, int month)
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);

        statement.IsFor(ChaseMaster4023.Id, year, month).Should().BeFalse();
    }

    [Fact]
    public void IsFor_DifferentCard_ReturnsFalse()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);

        statement.IsFor(BaAmex.Id, 2026, 7).Should().BeFalse();
    }

    [Fact]
    public void Create_ForABank_Throws()
    {
        var act = () => CardStatement.Create(TestAccounts.Bank("Chase"), 2026, 7);

        act.Should().Throw<ArgumentException>().WithMessage("Chase is not a card account.");
    }

    [Theory]
    [InlineData(2026, 6, true)]
    [InlineData(2025, 12, true)]
    [InlineData(2026, 7, false)]
    [InlineData(2026, 8, false)]
    public void IsAfter_ComparesYearThenMonth(int year, int month, bool expected)
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);

        statement.IsAfter(year, month).Should().Be(expected);
    }

    [Fact]
    public void SettleWithoutPayment_OpenStatement_RecordsTheDateAndClosesItWithoutPayingIt()
    {
        var statement = CardStatement.Create(BaAmex, 2026, 10);

        statement.SettleWithoutPayment(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 21));

        using (new AssertionScope())
        {
            statement.SettledOn.Should().Be(new DateOnly(2026, 11, 3));
            statement.IsClosed.Should().BeTrue();
            statement.IsPaid.Should().BeFalse();
            statement.PaymentTransferId.Should().BeNull();
        }
    }

    [Fact]
    public void SettleWithoutPayment_PaidOrSettledStatement_Throws()
    {
        var paid = CardStatement.Create(BaAmex, 2026, 10);
        paid.MarkPaidBy(Guid.NewGuid());
        var settled = CardStatement.Create(BaAmex, 2026, 11);
        settled.SettleWithoutPayment(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 21));

        using (new AssertionScope())
        {
            FluentActions.Invoking(() => paid.SettleWithoutPayment(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 21))).Should().Throw<InvalidOperationException>();
            FluentActions.Invoking(() => settled.SettleWithoutPayment(new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 21))).Should().Throw<InvalidOperationException>();
        }
    }

    [Fact]
    public void UnsettleWithoutPayment_SettledStatement_OpensItAgain()
    {
        var statement = CardStatement.Create(BaAmex, 2026, 10);
        statement.SettleWithoutPayment(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 21));

        statement.UnsettleWithoutPayment();

        using (new AssertionScope())
        {
            statement.SettledOn.Should().BeNull();
            statement.IsClosed.Should().BeFalse();
        }
    }

    [Fact]
    public void UnsettleWithoutPayment_StatementNotSettled_Throws()
    {
        var statement = CardStatement.Create(BaAmex, 2026, 10);

        FluentActions.Invoking(statement.UnsettleWithoutPayment).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsClosed_IsTrueOnlyWhenPaidOrSettled()
    {
        var open = CardStatement.Create(BaAmex, 2026, 10);
        var paid = CardStatement.Create(BaAmex, 2026, 11);
        paid.MarkPaidBy(Guid.NewGuid());

        using (new AssertionScope())
        {
            open.IsClosed.Should().BeFalse();
            paid.IsClosed.Should().BeTrue();
        }
    }

    [Fact]
    public void MarkPaidBy_SettledStatement_Throws()
    {
        var statement = CardStatement.Create(BaAmex, 2026, 10);
        statement.SettleWithoutPayment(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 21));

        FluentActions.Invoking(() => statement.MarkPaidBy(Guid.NewGuid())).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SettleWithoutPayment_DateAfterToday_ThrowsAndStaysOpen()
    {
        var statement = CardStatement.Create(BaAmex, 2026, 10);

        var act = () => statement.SettleWithoutPayment(new DateOnly(2026, 11, 22), new DateOnly(2026, 11, 21));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Date must not be in the future.");
            statement.IsClosed.Should().BeFalse();
        }
    }
}
