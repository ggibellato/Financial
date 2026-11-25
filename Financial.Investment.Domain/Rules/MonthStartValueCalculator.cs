using System;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record MonthStartValue(decimal? Value, decimal? UnitPrice, DateOnly? PriceDate, string? Currency)
{
    public static readonly MonthStartValue Missing = new(null, null, null, null);
}

public static class MonthStartValueCalculator
{
    public const int PriceWindowDays = 7;

    public static DateOnly EarliestPriceDate(DateOnly monthStart) => monthStart.AddDays(-PriceWindowDays);

    public static MonthStartValue Calculate(Asset asset, DateOnly monthStart, string defaultCurrency)
    {
        var price = asset.GetPriceAsOf(monthStart);
        return price is null || price.Date < EarliestPriceDate(monthStart)
            ? MonthStartValue.Missing
            : new MonthStartValue(
                HoldingValuationCalculator.ValueAt(asset.Quantity, price),
                price.Price,
                price.Date,
                string.IsNullOrEmpty(price.Currency) ? defaultCurrency : price.Currency);
    }

    public static decimal? ChangePercent(decimal? from, decimal? to) =>
        from is null || to is null || from == 0m ? null : (to - from) / from * 100m;
}
