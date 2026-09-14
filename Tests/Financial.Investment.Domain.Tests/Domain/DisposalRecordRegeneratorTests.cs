using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class DisposalRecordRegeneratorTests
{
    [Fact]
    public void RegenerateAsset_BackdatedBuyBeforeExistingSale_SupersedesAndReplacesTheSale()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        var original = asset.DisposalRecords.Single();
        original.CostBasis.Should().Be(500m);

        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.AverageCost, new DateTime(2021, 1, 1));

        original.Status.Should().Be(DisposalRecordStatus.Superseded);
        var replacement = asset.DisposalRecords.Single(r => r.Status == DisposalRecordStatus.Active);
        original.SupersededByRecordId.Should().Be(replacement.Id);
        replacement.CostBasis.Should().Be(375m);
    }

    [Fact]
    public void RegenerateAsset_AnchorAfterEveryDisposal_ChangesNothing()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        var original = asset.DisposalRecords.Single();

        DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.AverageCost, new DateTime(2023, 1, 1));

        asset.DisposalRecords.Should().ContainSingle();
        original.Status.Should().Be(DisposalRecordStatus.Active);
    }

    [Fact]
    public void RegenerateBroker_MethodChangedToFifo_RegeneratesEveryAssetUnderBroker()
    {
        var broker = Broker.Create("Trading 212", "GBP");
        var portfolio = broker.AddPortfolio("ISA");
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 10m, 200m, 0m));
        portfolio.AddAsset(asset);
        var original = asset.DisposalRecords.Single();
        original.CostBasis.Should().Be(750m);

        broker.SetCostBasisMethod(CostBasisMethod.FIFO);
        DisposalRecordRegenerator.RegenerateBroker(broker);

        original.Status.Should().Be(DisposalRecordStatus.Superseded);
        var replacement = asset.DisposalRecords.Single(r => r.Status == DisposalRecordStatus.Active);
        replacement.CostBasis.Should().Be(500m);
    }

    [Fact]
    public void RegenerateAsset_ChainOfTwoRegenerations_IsFollowableBackToOriginal()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 100m, 0m));
        var first = asset.DisposalRecords.Single();

        DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.FIFO, new DateTime(2021, 1, 1));
        var second = asset.DisposalRecords.Single(r => r.Status == DisposalRecordStatus.Active);

        DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.AverageCost, new DateTime(2021, 1, 1));
        var third = asset.DisposalRecords.Single(r => r.Status == DisposalRecordStatus.Active);

        first.Status.Should().Be(DisposalRecordStatus.Superseded);
        first.SupersededByRecordId.Should().Be(second.Id);
        second.Status.Should().Be(DisposalRecordStatus.Superseded);
        second.SupersededByRecordId.Should().Be(third.Id);
        asset.DisposalRecords.Should().HaveCount(3);
    }

    [Fact]
    public void RegenerateAsset_SupersededRecord_ExcludedFromRealizedGainLoss()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 10m, 100m, 0m));

        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.AverageCost, new DateTime(2020, 1, 1));

        var active = asset.DisposalRecords.Single(r => r.Status == DisposalRecordStatus.Active);
        asset.RealizedGainLoss.Should().Be(active.GainLoss);
    }

    [Fact]
    public void RegenerateAsset_SpecificId_ReplaysAllocationReconstructedFromSupersededRecord()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        var buy1 = Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 50m, 0m);
        var buy2 = Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Buy, 5m, 100m, 0m);
        asset.AddTransaction(buy1);
        asset.AddTransaction(buy2);
        var sell = Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 200m, 0m);
        asset.RecordTransaction(sell, CostBasisMethod.SpecificId, new[] { new SpecificLotAllocation(buy2.Id, 5m) });
        asset.DisposalRecords.Single().CostBasis.Should().Be(500m);

        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m));
        DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.SpecificId, new DateTime(2020, 1, 1));

        var replacement = asset.DisposalRecords.Single(r => r.Status == DisposalRecordStatus.Active);
        replacement.LotsConsumed.Should().ContainSingle(l => l.SourceTransactionId == buy2.Id);
        replacement.CostBasis.Should().Be(500m);
    }

    [Fact]
    public void RegenerateAsset_SpecificIdAllocationNoLongerValid_ThrowsAndLeavesPriorRecordActive()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        var buy = Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 50m, 0m);
        asset.AddTransaction(buy);
        var sell = Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 200m, 0m);
        asset.RecordTransaction(sell, CostBasisMethod.SpecificId, new[] { new SpecificLotAllocation(buy.Id, 5m) });
        var original = asset.DisposalRecords.Single();

        var shrunkBuy = Transaction.CreateWithId(
            buy.Id, buy.Date, buy.Type, 3m, buy.UnitPrice, buy.Fees, buy.Withheld, buy.Currency, buy.FxRateSnapshot);
        asset.UpdateTransaction(shrunkBuy);

        Action act = () => DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.SpecificId, buy.Date);

        act.Should().Throw<InvestmentRuleViolationException>();
        original.Status.Should().Be(DisposalRecordStatus.Active);
        asset.DisposalRecords.Should().ContainSingle();
    }

    [Fact]
    public void RegenerateAsset_TransactionDeleted_RetiresItsRecordWithNoReplacement()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        var sell = Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 100m, 0m);
        asset.RecordTransaction(sell);
        var original = asset.DisposalRecords.Single();

        asset.RemoveTransaction(sell.Id);
        DisposalRecordRegenerator.RegenerateAsset(asset, CostBasisMethod.AverageCost, sell.Date);

        original.Status.Should().Be(DisposalRecordStatus.Superseded);
        original.SupersededByRecordId.Should().BeNull();
        asset.DisposalRecords.Should().NotContain(r => r.Status == DisposalRecordStatus.Active);
    }
}
