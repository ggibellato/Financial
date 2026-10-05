using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class ReserveSplitAllocatorTests
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
    public void Allocate_ThreeBucketsSummingTo100_AssignsResidualToLargest()
    {
        var buckets = CreateBuckets(ThirdsSplit);

        var amounts = ReserveSplitAllocator.Allocate(buckets, 100.01m);

        amounts.Should().Equal(33.33m, 33.33m, 33.35m);
    }

    [Fact]
    public void Allocate_TiedLargestPercentage_AssignsResidualToFirstInOrder()
    {
        var buckets = CreateBuckets([50m, 50m]);

        var amounts = ReserveSplitAllocator.Allocate(buckets, 0.03m);

        amounts.Should().Equal(0.01m, 0.02m);
    }

    [Fact]
    public void Allocate_PercentagesSumTo99_TargetsNinetyNinePercent()
    {
        var buckets = CreateBuckets([50m, 49m]);

        var amounts = ReserveSplitAllocator.Allocate(buckets, 100.00m);

        amounts.Should().Equal(50.00m, 49.00m);
    }

    [Fact]
    public void Allocate_WhenPerBucketRoundingAlreadySumsToTarget_LeavesAmountsUntouched()
    {
        var buckets = CreateBuckets([33.33m, 33.33m, 16.67m, 16.67m]);

        var amounts = ReserveSplitAllocator.Allocate(buckets, 1963m);

        amounts.Should().Equal(654.27m, 654.27m, 327.23m, 327.23m);
    }

    [Fact]
    public void Allocate_ZeroBase_ReturnsZeroAmounts()
    {
        var buckets = CreateBuckets(ThirdsSplit);

        var amounts = ReserveSplitAllocator.Allocate(buckets, 0m);

        amounts.Should().Equal(0m, 0m, 0m);
    }

    [Fact]
    public void Allocate_NoBuckets_ReturnsEmpty()
    {
        var amounts = ReserveSplitAllocator.Allocate([], 100m);

        amounts.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Amounts))]
    public void Allocate_ThreeBucketsSummingTo100_ConservesTheBaseToThePenny(decimal baseAmount)
    {
        var amounts = ReserveSplitAllocator.Allocate(CreateBuckets(ThirdsSplit), baseAmount);

        amounts.Sum().Should().Be(baseAmount);
    }

    [Theory]
    [MemberData(nameof(Amounts))]
    public void Allocate_FourBucketsSummingTo100_ConservesTheBaseToThePenny(decimal baseAmount)
    {
        var amounts = ReserveSplitAllocator.Allocate(CreateBuckets(FortyThirtyTwentyTen), baseAmount);

        amounts.Sum().Should().Be(baseAmount);
    }

    private static List<ReserveBucket> CreateBuckets(decimal[] percentages) =>
        percentages.Select((p, i) => ReserveBucket.Create($"Bucket {i + 1}", p)).ToList();
}
