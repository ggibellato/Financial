using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class ActiveBrokerBreakdownService : IActiveBrokerBreakdownService
{
    private readonly IRepository _repository;

    public ActiveBrokerBreakdownService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return [];

        var broker = _repository.GetBrokerList(InvestmentScope.Active).FirstOrDefault(b => b.Name == brokerName);
        if (broker is null)
            return [];

        return BrokerBreakdownBuilder.Build(broker, CalculateNetInvested);
    }

    private static decimal CalculateNetInvested(Asset asset)
    {
        var (totalBought, totalSold, _) = NavigationMapper.CalculateTotals(asset);
        return totalBought - totalSold;
    }
}
