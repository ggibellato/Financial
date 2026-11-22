using System;
using System.Collections.Generic;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class RealisedGainTests
{
    [Fact]
    public void Create_ComputesCostBasisAsSumOfLotQuantityTimesUnitCost()
    {
        var lots = new List<RealisedGainLot>
        {
            new(Guid.NewGuid(), 4m, 10m),
            new(Guid.NewGuid(), 6m, 12m),
        };

        var record = RealisedGain.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.FIFO, lots, 10m, 130m, Currency.GBP, "2026/27", TestClock.Default);

        record.CostBasis.Should().Be(4m * 10m + 6m * 12m);
    }

    [Fact]
    public void Create_ComputesGainLossAsProceedsMinusCostBasis()
    {
        var lots = new List<RealisedGainLot> { new(null, 10m, 8m) };

        var record = RealisedGain.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost, lots, 10m, 95m, Currency.GBP, "2026/27", TestClock.Default);

        record.GainLoss.Should().Be(95m - 80m);
    }

    [Fact]
    public void Create_DefaultsToActiveStatusWithNoSupersedingRecord()
    {
        var lots = new List<RealisedGainLot> { new(null, 10m, 8m) };

        var record = RealisedGain.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost, lots, 10m, 95m, Currency.GBP, "2026/27", TestClock.Default);

        record.Status.Should().Be(RealisedGainStatus.Active);
        record.SupersededByRecordId.Should().BeNull();
    }

    [Fact]
    public void Create_NoLotsConsumed_ThrowsArgumentException()
    {
        Action act = () => RealisedGain.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost,
            Array.Empty<RealisedGainLot>(), 10m, 95m, Currency.GBP, "2026/27", TestClock.Default);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithId_UsesTheSuppliedIdAndCreatedAt()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var lots = new List<RealisedGainLot> { new(null, 10m, 8m) };

        var record = RealisedGain.CreateWithId(
            id, Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost, lots, 10m, 95m, Currency.GBP, "2026/27", createdAt);

        record.Id.Should().Be(id);
        record.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Supersede_ActiveRecord_SetsStatusAndLink()
    {
        var record = CreateActiveRecord();
        var replacementId = Guid.NewGuid();

        record.Supersede(replacementId);

        record.Status.Should().Be(RealisedGainStatus.Superseded);
        record.SupersededByRecordId.Should().Be(replacementId);
    }

    [Fact]
    public void Supersede_WithNoReplacement_SetsStatusWithNullLink()
    {
        var record = CreateActiveRecord();

        record.Supersede(null);

        record.Status.Should().Be(RealisedGainStatus.Superseded);
        record.SupersededByRecordId.Should().BeNull();
    }

    [Fact]
    public void Supersede_AlreadySuperseded_Throws()
    {
        var record = CreateActiveRecord();
        record.Supersede(Guid.NewGuid());

        Action act = () => record.Supersede(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    private static RealisedGain CreateActiveRecord()
    {
        var lots = new List<RealisedGainLot> { new(null, 10m, 8m) };
        return RealisedGain.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost, lots, 10m, 95m, Currency.GBP, "2026/27", TestClock.Default);
    }
}
