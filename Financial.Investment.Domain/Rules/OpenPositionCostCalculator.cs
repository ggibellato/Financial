using System;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class OpenPositionCostCalculator
{
    public static decimal CostOfUnitsHeld(Asset asset) =>
        CostOfUnitsHeld(asset.Quantity, asset.AveragePrice);

    public static decimal CostOfUnitsHeld(decimal quantity, decimal averagePrice) =>
        Math.Max(0m, quantity * averagePrice);
}
