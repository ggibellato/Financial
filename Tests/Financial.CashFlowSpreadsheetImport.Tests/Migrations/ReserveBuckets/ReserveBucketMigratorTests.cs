using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;
using FluentAssertions;
using Financial.TestUtilities;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.ReserveBuckets;

[Trait("Category", "Unit")]
public class ReserveBucketMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllFiveBucketsWithCorrectPercentages()
    {
        var data = CashFlowData.Create();

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.BucketsSeededCount.Should().Be(5);
        summary.BucketsAlreadyPresentCount.Should().Be(0);
        TestAccounts.ReservaIn(data).Pots!.Should().ContainSingle(b => b.Name == "Investimento" && b.SplitPercentage == 20m && b.IsActive);
        TestAccounts.ReservaIn(data).Pots!.Should().ContainSingle(b => b.Name == "HouseTreats" && b.SplitPercentage == 20m && b.IsActive);
        TestAccounts.ReservaIn(data).Pots!.Should().ContainSingle(b => b.Name == "Ariana" && b.SplitPercentage == 20m && b.IsActive);
        TestAccounts.ReservaIn(data).Pots!.Should().ContainSingle(b => b.Name == "Gleison" && b.SplitPercentage == 20m && b.IsActive);
        TestAccounts.ReservaIn(data).Pots!.Should().ContainSingle(b => b.Name == "Samuel" && b.SplitPercentage == 20m && b.IsActive);
    }

    [Fact]
    public void Migrate_WithNoPotsAccount_CreatesOneGbpReservaAccountHoldingThePots()
    {
        var data = CashFlowData.Create();
        data.AddAccount(TestAccounts.Bank("Barclays"));

        ReserveBucketMigrator.Migrate(data);

        var reserva = data.Accounts.Should().ContainSingle(a => a.Template == AccountTemplate.Pots).Which;
        reserva.Should().BeEquivalentTo(new { Name = "Reserva", Currency = Currency.GBP, IsActive = true });
        reserva.Pots.Should().HaveCount(5);
    }

    [Fact]
    public void Migrate_WithAPotsAccountUnderAnotherName_SeedsIntoItWithoutCreatingAnother()
    {
        var data = CashFlowData.Create();
        var savings = TestAccounts.Pots("Savings");
        data.AddAccount(savings);

        ReserveBucketMigrator.Migrate(data);

        data.Accounts.Should().ContainSingle().Which.Should().BeSameAs(savings);
        savings.Pots.Should().HaveCount(5);
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRunAndKeepsSameIds()
    {
        var data = CashFlowData.Create();
        ReserveBucketMigrator.Migrate(data);
        var idsAfterFirstRun = TestAccounts.ReservaIn(data).Pots!.Select(b => b.Id).OrderBy(id => id).ToList();

        var secondSummary = ReserveBucketMigrator.Migrate(data);

        secondSummary.BucketsSeededCount.Should().Be(0);
        secondSummary.BucketsAlreadyPresentCount.Should().Be(5);
        TestAccounts.ReservaIn(data).Pots!.Should().HaveCount(5);
        TestAccounts.ReservaIn(data).Pots!.Select(b => b.Id).OrderBy(id => id).Should().Equal(idsAfterFirstRun);
    }

    [Fact]
    public void Migrate_WithSomeBucketsAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("Investimento", 20m));

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.BucketsSeededCount.Should().Be(4);
        summary.BucketsAlreadyPresentCount.Should().Be(1);
        TestAccounts.ReservaIn(data).Pots!.Should().HaveCount(5);
    }

    [Fact]
    public void Migrate_MovementReferencingASeededBucket_CountsAsResolved()
    {
        var data = CashFlowData.Create();
        var bucket = ReserveBucket.Create("Investimento", 33.33m);
        TestAccounts.ReservaIn(data).AddPot(bucket);
        var movement = ReserveMovement.Create(bucket, 100m, new DateOnly(2026, 7, 1), "Split");
        data.AddReserveMovement(movement);

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.MovementsResolvedCount.Should().Be(1);
        summary.UnresolvedMovements.Should().BeEmpty();
    }

    [Fact]
    public void Migrate_MovementReferencingAnUnseededBucketInstance_IsFlaggedUnresolved()
    {
        var data = CashFlowData.Create();
        var orphanBucket = ReserveBucket.Create("RetiredBucket", 10m);
        var movement = ReserveMovement.Create(orphanBucket, 100m, new DateOnly(2026, 7, 1), "Orphaned");
        data.AddReserveMovement(movement);

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.UnresolvedMovements.Should().ContainSingle().Which.Id.Should().Be(movement.Id);
        summary.MovementsResolvedCount.Should().Be(0);
    }

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
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("Investimento", 50m));
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("HouseTreats", 30m));

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.ActiveSplitPercentageIsBalanced.Should().BeFalse();
        summary.Render().Should().Contain("WARNING");
    }

    [Fact]
    public void Migrate_WithInactiveBucketExcludedFromSum_StillReportsBalanced()
    {
        var data = CashFlowData.Create();
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("Investimento", 20m));
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("HouseTreats", 20m));
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("Ariana", 20m));
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("Gleison", 20m));
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("Samuel", 20m));
        TestAccounts.ReservaIn(data).AddPot(ReserveBucket.Create("Retired", 50m, isActive: false));

        var summary = ReserveBucketMigrator.Migrate(data);

        summary.ActiveSplitPercentageIsBalanced.Should().BeTrue();
        summary.ActiveSplitPercentageSum.Should().Be(100m);
    }
}
