using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class TransactionQueryService : ITransactionQueryService
{
    private readonly IRepository _repository;

    public TransactionQueryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return Array.Empty<TransactionSummaryItemDTO>();

        return MapAndSort(_repository.GetAssetsByBroker(brokerName));
    }

    public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return Array.Empty<TransactionSummaryItemDTO>();

        return MapAndSort(_repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName));
    }

    private static IReadOnlyList<TransactionSummaryItemDTO> MapAndSort(IEnumerable<Domain.Entities.Asset> assets)
    {
        return assets
            .SelectMany(asset => asset.Transactions.Select(transaction => NavigationMapper.MapTransactionSummaryItem(asset, transaction)))
            .OrderBy(item => item.Date)
            .ThenBy(item => item.AssetName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
