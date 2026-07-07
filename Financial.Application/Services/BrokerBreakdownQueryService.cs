using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class BrokerBreakdownQueryService : IBrokerBreakdownQueryService
{
    private readonly IRepository _repository;

    public BrokerBreakdownQueryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return [];

        var broker = _repository.GetBrokerList().FirstOrDefault(b => b.Name == brokerName);
        if (broker is null)
            return [];

        return broker.Portfolios
            .Where(p => !NavigationMapper.IsEncerradas(p.Name))
            .Select(BuildPortfolioBreakdown)
            .Where(p => p.Assets.Count > 0)
            .OrderBy(p => p.PortfolioName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static PortfolioBreakdownItemDTO BuildPortfolioBreakdown(Portfolio portfolio)
    {
        var assets = portfolio.Assets
            .Where(a => a.Active)
            .Select(BuildAssetBreakdown)
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

    private static AssetBreakdownItemDTO BuildAssetBreakdown(Asset asset)
    {
        var (totalBought, totalSold, _) = NavigationMapper.CalculateTotals(asset);
        return new AssetBreakdownItemDTO
        {
            AssetName = asset.Name,
            TotalInvested = totalBought - totalSold,
        };
    }
}
