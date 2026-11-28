using System;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record HoldingValuation(
    decimal? MarketValue,
    decimal CostOfUnitsHeld,
    decimal? UnrealisedGain,
    DateOnly? PriceAsOfDate,
    MarketStatus MarketStatus,
    decimal? PriceOnlyReturn,
    decimal? TotalReturn,
    decimal? TotalReturnNetOfTax = null);

public static class HoldingValuationCalculator
{
    public static HoldingValuation Calculate(
        decimal quantity, decimal averagePrice, AssetPriceSnapshot? price, ValuationRules rules, DateOnly valuationDate)
    {
        var costOfUnitsHeld = OpenPositionCostCalculator.CostOfUnitsHeld(quantity, averagePrice);

        if (price is null)
        {
            return quantity == 0m && !rules.ForBalanceHoldings
                ? new HoldingValuation(0m, costOfUnitsHeld, -costOfUnitsHeld, null, MarketStatus.Current, null, null)
                : new HoldingValuation(null, costOfUnitsHeld, null, null, MarketStatus.Unavailable, null, null);
        }

        var marketValue = rules.ValueAt(quantity, price);

        return new HoldingValuation(
            marketValue,
            costOfUnitsHeld,
            marketValue - costOfUnitsHeld,
            price.Date,
            MarketStatusCalculator.For(price.Date, valuationDate),
            null,
            null);
    }

    public static HoldingValuation NotMarkedToMarket(decimal quantity, decimal averagePrice) =>
        new(0m, OpenPositionCostCalculator.CostOfUnitsHeld(quantity, averagePrice), null, null, MarketStatus.Current, null, null);
}
