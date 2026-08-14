using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;

namespace Financial.Investment.Infrastructure.Repositories;

public sealed class InvestmentJsonRepository : IInvestmentRepository, ISyncStatusProvider
{
    private readonly IJsonStorage _storage;
    private readonly IInvestmentsSerializer _serializer;
    private readonly Investments _investiments;

    public InvestmentJsonRepository(Investments investments, IJsonStorage storage, IInvestmentsSerializer serializer)
    {
        _investiments = investments ?? throw new ArgumentNullException(nameof(investments));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) =>
        GetPortfoliosByBroker(name, scope).SelectMany(p => p.Assets);

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

    public SyncStatus GetStatus() =>
        _storage is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.GetStatus()
            : new SyncStatus(SyncState.Idle, null, null);

    public Task FlushAsync() =>
        _storage is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.FlushAsync()
            : Task.CompletedTask;

    private IReadOnlyCollection<Broker> ResolveBrokers(InvestmentScope scope) =>
        scope == InvestmentScope.Historic ? _investiments.HistoricBrokers : _investiments.ActiveBrokers;

    private IEnumerable<Broker> GetBrokersByName(string brokerName, InvestmentScope scope) =>
        ResolveBrokers(scope).Where(b => b.Name == brokerName);

    private IEnumerable<Portfolio> GetPortfoliosByBroker(string brokerName, InvestmentScope scope) =>
        GetBrokersByName(brokerName, scope).SelectMany(b => b.Portfolios);
}
