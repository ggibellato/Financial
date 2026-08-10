using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class CardStatementTests
{
    [Fact]
    public void Create_AssignsAllFieldsANewIdAndDefaultsIsPaidToFalse()
    {
        var statement = CardStatement.Create(Enums.CreditCard.BarclaysPlatinumVisa8003, 2026, 7);

        using (new AssertionScope())
        {
            statement.Id.Should().NotBeEmpty();
            statement.Card.Should().Be(Enums.CreditCard.BarclaysPlatinumVisa8003);
            statement.Year.Should().Be(2026);
            statement.Month.Should().Be(7);
            statement.IsPaid.Should().BeFalse();
        }
    }

    [Fact]
    public void MarkPaid_SetsIsPaidToTrue()
    {
        var statement = CardStatement.Create(Enums.CreditCard.ChaseMaster4023, 2026, 7);

        statement.MarkPaid();

        statement.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void MarkPaid_CalledTwice_LeavesIsPaidTrueWithoutError()
    {
        var statement = CardStatement.Create(Enums.CreditCard.ChaseMaster4023, 2026, 7);

        statement.MarkPaid();
        statement.MarkPaid();

        statement.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void MarkUnpaid_AfterMarkPaid_SetsIsPaidBackToFalse()
    {
        var statement = CardStatement.Create(Enums.CreditCard.ChaseMaster4023, 2026, 7);
        statement.MarkPaid();

        statement.MarkUnpaid();

        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public void Create_TwoStatements_HaveDifferentIds()
    {
        var first = CardStatement.Create(Enums.CreditCard.BaAmex, 2026, 7);
        var second = CardStatement.Create(Enums.CreditCard.BaAmex, 2026, 7);

        first.Id.Should().NotBe(second.Id);
    }
}
