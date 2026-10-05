using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
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
}
