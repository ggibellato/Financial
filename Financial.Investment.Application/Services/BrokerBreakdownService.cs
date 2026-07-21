using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

public sealed class BrokerBreakdownService : IBrokerBreakdownService
{
    private readonly IRepository _repository;

    public BrokerBreakdownService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return [];

        var broker = _repository.GetBrokerList(scope).FirstOrDefault(b => b.Name == brokerName);
        if (broker is null)
            return [];

        return scope == InvestmentScope.Historic
            ? BrokerBreakdownBuilder.Build(broker, CalculateGrossBought)
            : BrokerBreakdownBuilder.Build(broker, CalculateNetInvested);
    }

    private static decimal CalculateNetInvested(Asset asset)
    {
        var (totalBought, totalSold, _) = NavigationMapper.CalculateTotals(asset);
        return totalBought - totalSold;
    }

    private static decimal CalculateGrossBought(Asset asset) =>
        NavigationMapper.CalculateTotals(asset).TotalBought;
}
