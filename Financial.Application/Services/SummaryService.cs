using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class SummaryService : ISummaryService
{
    private readonly IRepository _repository;

    public SummaryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public AggregatedSummaryDTO GetBrokerSummary(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return new AggregatedSummaryDTO();

        var broker = _repository.GetBrokerList().FirstOrDefault(b => b.Name == brokerName);
        if (broker is null)
            return new AggregatedSummaryDTO();

        var assets = broker.Portfolios
            .Where(p => !NavigationMapper.IsEncerradas(p.Name))
            .SelectMany(p => p.Assets)
            .Where(a => a.Active);

        return Aggregate(assets);
    }

    public AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return new AggregatedSummaryDTO();

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName).Where(a => a.Active);
        return Aggregate(assets);
    }

    private static AggregatedSummaryDTO Aggregate(IEnumerable<Domain.Entities.Asset> assets)
    {
        decimal totalBought = 0, totalSold = 0, totalCredits = 0;

        foreach (var asset in assets)
        {
            var (bought, sold, credits) = NavigationMapper.CalculateTotals(asset);
            totalBought += bought;
            totalSold += sold;
            totalCredits += credits;
        }

        return new AggregatedSummaryDTO
        {
            TotalBought = totalBought,
            TotalSold = totalSold,
            TotalCredits = totalCredits,
            TotalInvested = totalBought - totalSold,
        };
    }
}
