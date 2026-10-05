using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class TitheCarryForwardTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_WithValidValues_SetsAllProperties(bool included)
    {
        var decision = TitheCarryForward.Create(2026, 8, included);

        decision.Year.Should().Be(2026);
        decision.Month.Should().Be(8);
        decision.Included.Should().Be(included);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Create_WithMonthOutOfRange_Throws(int month)
    {
        Action act = () => TitheCarryForward.Create(2026, month, true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetIncluded_False_TogglesTheFlag()
    {
        var decision = TitheCarryForward.Create(2026, 8, true);

        decision.SetIncluded(false);

        decision.Included.Should().BeFalse();
    }
}
