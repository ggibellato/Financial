using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class DisposalRecordBackfillTests
{
    [Fact]
    public void Apply_ExistingSellWithNoDisposalRecord_BackfillsExactlyOneActiveRecord()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var failures = DisposalRecordBackfill.Apply(investments);

        failures.Should().BeEmpty();
        asset.DisposalRecords.Should().ContainSingle();
        var record = asset.DisposalRecords.Single();
        record.Status.Should().Be(DisposalRecordStatus.Active);
        record.Method.Should().Be(CostBasisMethod.AverageCost);
        record.GainLoss.Should().Be(50m);
    }

    [Fact]
    public void Apply_CalledTwice_DoesNotDuplicateRecords()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        DisposalRecordBackfill.Apply(investments);
        DisposalRecordBackfill.Apply(investments);

        asset.DisposalRecords.Should().ContainSingle();
    }

    [Fact]
    public void Apply_TransactionAlreadyHasADisposalRecord_LeavesItUntouched()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);
        var existingRecord = asset.DisposalRecords.Single();

        var failures = DisposalRecordBackfill.Apply(investments);

        failures.Should().BeEmpty();
        asset.DisposalRecords.Should().ContainSingle();
        asset.DisposalRecords.Single().Id.Should().Be(existingRecord.Id);
    }

    [Fact]
    public void Apply_TransferOut_IsNeverBackfilled()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.TransferOut, 5m, 100m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var failures = DisposalRecordBackfill.Apply(investments);

        failures.Should().BeEmpty();
        asset.DisposalRecords.Should().BeEmpty();
    }

    [Fact]
    public void Apply_CoversActiveAndHistoricBrokersAcrossMultiplePortfolios()
    {
        var investments = Investments.Create();

        var activeBroker = Broker.Create("Trading212", "GBP");
        var activeAsset = Asset.Create("Active Asset", "ISIN1", "LSE", "AAA");
        activeAsset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        activeAsset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 4m, 12m, 0m));
        activeBroker.AddPortfolio("ISA").AddAsset(activeAsset);
        investments.AddActiveBroker(activeBroker);

        var historicBroker = Broker.Create("OldBroker", "GBP");
        var historicAsset = Asset.Create("Historic Asset", "ISIN2", "LSE", "BBB");
        historicAsset.AddTransaction(Transaction.Create(new DateTime(2019, 1, 1), Transaction.TransactionType.Buy, 5m, 20m, 0m));
        historicAsset.AddTransaction(Transaction.Create(new DateTime(2019, 6, 1), Transaction.TransactionType.Sell, 5m, 25m, 0m));
        historicBroker.AddPortfolio("Closed").AddAsset(historicAsset);
        investments.AddHistoricBroker(historicBroker);

        var failures = DisposalRecordBackfill.Apply(investments);

        failures.Should().BeEmpty();
        activeAsset.DisposalRecords.Should().ContainSingle();
        historicAsset.DisposalRecords.Should().ContainSingle();
    }
}
