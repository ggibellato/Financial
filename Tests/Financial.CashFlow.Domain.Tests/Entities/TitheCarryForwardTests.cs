using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class TitheCarryForwardTests
{
    [Fact]
    public void Create_WithValidValues_SetsAllProperties()
    {
        var decision = TitheCarryForward.Create(2026, 8, 50m);

        decision.Year.Should().Be(2026);
        decision.Month.Should().Be(8);
        decision.Amount.Should().Be(50m);
    }

    [Fact]
    public void Create_DefaultsIncludedToTrue()
    {
        var decision = TitheCarryForward.Create(2026, 8, 50m);

        decision.Included.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Create_WithMonthOutOfRange_Throws(int month)
    {
        Action act = () => TitheCarryForward.Create(2026, month, 50m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveAmount_Throws(decimal amount)
    {
        Action act = () => TitheCarryForward.Create(2026, 8, amount);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetIncluded_False_TogglesTheFlag()
    {
        var decision = TitheCarryForward.Create(2026, 8, 50m);

        decision.SetIncluded(false);

        decision.Included.Should().BeFalse();
    }

    [Fact]
    public void SetIncluded_DoesNotChangeTheSnapshottedAmount()
    {
        var decision = TitheCarryForward.Create(2026, 8, 50m);

        decision.SetIncluded(false);
        decision.SetIncluded(true);

        decision.Amount.Should().Be(50m);
    }
}
