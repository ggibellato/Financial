using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests.Services;

public class AssetCashFlowBuilderTests
{
    private static Asset MakeAsset(string name) => Asset.Create(name, "ISIN", "BVMF", name);

    [Fact]
    public void ConcatenateWithoutCredits_TwoAssetsSameDate_KeepsBothEntriesRatherThanMerging()
    {
        var date = new DateTime(2026, 1, 1);
        var asset1 = MakeAsset("AAAA");
        asset1.AddTransaction(Transaction.Create(date, Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var asset2 = MakeAsset("BBBB");
        asset2.AddTransaction(Transaction.Create(date, Transaction.TransactionType.Sell, 10m, 5m, 0m));

        var result = AssetCashFlowBuilder.ConcatenateWithoutCredits([asset1, asset2]);

        result.Should().HaveCount(2);
        result.Should().Contain(cf => cf.Date == date && cf.Amount == -50m);
        result.Should().Contain(cf => cf.Date == date && cf.Amount == 50m);
    }

    [Fact]
    public void ConcatenateWithoutCredits_ConcatenatesEveryAssetsOwnFlows()
    {
        var asset1 = MakeAsset("AAAA");
        asset1.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var asset2 = MakeAsset("BBBB");
        asset2.AddTransaction(Transaction.Create(new DateTime(2025, 6, 1), Transaction.TransactionType.Buy, 4m, 2m, 0m));
        asset2.AddTransaction(Transaction.Create(new DateTime(2025, 7, 1), Transaction.TransactionType.Sell, 4m, 3m, 0m));

        var result = AssetCashFlowBuilder.ConcatenateWithoutCredits([asset1, asset2]);

        result.Should().HaveCount(3);
    }

    [Fact]
    public void ConcatenateWithoutCredits_ExcludesCredits()
    {
        var asset = MakeAsset("AAAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2025, 6, 1), Credit.CreditType.Dividend, 5m));

        var result = AssetCashFlowBuilder.ConcatenateWithoutCredits([asset]);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void ConcatenateWithCredits_IncludesEveryAssetsCredits()
    {
        var asset1 = MakeAsset("AAAA");
        asset1.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset1.AddCredit(Credit.Create(new DateTime(2025, 6, 1), Credit.CreditType.Dividend, 5m));
        var asset2 = MakeAsset("BBBB");
        asset2.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 4m, 2m, 0m));
        asset2.AddCredit(Credit.Create(new DateTime(2025, 7, 1), Credit.CreditType.Dividend, 3m));

        var result = AssetCashFlowBuilder.ConcatenateWithCredits([asset1, asset2]);

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ConcatenateWithoutCredits_NoAssets_ReturnsEmpty()
    {
        var result = AssetCashFlowBuilder.ConcatenateWithoutCredits([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ConcatenateWithCredits_NoAssets_ReturnsEmpty()
    {
        var result = AssetCashFlowBuilder.ConcatenateWithCredits([]);

        result.Should().BeEmpty();
    }
}
