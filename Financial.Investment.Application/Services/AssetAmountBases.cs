using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Services;

internal readonly record struct AssetAmountBases(decimal InvestedAmount, decimal WeightBasis, decimal IncomeYieldBasis)
{
    internal static AssetAmountBases For(InvestmentScope scope, AssetTotals totals)
    {
        var investedAmount = scope == InvestmentScope.Historic ? totals.TotalBought : totals.OpenPositionCost;
        var weightBasis = scope == InvestmentScope.Historic ? totals.TotalBought : totals.TotalBought - totals.TotalSold;

        return new AssetAmountBases(investedAmount, weightBasis, investedAmount);
    }
}
