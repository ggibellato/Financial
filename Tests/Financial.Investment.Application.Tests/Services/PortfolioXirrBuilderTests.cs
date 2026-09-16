using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

public class PortfolioXirrBuilderTests
{
    private static readonly DateTime AsOf = new(2026, 8, 14);
    private static readonly DateTime PurchaseDate = new(2025, 1, 1);

    [Fact]
    public void BuildSeries_AppendsOneTerminalEntryPerPricedActiveHolding()
    {
        var first = MakeBoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var second = MakeBoughtAsset("BBBB", quantity: 4m, unitPrice: 2m);

        var series = PortfolioXirrBuilder.BuildSeries(
            [ActiveHolding(first, 80m), ActiveHolding(second, 12m)], AsOf, AssetCashFlowBuilder.BuildWithCredits);

        TerminalAmounts(series).Should().BeEquivalentTo(new[] { 80m, 12m });
    }

    [Fact]
    public void BuildSeries_OmitsTerminalEntryForUnpricedActiveHolding()
    {
        var priced = MakeBoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var unpriced = MakeBoughtAsset("BBBB", quantity: 4m, unitPrice: 2m);

        var series = PortfolioXirrBuilder.BuildSeries(
            [ActiveHolding(priced, 80m), ActiveHolding(unpriced, marketValue: null)], AsOf, AssetCashFlowBuilder.BuildWithCredits);

        using var _ = new AssertionScope();
        TerminalAmounts(series).Should().BeEquivalentTo(new[] { 80m });
        series.Should().HaveCount(3, "both holdings still contribute their own dated purchase");
    }

    [Fact]
    public void BuildSeries_OmitsTerminalEntryForHistoricHolding()
    {
        var closed = MakeBoughtAsset("CLOSED", quantity: 5m, unitPrice: 10m);
        closed.AddTransaction(Transaction.Create(new DateTime(2025, 6, 1), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        var series = PortfolioXirrBuilder.BuildSeries(
            [HistoricHolding(closed)], AsOf, AssetCashFlowBuilder.BuildWithCredits);

        using var _ = new AssertionScope();
        TerminalAmounts(series).Should().BeEmpty();
        series.Should().HaveCount(2);
    }

    [Fact]
    public void BuildSeries_NetOfTaxSeriesUsesNetCreditAmounts_WhereTheGrossSeriesUsesCreditValues()
    {
        var asset = MakeBoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        asset.AddCredit(Credit.Create(new DateTime(2025, 6, 1), Credit.CreditType.Dividend, 100m, withheld: 15m));
        IReadOnlyList<PortfolioHolding> holdings = [ActiveHolding(asset, 80m)];

        var gross = PortfolioXirrBuilder.BuildSeries(holdings, AsOf, AssetCashFlowBuilder.BuildWithCredits);
        var netOfTax = PortfolioXirrBuilder.BuildSeries(holdings, AsOf, AssetCashFlowBuilder.BuildNetOfTaxWithCredits);

        using var _ = new AssertionScope();
        gross.Should().Contain(flow => flow.Amount == 100m);
        netOfTax.Should().Contain(flow => flow.Amount == 85m);
        TerminalAmounts(gross).Should().BeEquivalentTo(TerminalAmounts(netOfTax), "the terminal value is the same market value in both series");
    }

    [Fact]
    public void Calculate_SolvesOneRateOverEveryHoldingsFlowsCombined()
    {
        var asset = MakeBoughtAsset("AAAA", quantity: 10m, unitPrice: 10m);

        var result = PortfolioXirrBuilder.Calculate([ActiveHolding(asset, 110m)], new DateTime(2026, 1, 1));

        using var _ = new AssertionScope();
        result.GrossXirr.Should().BeApproximately(0.1m, 0.001m);
        result.NetXirr.Should().Be(result.GrossXirr, "nothing was withheld, so the two series are identical");
    }

    [Fact]
    public void Calculate_ReturnsNullWhenNoHoldingContributesAnInflow()
    {
        var asset = MakeBoughtAsset("AAAA", quantity: 10m, unitPrice: 10m);

        var result = PortfolioXirrBuilder.Calculate([ActiveHolding(asset, marketValue: null)], AsOf);

        using var _ = new AssertionScope();
        result.GrossXirr.Should().BeNull();
        result.NetXirr.Should().BeNull();
    }

    private static IEnumerable<decimal> TerminalAmounts(IReadOnlyList<(DateTime Date, decimal Amount)> series) =>
        series.Where(flow => flow.Date == AsOf).Select(flow => flow.Amount);

    private static Asset MakeBoughtAsset(string name, decimal quantity, decimal unitPrice)
    {
        var asset = Asset.Create(name, $"ISIN-{name}", "BVMF", name);
        asset.AddTransaction(Transaction.Create(PurchaseDate, Transaction.TransactionType.Buy, quantity, unitPrice, 0m));
        return asset;
    }

    private static PortfolioHolding ActiveHolding(Asset asset, decimal? marketValue) =>
        new(asset, Currency.BRL, IsActive: true, marketValue, UnrealisedGain: null);

    private static PortfolioHolding HistoricHolding(Asset asset) =>
        new(asset, Currency.BRL, IsActive: false, MarketValue: 0m, UnrealisedGain: null);
}
