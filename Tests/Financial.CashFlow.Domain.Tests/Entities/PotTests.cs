using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class PotTests
{
    [Fact]
    public void Create_WithValidValues_SetsAllProperties()
    {
        var pot = Pot.Create("Investimento", 33.33m, isActive: true);

        using (new AssertionScope())
        {
            pot.Id.Should().NotBeEmpty();
            pot.Name.Should().Be("Investimento");
            pot.SplitPercentage.Should().Be(33.33m);
            pot.IsActive.Should().BeTrue();
        }
    }


    [Fact]
    public void Create_DefaultsIsActiveToTrue()
    {
        var pot = Pot.Create("Investimento", 33.33m);

        pot.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithIsActiveFalse_AssignsFalse()
    {
        var pot = Pot.Create("Investimento", 33.33m, isActive: false);

        pot.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name)
    {
        var act = () => Pot.Create(name!, 33.33m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSplitPercentageBelowZero_Throws()
    {
        var act = () => Pot.Create("Investimento", -0.01m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSplitPercentageAboveHundred_Throws()
    {
        var act = () => Pot.Create("Investimento", 100.01m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Create_WithBoundarySplitPercentage_Succeeds(decimal splitPercentage)
    {
        var pot = Pot.Create("Investimento", splitPercentage);

        pot.SplitPercentage.Should().Be(splitPercentage);
    }

    [Fact]
    public void CalculateSplitAmount_AppliesPercentageAndRoundsAwayFromZero()
    {
        var pot = Pot.Create("Investimento", 33.33m);

        // 1963 * 33.33 / 100 = 654.2679, rounds to 654.27. Note this is 6 cents off the old
        // ReserveSplitCalculator's exact-1/3-fraction result (654.33) for the same input - an
        // accepted, documented rounding difference from moving off compiled-in exact fractions
        // to a stored 2-decimal percentage (see PRD Objectives).
        var amount = pot.CalculateSplitAmount(1963m);

        amount.Should().Be(654.27m);
    }

    [Fact]
    public void CalculateSplitAmount_WithZeroPercentage_ReturnsZero()
    {
        var pot = Pot.Create("Investimento", 0m);

        var amount = pot.CalculateSplitAmount(1963m);

        amount.Should().Be(0m);
    }

    [Fact]
    public void CalculateSplitAmount_WithHundredPercentage_ReturnsTheFullAmountRounded()
    {
        var pot = Pot.Create("Investimento", 100m);

        var amount = pot.CalculateSplitAmount(1963.005m);

        amount.Should().Be(1963.01m);
    }

    [Fact]
    public void CalculateSplitAmount_OnAnExactMidpoint_RoundsAwayFromZeroNotToEven()
    {
        var pot = Pot.Create("Investimento", 50m);

        // 1.01 * 50 / 100 = 0.505 exactly - a true midpoint. AwayFromZero rounds to 0.51;
        // banker's rounding (ToEven) would instead round to 0.50, since 0 is even.
        var amount = pot.CalculateSplitAmount(1.01m);

        amount.Should().Be(0.51m);
    }

    [Fact]
    public void Create_NameWithSurroundingSpaces_StoresItTrimmed()
    {
        var pot = Pot.Create(" Holidays ", 10m);

        pot.Name.Should().Be("Holidays");
    }

    [Fact]
    public void Update_NameWithSurroundingSpaces_StoresItAsGiven()
    {
        var pot = Pot.Create("Holidays", 10m);

        pot.Update(" Trips ", 10m, true);

        pot.Name.Should().Be(" Trips ");
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var pot = Pot.Create("Investimento", 33.33m, isActive: true);

        pot.Update("Ferias", 10m, isActive: false);

        using (new AssertionScope())
        {
            pot.Name.Should().Be("Ferias");
            pot.SplitPercentage.Should().Be(10m);
            pot.IsActive.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithoutAName_ThrowsAndLeavesPriorValuesUntouched(string? name)
    {
        var pot = Pot.Create("Investimento", 33.33m, isActive: true);

        var act = () => pot.Update(name!, 10m, isActive: false);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            pot.Name.Should().Be("Investimento");
            pot.SplitPercentage.Should().Be(33.33m);
            pot.IsActive.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Update_WithSplitPercentageOutOfRange_ThrowsAndLeavesPriorValuesUntouched(decimal splitPercentage)
    {
        var pot = Pot.Create("Investimento", 33.33m, isActive: true);

        var act = () => pot.Update("Ferias", splitPercentage, isActive: false);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            pot.Name.Should().Be("Investimento");
            pot.SplitPercentage.Should().Be(33.33m);
        }
    }

    [Fact]
    public void Update_DeactivatingThePot_SetsIsActiveFalseWithoutRemovingIt()
    {
        var pot = Pot.Create("Investimento", 33.33m, isActive: true);

        pot.Update(pot.Name, pot.SplitPercentage, isActive: false);

        pot.IsActive.Should().BeFalse();
    }
}
