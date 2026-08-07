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
}
