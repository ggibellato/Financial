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

    /// <summary>Serializing the document walks every collection in the graph, so one writer at a
    /// time. A semaphore rather than a lock because the critical section awaits the storage.</summary>
    private readonly SemaphoreSlim _writeGate = new(1, 1);

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

    public Investments GetInvestments() => _investiments;

    public async Task<bool> ApplyAndSaveAsync(Func<bool> applyChanges)
    {
        ArgumentNullException.ThrowIfNull(applyChanges);

        await _writeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!applyChanges())
            {
                return false;
            }

            // The write stays inside the gate. Serializing here but writing outside would let a
            // thread holding an older document reach the file after one holding a newer document,
            // silently discarding the newer change with no error anywhere.
            var json = _serializer.Serialize(_investiments);
            await _storage.WriteAsync(json).ConfigureAwait(false);
            return true;
        }
        finally
        {
            // Never conditional: skipping this on a storage failure would leave the singleton
            // repository permanently locked, hanging every later save instead of throwing.
            _writeGate.Release();
        }
    }

    public SyncStatus GetStatus() => _storage.GetStatusOrIdle();

    public Task FlushAsync() => _storage.FlushIfSupportedAsync();

    private IReadOnlyCollection<Broker> ResolveBrokers(InvestmentScope scope) =>
        scope == InvestmentScope.Historic ? _investiments.HistoricBrokers : _investiments.ActiveBrokers;

    private IEnumerable<Broker> GetBrokersByName(string brokerName, InvestmentScope scope) =>
        ResolveBrokers(scope).Where(b => b.Name == brokerName);

    private IEnumerable<Portfolio> GetPortfoliosByBroker(string brokerName, InvestmentScope scope) =>
        GetBrokersByName(brokerName, scope).SelectMany(b => b.Portfolios);
}
