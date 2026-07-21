using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.Infrastructure.Repositories;

public sealed class JSONRepository : IRepository
{
    private readonly IJsonStorage _storage;
    private readonly IInvestmentsSerializer _serializer;
    private readonly Investments _investiments;

    public JSONRepository(Investments investments, IJsonStorage storage, IInvestmentsSerializer serializer)
    {
        _investiments = investments ?? throw new ArgumentNullException(nameof(investments));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active)
    {
        return GetAssetsByBrokerInternal(name, scope);
    }

    public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active)
    {
        return GetPortfoliosByBroker(broker, scope)
            .Where(p => p.Name == portfolio)
            .SelectMany(p => p.Assets);
    }

    public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active)
    {
        return GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope)
            .FirstOrDefault(a => a.Name == assetName);
    }

    public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active)
    {
        return ResolveBrokers(scope);
    }

    public async Task SaveChangesAsync()
    {
        var json = _serializer.Serialize(_investiments);
        await _storage.WriteAsync(json).ConfigureAwait(false);
    }

    private IReadOnlyCollection<Broker> ResolveBrokers(InvestmentScope scope) =>
        scope == InvestmentScope.Historic ? _investiments.HistoricBrokers : _investiments.ActiveBrokers;

    private IEnumerable<Broker> GetBrokersByName(string brokerName, InvestmentScope scope) =>
        ResolveBrokers(scope).Where(b => b.Name == brokerName);

    private IEnumerable<Portfolio> GetPortfoliosByBroker(string brokerName, InvestmentScope scope) =>
        GetBrokersByName(brokerName, scope).SelectMany(b => b.Portfolios);

    private IEnumerable<Asset> GetAssetsByBrokerInternal(string brokerName, InvestmentScope scope) =>
        GetPortfoliosByBroker(brokerName, scope).SelectMany(p => p.Assets);
}
