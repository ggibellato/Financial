using System;
using System.Collections.Generic;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class DisposalRecordTests
{
    [Fact]
    public void Create_ComputesCostBasisAsSumOfLotQuantityTimesUnitCost()
    {
        var lots = new List<DisposalLotConsumption>
        {
            new(Guid.NewGuid(), 4m, 10m),
            new(Guid.NewGuid(), 6m, 12m),
        };

        var record = DisposalRecord.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.FIFO, lots, 10m, 130m, Currency.GBP, "2026/27");

        record.CostBasis.Should().Be(4m * 10m + 6m * 12m);
    }

    [Fact]
    public void Create_ComputesGainLossAsProceedsMinusCostBasis()
    {
        var lots = new List<DisposalLotConsumption> { new(null, 10m, 8m) };

        var record = DisposalRecord.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost, lots, 10m, 95m, Currency.GBP, "2026/27");

        record.GainLoss.Should().Be(95m - 80m);
    }

    [Fact]
    public void Create_DefaultsToActiveStatusWithNoSupersedingRecord()
    {
        var lots = new List<DisposalLotConsumption> { new(null, 10m, 8m) };

        var record = DisposalRecord.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost, lots, 10m, 95m, Currency.GBP, "2026/27");

        record.Status.Should().Be(DisposalRecordStatus.Active);
        record.SupersededByRecordId.Should().BeNull();
    }

    [Fact]
    public void Create_NoLotsConsumed_ThrowsArgumentException()
    {
        Action act = () => DisposalRecord.Create(
            Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost,
            Array.Empty<DisposalLotConsumption>(), 10m, 95m, Currency.GBP, "2026/27");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithId_UsesTheSuppliedIdAndCreatedAt()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var lots = new List<DisposalLotConsumption> { new(null, 10m, 8m) };

        var record = DisposalRecord.CreateWithId(
            id, Guid.NewGuid(), new DateTime(2026, 5, 1), CostBasisMethod.AverageCost, lots, 10m, 95m, Currency.GBP, "2026/27", createdAt);

        record.Id.Should().Be(id);
        record.CreatedAt.Should().Be(createdAt);
    }
}
