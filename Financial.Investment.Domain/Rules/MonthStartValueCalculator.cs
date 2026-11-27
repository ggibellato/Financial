using System;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record MonthStartValue(decimal? Value, decimal? UnitPrice, DateOnly? PriceDate, string? Currency)
{
    public static readonly MonthStartValue Missing = new(null, null, null, null);
}

public static class MonthStartValueCalculator
{
    public const int PriceWindowDays = 7;

    public static (DateOnly Last, DateOnly This) MonthStartsFor(DateOnly today)
    {
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        return (thisMonthStart.AddMonths(-1), thisMonthStart);
    }

    public static DateOnly EarliestPriceDate(DateOnly monthStart) => monthStart.AddDays(-PriceWindowDays);

    public static DateOnly LatestPriceDate(DateOnly monthStart) => monthStart.AddDays(PriceWindowDays);

    public static MonthStartValue Calculate(Asset asset, DateOnly monthStart, string fallbackCurrency)
    {
        var price = NearestPrice(asset, monthStart);
        return price is null
            ? MonthStartValue.Missing
            : new MonthStartValue(
                ValueAt(asset, monthStart, price),
                price.Price,
                price.Date,
                string.IsNullOrEmpty(price.Currency) ? AssetCurrency(asset) ?? fallbackCurrency : price.Currency);
    }

    private static decimal ValueAt(Asset asset, DateOnly monthStart, AssetPriceSnapshot price)
    {
        var quantity = QuantityInPriceBasis(asset, monthStart, price.Date);
        return quantity == 0m ? 0m : HoldingValuationCalculator.ValueAt(quantity, price);
    }

    private static decimal QuantityInPriceBasis(Asset asset, DateOnly monthStart, DateOnly priceDate) =>
        asset.QuantityHeldOn(monthStart) * SplitFactorBetween(asset, monthStart, priceDate) / SplitFactorBetween(asset, priceDate, monthStart);

    private static decimal SplitFactorBetween(Asset asset, DateOnly after, DateOnly upTo) =>
        asset.CorporateActions
            .Where(action => action.Type == CorporateAction.CorporateActionType.Split
                && DateOnly.FromDateTime(action.EffectiveDate) > after
                && DateOnly.FromDateTime(action.EffectiveDate) <= upTo)
            .Aggregate(1m, (factor, split) => factor * CorporateActionReplay.RequireRatioFactor(split));

    private static AssetPriceSnapshot? NearestPrice(Asset asset, DateOnly monthStart) =>
        asset.PriceSnapshots
            .Select(price => new { Price = price, Distance = Math.Abs(price.Date.DayNumber - monthStart.DayNumber) })
            .Where(candidate => candidate.Distance <= PriceWindowDays)
            .MinBy(candidate => (candidate.Distance, candidate.Price.Date))
            ?.Price;

    private static string? AssetCurrency(Asset asset) => asset.Transactions.MaxBy(t => t.Date)?.Currency.ToString();

    public static decimal? ChangePercent(decimal? from, decimal? to) =>
        from is null || to is null || from == 0m ? null : (to - from) / from * 100m;
}
