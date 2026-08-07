using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;
using FluentAssertions;
using ReserveBucketEnum = Financial.CashFlow.Domain.Enums.ReserveBucket;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.ReserveBuckets;

public class ReserveBucketMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllFourBucketsWithCorrectPercentages()
    {
        var data = CashFlowData.Create();

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.BucketsSeededCount.Should().Be(4);
        summary.BucketsAlreadyPresentCount.Should().Be(0);
        data.ReserveBuckets.Should().ContainSingle(b => b.Name == "Investimento" && b.SplitPercentage == 33.33m && b.IsActive);
        data.ReserveBuckets.Should().ContainSingle(b => b.Name == "HouseTreats" && b.SplitPercentage == 33.33m && b.IsActive);
        data.ReserveBuckets.Should().ContainSingle(b => b.Name == "Ariana" && b.SplitPercentage == 16.67m && b.IsActive);
        data.ReserveBuckets.Should().ContainSingle(b => b.Name == "Gleison" && b.SplitPercentage == 16.67m && b.IsActive);
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRunAndKeepsSameIds()
    {
        var data = CashFlowData.Create();
        ReserveBucketMigrator.Migrate(data);
        var idsAfterFirstRun = data.ReserveBuckets.Select(b => b.Id).OrderBy(id => id).ToList();

        var secondSummary = ReserveBucketMigrator.Migrate(data);

        secondSummary.BucketsSeededCount.Should().Be(0);
        secondSummary.BucketsAlreadyPresentCount.Should().Be(4);
        data.ReserveBuckets.Should().HaveCount(4);
        data.ReserveBuckets.Select(b => b.Id).OrderBy(id => id).Should().Equal(idsAfterFirstRun);
    }

    [Fact]
    public void Migrate_WithSomeBucketsAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddReserveBucket(ReserveBucket.Create("Investimento", 33.33m));

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.BucketsSeededCount.Should().Be(3);
        summary.BucketsAlreadyPresentCount.Should().Be(1);
        data.ReserveBuckets.Should().HaveCount(4);
    }

    [Fact]
    public void Migrate_MovementWithMatchingBucketName_CountsAsResolved()
    {
        var data = CashFlowData.Create();
        var movement = ReserveMovement.Create(ReserveBucketEnum.Investimento, 100m, new DateOnly(2026, 7, 1), "Split");
        data.AddReserveMovement(movement);

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.MovementsResolvedCount.Should().Be(1);
        summary.UnresolvedMovements.Should().BeEmpty();
    }

    // No test exercises the unresolved-movement audit path directly: ReserveMovement.Bucket is
    // still the pre-F02 enum, which has exactly the same 4 fixed values the migrator always
    // (idempotently) seeds, so a movement can never fail to resolve through any call reachable
    // via the public API today. The audit logic is kept for forward compatibility - it becomes
    // reachable once F02 turns Bucket into a real, possibly-orphaned entity reference.

    [Fact]
    public void Migrate_WithDefaultSeed_ActiveSplitPercentagesSumToOneHundred()
    {
        var data = CashFlowData.Create();

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.ActiveSplitPercentageIsBalanced.Should().BeTrue();
        summary.ActiveSplitPercentageSum.Should().Be(100.00m);
    }

    [Fact]
    public void Migrate_WithActivePercentagesNotSummingToOneHundred_FlagsImbalanceWithoutFailing()
    {
        var data = CashFlowData.Create();
        data.AddReserveBucket(ReserveBucket.Create("Investimento", 50m));
        data.AddReserveBucket(ReserveBucket.Create("HouseTreats", 30m));

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.ActiveSplitPercentageIsBalanced.Should().BeFalse();
        summary.Render().Should().Contain("WARNING");
    }

    [Fact]
    public void Migrate_WithInactiveBucketExcludedFromSum_StillReportsBalanced()
    {
        var data = CashFlowData.Create();
        data.AddReserveBucket(ReserveBucket.Create("Investimento", 33.33m));
        data.AddReserveBucket(ReserveBucket.Create("HouseTreats", 33.33m));
        data.AddReserveBucket(ReserveBucket.Create("Ariana", 16.67m));
        data.AddReserveBucket(ReserveBucket.Create("Gleison", 16.67m));
        data.AddReserveBucket(ReserveBucket.Create("Retired", 50m, isActive: false));

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.ActiveSplitPercentageIsBalanced.Should().BeTrue();
        summary.ActiveSplitPercentageSum.Should().Be(100m);
    }

    [Fact]
    public void Migrate_WithNullData_Throws()
    {
        var act = () => ReserveBucketMigrator.Migrate(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
