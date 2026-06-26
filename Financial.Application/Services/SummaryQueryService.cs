using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class SummaryQueryService : ISummaryQueryService
{
    private readonly IRepository _repository;

    public SummaryQueryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public AggregatedSummaryDTO GetBrokerSummary(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return new AggregatedSummaryDTO();

        var assets = _repository.GetAssetsByBroker(brokerName).Where(a => a.Active);
        return Aggregate(assets);
    }

    public AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return new AggregatedSummaryDTO();

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName).Where(a => a.Active);
        return Aggregate(assets);
    }

    private static AggregatedSummaryDTO Aggregate(IEnumerable<Asset> assets)
    {
        decimal totalBought = 0;
        decimal totalSold = 0;
        decimal totalCredits = 0;

        foreach (var asset in assets)
        {
            foreach (var transaction in asset.Transactions)
            {
                if (transaction.Type == Transaction.TransactionType.Buy)
                    totalBought += transaction.TotalPrice;
                else
                    totalSold += transaction.TotalPrice;
            }

            foreach (var credit in asset.Credits)
                totalCredits += credit.Value;
        }

        return new AggregatedSummaryDTO
        {
            TotalBought = totalBought,
            TotalSold = totalSold,
            TotalCredits = totalCredits,
        };
    }
}
