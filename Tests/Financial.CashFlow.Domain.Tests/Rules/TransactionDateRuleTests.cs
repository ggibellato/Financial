using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class TransactionDateRuleTests
{
    private static readonly DateOnly Today = new(2026, 11, 11);

    [Fact]
    public void EnsureNotInFuture_DateAfterToday_Throws()
    {
        var act = () => TransactionDateRule.EnsureNotInFuture(new DateOnly(2026, 11, 12), Today);

        act.Should().Throw<ArgumentException>().WithMessage("Date must not be in the future.");
    }

    [Fact]
    public void EnsureNotInFuture_Today_Succeeds()
    {
        var act = () => TransactionDateRule.EnsureNotInFuture(Today, Today);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNotInFuture_PastDate_Succeeds()
    {
        var act = () => TransactionDateRule.EnsureNotInFuture(new DateOnly(2020, 1, 1), Today);

        act.Should().NotThrow();
    }
}
