using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class CardStatementTests
{
    [Fact]
    public void Create_AssignsAllFieldsANewIdAndDefaultsIsPaidToFalse()
    {
        var statement = CardStatement.Create(CreditCard.BarclaysPlatinumVisa8003, 2026, 7);

        statement.Id.Should().NotBeEmpty();
        statement.Card.Should().Be(CreditCard.BarclaysPlatinumVisa8003);
        statement.Year.Should().Be(2026);
        statement.Month.Should().Be(7);
        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public void MarkPaid_SetsIsPaidToTrue()
    {
        var statement = CardStatement.Create(CreditCard.ChaseMaster4023, 2026, 7);

        statement.MarkPaid();

        statement.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void MarkPaid_CalledTwice_LeavesIsPaidTrueWithoutError()
    {
        var statement = CardStatement.Create(CreditCard.ChaseMaster4023, 2026, 7);

        statement.MarkPaid();
        statement.MarkPaid();

        statement.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void MarkUnpaid_AfterMarkPaid_SetsIsPaidBackToFalse()
    {
        var statement = CardStatement.Create(CreditCard.ChaseMaster4023, 2026, 7);
        statement.MarkPaid();

        statement.MarkUnpaid();

        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public void Create_TwoStatements_HaveDifferentIds()
    {
        var first = CardStatement.Create(CreditCard.BaAmex, 2026, 7);
        var second = CardStatement.Create(CreditCard.BaAmex, 2026, 7);

        first.Id.Should().NotBe(second.Id);
    }
}
