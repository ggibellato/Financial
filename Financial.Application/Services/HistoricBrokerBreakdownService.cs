using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class HistoricBrokerBreakdownService : IHistoricBrokerBreakdownService
{
    private readonly IRepository _repository;

    public HistoricBrokerBreakdownService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return [];

        var broker = _repository.GetBrokerList(InvestmentScope.Historic).FirstOrDefault(b => b.Name == brokerName);
        if (broker is null)
            return [];

        return BrokerBreakdownBuilder.Build(broker, CalculateGrossBought);
    }

    private static decimal CalculateGrossBought(Asset asset) =>
        NavigationMapper.CalculateTotals(asset).TotalBought;
}
