using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Services;

internal static class AssetInvestedAmountSelector
{
    internal static decimal Select(InvestmentScope scope, decimal totalBought, decimal totalSold) =>
        scope == InvestmentScope.Historic ? totalBought : totalBought - totalSold;
}
