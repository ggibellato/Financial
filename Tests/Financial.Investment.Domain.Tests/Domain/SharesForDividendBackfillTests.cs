using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class SharesForDividendBackfillTests
{
    [Fact]
    public void Apply_DividendCreditWithoutSharesForDividend_BackfillsQuantityHeldTheDayBefore()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1000m, 9m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var failures = SharesForDividendBackfill.Apply(investments);

        failures.Should().BeEmpty();
        asset.Credits.Single().SharesForDividend.Should().Be(1000m);
    }

    [Fact]
    public void Apply_ExcludesSharesBoughtOnOrAfterTheCreditDate()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 800m, 9m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 6, 1), Transaction.TransactionType.Buy, 200m, 20m, 0m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        SharesForDividendBackfill.Apply(investments);

        asset.Credits.Single().SharesForDividend.Should().Be(800m);
    }

    [Fact]
    public void Apply_JcpCredit_IsBackfilled()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 500m, 9m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.JCP, 100m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        SharesForDividendBackfill.Apply(investments);

        asset.Credits.Single().SharesForDividend.Should().Be(500m);
    }

    [Theory]
    [InlineData(Credit.CreditType.SecuritiesLendingIncome)]
    [InlineData(Credit.CreditType.Coupon)]
    public void Apply_NonDividendLikeCreditType_IsNeverBackfilled(Credit.CreditType type)
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 500m, 9m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), type, 100m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var failures = SharesForDividendBackfill.Apply(investments);

        failures.Should().BeEmpty();
        asset.Credits.Single().SharesForDividend.Should().BeNull();
    }

    [Fact]
    public void Apply_CreditAlreadyHasSharesForDividend_LeavesItUntouched()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1000m, 9m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 250m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var failures = SharesForDividendBackfill.Apply(investments);

        failures.Should().BeEmpty();
        asset.Credits.Single().SharesForDividend.Should().Be(250m);
    }

    [Fact]
    public void Apply_CalledTwice_DoesNotChangeTheAlreadyBackfilledValue()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1000m, 9m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        SharesForDividendBackfill.Apply(investments);
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 7, 1), Transaction.TransactionType.Buy, 500m, 9m, 0m));
        SharesForDividendBackfill.Apply(investments);

        asset.Credits.Single().SharesForDividend.Should().Be(1000m);
    }

    [Fact]
    public void Apply_NoSharesHeldBeforeTheCreditDate_ReportsAFailureAndLeavesItNull()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m);
        asset.AddCredit(credit);
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);

        var failures = SharesForDividendBackfill.Apply(investments);

        failures.Should().ContainSingle(f => f.CreditId == credit.Id);
        asset.Credits.Single().SharesForDividend.Should().BeNull();
    }

    [Fact]
    public void Apply_CoversActiveAndHistoricBrokersAcrossMultiplePortfolios()
    {
        var investments = Investments.Create();

        var activeBroker = Broker.Create("Trading212", "GBP");
        var activeAsset = Asset.Create("Active Asset", "ISIN1", "LSE", "AAA");
        activeAsset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        activeAsset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 5m));
        activeBroker.AddPortfolio("ISA").AddAsset(activeAsset);
        investments.AddActiveBroker(activeBroker);

        var historicBroker = Broker.Create("OldBroker", "GBP");
        var historicAsset = Asset.Create("Historic Asset", "ISIN2", "LSE", "BBB");
        historicAsset.AddTransaction(Transaction.Create(new DateTime(2019, 1, 1), Transaction.TransactionType.Buy, 20m, 20m, 0m));
        historicAsset.AddCredit(Credit.Create(new DateTime(2019, 6, 1), Credit.CreditType.JCP, 10m));
        historicBroker.AddPortfolio("Closed").AddAsset(historicAsset);
        investments.AddHistoricBroker(historicBroker);

        var failures = SharesForDividendBackfill.Apply(investments);

        failures.Should().BeEmpty();
        activeAsset.Credits.Single().SharesForDividend.Should().Be(10m);
        historicAsset.Credits.Single().SharesForDividend.Should().Be(20m);
    }
}
