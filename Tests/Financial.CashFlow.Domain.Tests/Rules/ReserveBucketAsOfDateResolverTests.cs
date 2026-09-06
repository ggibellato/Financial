using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

public class ReserveBucketAsOfDateResolverTests
{
    [Fact]
    public void LastDayOfPriorMonth_MidYear_ReturnsLastDayOfPreviousMonth()
    {
        var result = ReserveBucketAsOfDateResolver.LastDayOfPriorMonth(2026, 8);

        result.Should().Be(new DateOnly(2026, 7, 31));
    }

    [Fact]
    public void LastDayOfPriorMonth_January_ReturnsDecember31OfPriorYear()
    {
        var result = ReserveBucketAsOfDateResolver.LastDayOfPriorMonth(2026, 1);

        result.Should().Be(new DateOnly(2025, 12, 31));
    }

    [Fact]
    public void TotalBalanceAsOf_ExcludesMovementsAfterCutoff()
    {
        var bucket = ReserveBucket.Create("Investimento", 100m);
        var before = ReserveMovement.Create(bucket, 100m, new DateOnly(2026, 7, 31), "Contribution");
        var after = ReserveMovement.Create(bucket, 50m, new DateOnly(2026, 8, 1), "Later contribution");

        var total = ReserveBucketAsOfDateResolver.TotalBalanceAsOf([before, after], new DateOnly(2026, 7, 31));

        total.Should().Be(100m);
    }

    [Fact]
    public void TotalBalanceAsOf_IncludesMovementsAcrossAllBuckets()
    {
        var bucketA = ReserveBucket.Create("Investimento", 100m);
        var bucketB = ReserveBucket.Create("HouseTreats", 0m);
        var movementA = ReserveMovement.Create(bucketA, 100m, new DateOnly(2026, 7, 1), "Contribution A");
        var movementB = ReserveMovement.Create(bucketB, 30m, new DateOnly(2026, 7, 15), "Contribution B");

        var total = ReserveBucketAsOfDateResolver.TotalBalanceAsOf([movementA, movementB], new DateOnly(2026, 7, 31));

        total.Should().Be(130m);
    }
}
