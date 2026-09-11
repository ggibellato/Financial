using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

internal readonly record struct AssetTotals(decimal TotalBought, decimal TotalSold, decimal TotalCredits, decimal OpenPositionCost)
{
    internal static AssetTotals For(Asset asset)
    {
        var (totalBought, totalSold, totalCredits) = AssetTotalsCalculator.CalculateTotals(asset);
        return new AssetTotals(totalBought, totalSold, totalCredits, OpenPositionCostCalculator.CostOfUnitsHeld(asset));
    }
}
