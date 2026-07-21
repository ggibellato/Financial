using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

public sealed class SummaryService : ISummaryService
{
    private readonly IRepository _repository;

    public SummaryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public AggregatedSummaryDTO GetBrokerSummary(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return new AggregatedSummaryDTO();

        var broker = _repository.GetBrokerList(scope).FirstOrDefault(b => b.Name == brokerName);
        if (broker is null)
            return new AggregatedSummaryDTO();

        var assets = broker.Portfolios.SelectMany(p => p.Assets);

        return Aggregate(assets);
    }

    public AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return new AggregatedSummaryDTO();

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope);
        return Aggregate(assets);
    }

    private static AggregatedSummaryDTO Aggregate(IEnumerable<Asset> assets)
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
