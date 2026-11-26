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
                HoldingValuationCalculator.ValueAt(asset.Quantity, price),
                price.Price,
                price.Date,
                string.IsNullOrEmpty(price.Currency) ? AssetCurrency(asset) ?? fallbackCurrency : price.Currency);
    }

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
