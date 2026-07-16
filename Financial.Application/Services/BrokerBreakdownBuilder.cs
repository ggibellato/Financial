using Financial.Application.DTOs;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

internal static class BrokerBreakdownBuilder
{
    internal static IReadOnlyList<PortfolioBreakdownItemDTO> Build(Broker broker, Func<Asset, decimal> investedSelector)
    {
        return broker.Portfolios
            .Select(p => BuildPortfolioBreakdown(p, investedSelector))
            .Where(p => p.Assets.Count > 0)
            .OrderBy(p => p.PortfolioName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static PortfolioBreakdownItemDTO BuildPortfolioBreakdown(Portfolio portfolio, Func<Asset, decimal> investedSelector)
    {
        var assets = portfolio.Assets
            .Select(a => BuildAssetBreakdown(a, investedSelector))
            .Where(a => a.TotalInvested > 0)
            .OrderBy(a => a.AssetName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new PortfolioBreakdownItemDTO
        {
            PortfolioName = portfolio.Name,
            TotalInvested = assets.Sum(a => a.TotalInvested),
            Assets = assets,
        };
    }

    private static AssetBreakdownItemDTO BuildAssetBreakdown(Asset asset, Func<Asset, decimal> investedSelector) =>
        new()
        {
            AssetName = asset.Name,
            TotalInvested = investedSelector(asset),
        };
}
