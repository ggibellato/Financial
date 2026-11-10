using Financial.CashFlow.Application.Services;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ReserveSplitRuleTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(99.99)]
    [InlineData(100.01)]
    public void IsBalanced_WithinTheToleranceOf100_IsTrue(double total)
    {
        ReserveSplitRule.IsBalanced((decimal)total).Should().BeTrue();
    }

    [Theory]
    [InlineData(99.98)]
    [InlineData(100.02)]
    [InlineData(60)]
    [InlineData(0)]
    public void IsBalanced_OutsideTheToleranceOf100_IsFalse(double total)
    {
        ReserveSplitRule.IsBalanced((decimal)total).Should().BeFalse();
    }
}
