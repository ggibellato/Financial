using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class PotSplitAllocatorTests
{
    private static readonly decimal[] ThirdsSplit = [33.33m, 33.33m, 33.34m];
    private static readonly decimal[] FortyThirtyTwentyTen = [40m, 30m, 20m, 10m];

    public static IEnumerable<object[]> Amounts()
    {
        var amounts = new List<decimal> { 0.01m, 0.02m, 0.03m, 0.99m, 1.01m, 100.01m, 1963.005m, 2205.00m, 9999.99m };
        amounts.AddRange(Enumerable.Range(1, 55).Select(i => i * 17.37m + 0.01m));
        return amounts.Select(a => new object[] { Math.Round(a, 2, MidpointRounding.AwayFromZero) });
    }

    [Fact]
    public void Allocate_ThreePotsSummingTo100_AssignsResidualToLargest()
    {
        var pots = CreatePots(ThirdsSplit);

        var amounts = PotSplitAllocator.Allocate(pots, 100.01m);

        amounts.Should().Equal(33.33m, 33.33m, 33.35m);
    }

    [Fact]
    public void Allocate_TiedLargestPercentage_AssignsResidualToFirstInOrder()
    {
        var pots = CreatePots([50m, 50m]);

        var amounts = PotSplitAllocator.Allocate(pots, 0.03m);

        amounts.Should().Equal(0.01m, 0.02m);
    }

    [Fact]
    public void Allocate_PercentagesSumTo99_TargetsNinetyNinePercent()
    {
        var pots = CreatePots([50m, 49m]);

        var amounts = PotSplitAllocator.Allocate(pots, 100.00m);

        amounts.Should().Equal(50.00m, 49.00m);
    }

    [Fact]
    public void Allocate_WhenPerPotRoundingAlreadySumsToTarget_LeavesAmountsUntouched()
    {
        var pots = CreatePots([33.33m, 33.33m, 16.67m, 16.67m]);

        var amounts = PotSplitAllocator.Allocate(pots, 1963m);

        amounts.Should().Equal(654.27m, 654.27m, 327.23m, 327.23m);
    }

    [Fact]
    public void Allocate_ZeroBase_ReturnsZeroAmounts()
    {
        var pots = CreatePots(ThirdsSplit);

        var amounts = PotSplitAllocator.Allocate(pots, 0m);

        amounts.Should().Equal(0m, 0m, 0m);
    }

    [Fact]
    public void Allocate_NoPots_ReturnsEmpty()
    {
        var amounts = PotSplitAllocator.Allocate([], 100m);

        amounts.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Amounts))]
    public void Allocate_ThreePotsSummingTo100_ConservesTheBaseToThePenny(decimal baseAmount)
    {
        var amounts = PotSplitAllocator.Allocate(CreatePots(ThirdsSplit), baseAmount);

        amounts.Sum().Should().Be(baseAmount);
    }

    [Theory]
    [MemberData(nameof(Amounts))]
    public void Allocate_FourPotsSummingTo100_ConservesTheBaseToThePenny(decimal baseAmount)
    {
        var amounts = PotSplitAllocator.Allocate(CreatePots(FortyThirtyTwentyTen), baseAmount);

        amounts.Sum().Should().Be(baseAmount);
    }

    private static List<Pot> CreatePots(decimal[] percentages) =>
        percentages.Select((p, i) => Pot.Create($"Pot {i + 1}", p)).ToList();
}
