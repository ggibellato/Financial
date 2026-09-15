using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests.Rules;

public class TaxClassificationBackfillTests
{
    [Fact]
    public void Apply_ExistingDisposalRecordWithNoClassification_BackfillsExactlyOne()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);
        DisposalRecordBackfill.Apply(investments);

        var failures = TaxClassificationBackfill.Apply(investments);

        failures.Should().BeEmpty();
        var classification = asset.TaxClassifications.Should().ContainSingle().Subject;
        classification.SourceType.Should().Be(SourceType.Disposal);
        classification.SourceId.Should().Be(asset.DisposalRecords.Single().Id);
    }

    [Fact]
    public void Apply_QualifyingCreditWithNoClassification_BackfillsExactlyOne()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var credit = Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m);
        asset.AddCredit(credit);
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var failures = TaxClassificationBackfill.Apply(investments);

        failures.Should().BeEmpty();
        var classification = asset.TaxClassifications.Should().ContainSingle().Subject;
        classification.SourceType.Should().Be(SourceType.Credit);
        classification.SourceId.Should().Be(credit.Id);
    }

    [Fact]
    public void Apply_CalledTwice_DoesNotDuplicateClassifications()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        TaxClassificationBackfill.Apply(investments);
        TaxClassificationBackfill.Apply(investments);

        asset.TaxClassifications.Should().ContainSingle();
    }

    [Fact]
    public void Apply_SupersededDisposalRecord_IsNeverClassified()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Trading212", "GBP");
        var asset = Asset.Create("Asset A", "ISIN123", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 4m, 12m, 0m));
        broker.AddPortfolio("ISA").AddAsset(asset);
        investments.AddActiveBroker(broker);
        var record = asset.DisposalRecords.Single();
        record.Supersede(null);

        var failures = TaxClassificationBackfill.Apply(investments);

        failures.Should().BeEmpty();
        asset.TaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void Apply_CoversActiveAndHistoricBrokersAcrossMultiplePortfolios()
    {
        var investments = Investments.Create();

        var activeBroker = Broker.Create("Trading212", "GBP");
        var activeAsset = Asset.Create("Active Asset", "ISIN1", "LSE", "AAA");
        activeAsset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Coupon, 50m, 0m));
        activeBroker.AddPortfolio("ISA").AddAsset(activeAsset);
        investments.AddActiveBroker(activeBroker);

        var historicBroker = Broker.Create("OldBroker", "BRL");
        var historicAsset = Asset.Create("Historic Asset", "ISIN2", "BVMF", "BBB");
        historicAsset.AddCredit(Credit.Create(new DateTime(2019, 6, 1), Credit.CreditType.SecuritiesLendingIncome, 30m, 0m));
        historicBroker.AddPortfolio("Closed").AddAsset(historicAsset);
        investments.AddHistoricBroker(historicBroker);

        var failures = TaxClassificationBackfill.Apply(investments);

        failures.Should().BeEmpty();
        activeAsset.TaxClassifications.Should().ContainSingle();
        historicAsset.TaxClassifications.Should().ContainSingle();
    }
}
