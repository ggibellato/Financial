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
    public void Calculate_NoPriceOnTheFirst_UsesTheLatestSavedBeforeIt()
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(2026, 10, 30), 10m), (new DateOnly(2026, 11, 2), 11m));

        var result = MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL");

        using var _ = new AssertionScope();
        result.UnitPrice.Should().Be(10m);
        result.PriceDate.Should().Be(new DateOnly(2026, 10, 30));
        result.Value.Should().Be(1000m);
    }

    [Fact]
    public void Calculate_PriceSevenDaysBeforeTheFirst_IsStillUsed()
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(2026, 10, 25), 10m));

        MonthStartValueCalculator.Calculate(asset, FirstOfNovember, "BRL").Value.Should().Be(1000m);
    }

    [Fact]
    public void Calculate_LatestPriceOlderThanTheWindow_IsMissing()
    {
        var asset = MarketPricedAsset(100m, (new DateOnly(2026, 10, 20), 10m));

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
    public void EarliestPriceDate_IsSevenDaysBeforeTheFirst()
    {
        MonthStartValueCalculator.EarliestPriceDate(FirstOfNovember).Should().Be(new DateOnly(2026, 10, 25));
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
