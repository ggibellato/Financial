using System;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class OpenPositionCostCalculator
{
    public static decimal CostOfUnitsHeld(Asset asset) =>
        Math.Max(0m, asset.Quantity * asset.AveragePrice);
}
