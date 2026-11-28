using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class MonthStartValueCalculatorTests
{
    private static readonly DateOnly FirstOfNovember = new(2026, 11, 1);

    [Fact]
    public void Calculate_PriceSavedOnTheFirst_UsesIt()
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(2026, 10, 30), 10m), (FirstOfNovember, 11m));

        var result = MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL");

        using var _ = new AssertionScope();
        result.UnitPrice.Should().Be(11m);
        result.PriceDate.Should().Be(FirstOfNovember);
        result.Value.Should().Be(1100m);
    }

    [Fact]
    public void Calculate_NoPriceOnTheFirst_UsesTheNearestSavedPrice()
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(2026, 10, 28), 10m), (new DateOnly(2026, 11, 3), 11m));

        var result = MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL");

        using var _ = new AssertionScope();
        result.UnitPrice.Should().Be(11m);
        result.PriceDate.Should().Be(new DateOnly(2026, 11, 3));
        result.Value.Should().Be(1100m);
    }

    [Fact]
    public void Calculate_TwoPricesBeforeTheFirst_UsesTheLaterOne()
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(2026, 10, 28), 10m), (new DateOnly(2026, 10, 30), 11m));

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").PriceDate.Should().Be(new DateOnly(2026, 10, 30));
    }

    [Fact]
    public void Calculate_PricesEquallyNearBeforeAndAfter_UsesTheEarlierOne()
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(2026, 10, 30), 10m), (new DateOnly(2026, 11, 3), 11m));

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").PriceDate.Should().Be(new DateOnly(2026, 10, 30));
    }

    [Theory]
    [InlineData(2026, 10, 25)]
    [InlineData(2026, 11, 8)]
    public void Calculate_PriceSevenDaysFromTheFirst_IsStillUsed(int year, int month, int day)
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(year, month, day), 10m));

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Value.Should().Be(1000m);
    }

    [Theory]
    [InlineData(2026, 10, 24)]
    [InlineData(2026, 11, 9)]
    public void Calculate_PricesOnlyOutsideTheWindow_IsMissing(int year, int month, int day)
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(year, month, day), 10m));

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Should().Be(MonthStartValue.Missing);
    }

    [Fact]
    public void Calculate_NoSavedPrice_IsMissing()
    {
        var asset = MarketPricedAsset(100m);

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Should().Be(MonthStartValue.Missing);
    }

    [Theory]
    [InlineData(ValuationMethod.ProviderValue)]
    [InlineData(ValuationMethod.Manual)]
    public void Calculate_RecordedTotalValue_IsUsedWithoutMultiplyingByQuantity(ValuationMethod method)
    {
        var asset = Asset.Create("FUND", "ISIN", "", "FUND");
        asset.SetValuationMethod(method);
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 16m, 100m, 0m));
        asset.SetPrice(FirstOfNovember, 5000m, isManual: false);

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Value.Should().Be(5000m);
    }

    [Fact]
    public void Calculate_BalanceAssetWithAPriceSavedAsMarketPrice_UsesTheSnapshotTotal()
    {
        var asset = Asset.Create("FUND", "ISIN", "", "FUND");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 16m, 100m, 0m));
        asset.SetPrice(FirstOfNovember, 5000m, isManual: false);
        asset.SetValuationMethod(ValuationMethod.ProviderValue);

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Value.Should().Be(5000m);
    }

    [Fact]
    public void Calculate_PriceSavedWithoutACurrency_UsesTheCurrencyOfTheLatestTransactionByDate()
    {
        var asset = Asset.Create("VWRL", "ISIN", "LSE", "VWRL");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 10m, 100m, 0m, currency: Currency.GBP));
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 5), Transaction.TransactionType.Buy, 10m, 100m, 0m, currency: Currency.USD));
        asset.SetPrice(FirstOfNovember, 110m, isManual: false);

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Currency.Should().Be("GBP");
    }

    [Fact]
    public void Calculate_PriceAndAssetWithoutACurrency_UsesTheFallbackCurrency()
    {
        var asset = Asset.Create("FUND", "ISIN", "", "FUND");
        asset.SetValuationMethod(ValuationMethod.Manual);
        asset.SetPrice(FirstOfNovember, 500m, isManual: true);

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Currency.Should().Be("BRL");
    }

    [Theory]
    [InlineData(2026, 11, 25, 2026, 10, 2026, 11)]
    [InlineData(2026, 1, 1, 2025, 12, 2026, 1)]
    public void MonthStartsFor_GivesTheFirstOfLastMonthAndOfThisMonth(int year, int month, int day, int lastYear, int lastMonth, int thisYear, int thisMonth)
    {
        var (last, current) = MonthStartValueCalculator.MonthStartsFor(new DateOnly(year, month, day));

        last.Should().Be(new DateOnly(lastYear, lastMonth, 1));
        current.Should().Be(new DateOnly(thisYear, thisMonth, 1));
    }

    [Fact]
    public void PriceWindow_IsSevenDaysEitherSideOfTheFirst()
    {
        MonthStartValueCalculator.EarliestPriceDate(FirstOfNovember).Should().Be(new DateOnly(2026, 10, 25));
        MonthStartValueCalculator.LatestPriceDate(FirstOfNovember).Should().Be(new DateOnly(2026, 11, 8));
    }

    [Theory]
    [InlineData(1000, 1100, 10)]
    [InlineData(1000, 900, -10)]
    public void ChangePercent_BothValues_IsTheChangeFromTheFirst(decimal from, decimal to, decimal expected)
    {
        MonthStartValueCalculator.ChangePercent(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, 1100)]
    [InlineData(1000, null)]
    [InlineData(0, 1100)]
    public void ChangePercent_MissingOrZeroStart_IsNull(int? from, int? to)
    {
        MonthStartValueCalculator.ChangePercent((decimal?)from, (decimal?)to).Should().BeNull();
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Buy)]
    [InlineData(Transaction.TransactionType.Sell)]
    public void Calculate_TradeAfterTheFirst_UsesTheQuantityHeldOnTheFirst(Transaction.TransactionType type)
    {
        var asset = MarketPricedAsset(10m, (FirstOfNovember, 10m));
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 11, 15), type, 4m, 12m, 0m));

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Value.Should().Be(100m);
    }

    [Theory]
    [InlineData(11, 1, 11, 1)]
    [InlineData(10, 29, 10, 31)]
    public void Calculate_TradeOnOrBeforeTheFirst_CountsInTheQuantityHeldOnTheFirst(int priceMonth, int priceDay, int tradeMonth, int tradeDay)
    {
        var asset = MarketPricedAsset(10m, (new DateOnly(2026, priceMonth, priceDay), 10m));
        asset.AddTransaction(Transaction.Create(new DateTime(2026, tradeMonth, tradeDay), Transaction.TransactionType.Buy, 5m, 10m, 0m));

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Value.Should().Be(150m);
    }

    [Theory]
    [InlineData(ValuationMethod.MarketPrice)]
    [InlineData(ValuationMethod.ProviderValue)]
    public void Calculate_FirstBoughtAfterTheFirst_IsZeroWithThePriceUsed(ValuationMethod method)
    {
        var asset = Asset.Create("AAAA11", "ISIN", "BVMF", "AAAA11");
        asset.SetValuationMethod(method);
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 11, 3), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.SetPrice(new DateOnly(2026, 11, 3), 100m, isManual: false);

        var result = MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL");

        using var _ = new AssertionScope();
        result.Value.Should().Be(0m);
        result.UnitPrice.Should().Be(100m);
        result.PriceDate.Should().Be(new DateOnly(2026, 11, 3));
    }

    [Theory]
    [InlineData(10, 30, 100, 11, 1)]
    [InlineData(11, 3, 50, 11, 2)]
    [InlineData(11, 2, 50, 11, 2)]
    [InlineData(11, 1, 50, 10, 1)]
    public void Calculate_Split_ValuesTheQuantityOnTheSameSideOfTheSplitAsThePrice(int priceMonth, int priceDay, int price, int splitMonth, int splitDay)
    {
        var asset = MarketPricedAsset(10m, (new DateOnly(2026, priceMonth, priceDay), price));
        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2026, splitMonth, splitDay), 2m), DateTimeOffset.UnixEpoch);

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Value.Should().Be(1000m);
    }

    private static Asset MarketPricedAsset(decimal units, params (DateOnly Date, decimal Price)[] prices)
    {
        var asset = Asset.Create("AAAA11", "ISIN", "BVMF", "AAAA11");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, units, 9m, 0m));
        foreach (var (date, price) in prices)
        {
            asset.SetPrice(date, price, isManual: false);
        }
        return asset;
    }
}
