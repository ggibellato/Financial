using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Infrastructure.Tests;

internal sealed class StubRepository : IRepository
{
    private readonly List<Broker> _brokers;

    public StubRepository(IEnumerable<Broker> brokers)
    {
        _brokers = brokers.ToList();
    }

    public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();

    public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();

    public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => _brokers;

    public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();

    public Task SaveChangesAsync() => throw new NotImplementedException();
}
