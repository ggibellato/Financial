using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class CardStatementTests
{
    private static readonly CreditCard BarclaysPlatinumVisa8003 = CreditCard.Create("BarclaysPlatinumVisa8003");
    private static readonly CreditCard ChaseMaster4023 = CreditCard.Create("ChaseMaster4023");
    private static readonly CreditCard BaAmex = CreditCard.Create("BaAmex");

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
        }
    }

    [Fact]
    public void MarkPaid_SetsIsPaidToTrue()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);

        statement.MarkPaid();

        statement.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void MarkPaid_CalledTwice_LeavesIsPaidTrueWithoutError()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);

        statement.MarkPaid();
        statement.MarkPaid();

        statement.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void MarkUnpaid_AfterMarkPaid_SetsIsPaidBackToFalse()
    {
        var statement = CardStatement.Create(ChaseMaster4023, 2026, 7);
        statement.MarkPaid();

        statement.MarkUnpaid();

        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public void Create_TwoStatements_HaveDifferentIds()
    {
        var first = CardStatement.Create(BaAmex, 2026, 7);
        var second = CardStatement.Create(BaAmex, 2026, 7);

        first.Id.Should().NotBe(second.Id);
    }
}
