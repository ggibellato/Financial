using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class ReserveBucketTests
{
    [Fact]
    public void Create_WithValidValues_SetsAllProperties()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m, isActive: true);

        using (new AssertionScope())
        {
            bucket.Id.Should().NotBeEmpty();
            bucket.Name.Should().Be("Investimento");
            bucket.SplitPercentage.Should().Be(33.33m);
            bucket.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public void Create_TwoBuckets_HaveDifferentIds()
    {
        var first = ReserveBucket.Create("Investimento", 33.33m);
        var second = ReserveBucket.Create("HouseTreats", 33.33m);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_DefaultsIsActiveToTrue()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m);

        bucket.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithIsActiveFalse_AssignsFalse()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m, isActive: false);

        bucket.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name)
    {
        var act = () => ReserveBucket.Create(name!, 33.33m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSplitPercentageBelowZero_Throws()
    {
        var act = () => ReserveBucket.Create("Investimento", -0.01m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSplitPercentageAboveHundred_Throws()
    {
        var act = () => ReserveBucket.Create("Investimento", 100.01m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Create_WithBoundarySplitPercentage_Succeeds(decimal splitPercentage)
    {
        var bucket = ReserveBucket.Create("Investimento", splitPercentage);

        bucket.SplitPercentage.Should().Be(splitPercentage);
    }

    [Fact]
    public void CalculateSplitAmount_AppliesPercentageAndRoundsAwayFromZero()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m);

        // 1963 * 33.33 / 100 = 654.2679, rounds to 654.27. Note this is 6 cents off the old
        // ReserveSplitCalculator's exact-1/3-fraction result (654.33) for the same input - an
        // accepted, documented rounding difference from moving off compiled-in exact fractions
        // to a stored 2-decimal percentage (see PRD Objectives).
        var amount = bucket.CalculateSplitAmount(1963m);

        amount.Should().Be(654.27m);
    }

    [Fact]
    public void CalculateSplitAmount_WithZeroPercentage_ReturnsZero()
    {
        var bucket = ReserveBucket.Create("Investimento", 0m);

        var amount = bucket.CalculateSplitAmount(1963m);

        amount.Should().Be(0m);
    }

    [Fact]
    public void CalculateSplitAmount_WithHundredPercentage_ReturnsTheFullAmountRounded()
    {
        var bucket = ReserveBucket.Create("Investimento", 100m);

        var amount = bucket.CalculateSplitAmount(1963.005m);

        amount.Should().Be(1963.01m);
    }

    [Fact]
    public void CalculateSplitAmount_OnAnExactMidpoint_RoundsAwayFromZeroNotToEven()
    {
        var bucket = ReserveBucket.Create("Investimento", 50m);

        // 1.01 * 50 / 100 = 0.505 exactly - a true midpoint. AwayFromZero rounds to 0.51;
        // banker's rounding (ToEven) would instead round to 0.50, since 0 is even.
        var amount = bucket.CalculateSplitAmount(1.01m);

        amount.Should().Be(0.51m);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m, isActive: true);

        bucket.Update("Ferias", 10m, isActive: false);

        using (new AssertionScope())
        {
            bucket.Name.Should().Be("Ferias");
            bucket.SplitPercentage.Should().Be(10m);
            bucket.IsActive.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithoutAName_ThrowsAndLeavesPriorValuesUntouched(string? name)
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m, isActive: true);

        var act = () => bucket.Update(name!, 10m, isActive: false);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            bucket.Name.Should().Be("Investimento");
            bucket.SplitPercentage.Should().Be(33.33m);
            bucket.IsActive.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Update_WithSplitPercentageOutOfRange_ThrowsAndLeavesPriorValuesUntouched(decimal splitPercentage)
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m, isActive: true);

        var act = () => bucket.Update("Ferias", splitPercentage, isActive: false);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            bucket.Name.Should().Be("Investimento");
            bucket.SplitPercentage.Should().Be(33.33m);
        }
    }

    [Fact]
    public void Update_DeactivatingTheBucket_SetsIsActiveFalseWithoutRemovingIt()
    {
        var bucket = ReserveBucket.Create("Investimento", 33.33m, isActive: true);

        bucket.Update(bucket.Name, bucket.SplitPercentage, isActive: false);

        bucket.IsActive.Should().BeFalse();
    }
}
