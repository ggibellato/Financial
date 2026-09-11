using System;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record HoldingValuation(
    decimal? MarketValue,
    decimal CostOfUnitsHeld,
    decimal? UnrealisedGain,
    DateOnly? PriceAsOfDate,
    bool IsPriceStale,
    decimal? PriceOnlyReturn,
    decimal? TotalReturn);

public static class HoldingValuationCalculator
{
    public static HoldingValuation Calculate(decimal quantity, decimal averagePrice, AssetPriceSnapshot? price, DateOnly valuationDate)
    {
        var costOfUnitsHeld = OpenPositionCostCalculator.CostOfUnitsHeld(quantity, averagePrice);

        if (price is null)
        {
            return new HoldingValuation(null, costOfUnitsHeld, null, null, false, null, null);
        }

        var marketValue = quantity * price.Price;

        return new HoldingValuation(
            marketValue,
            costOfUnitsHeld,
            marketValue - costOfUnitsHeld,
            price.Date,
            price.Date < PreviousWeekday(valuationDate),
            null,
            null);
    }

    public static HoldingValuation NotMarkedToMarket(decimal quantity, decimal averagePrice) =>
        new(0m, OpenPositionCostCalculator.CostOfUnitsHeld(quantity, averagePrice), null, null, false, null, null);

    private static DateOnly PreviousWeekday(DateOnly date)
    {
        var previous = date.AddDays(-1);
        while (previous.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            previous = previous.AddDays(-1);
        }

        return previous;
    }
}
