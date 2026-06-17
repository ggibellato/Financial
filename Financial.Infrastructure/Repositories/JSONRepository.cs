using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public IReadOnlyList<string> GetAllAssetsFullName()
    {
        return _investiments.Brokers
            .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Select(a => $"{b.Name}/{p.Name}/{a.Name}")))
            .ToList();
    }

    public IEnumerable<Asset> GetAssetsByBroker(string name)
    {
        return GetAssetsByBrokerInternal(name);
    }

    public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio)
    {
        return GetPortfoliosByBroker(broker)
            .Where(p => p.Name == portfolio)
            .SelectMany(p => p.Assets);
    }

    public IEnumerable<Asset> GetAssetsByPortfolio(string name)
    {
        return GetPortfoliosByName(name)
            .SelectMany(p => p.Assets);
    }

    public IEnumerable<Asset> GetAssetsByAssetName(string name)
    {
        return GetAllAssetsInternal()
            .Where(a => a.Name == name);
    }

    public Asset? GetAsset(string brokerName, string portfolioName, string assetName)
    {
        return GetAssetsByBrokerPortfolio(brokerName, portfolioName)
            .FirstOrDefault(a => a.Name == assetName);
    }

    public IEnumerable<Broker> GetBrokerList()
    {
        return _investiments.Brokers;
    }

    public async Task SaveChangesAsync()
    {
        var json = _serializer.Serialize(_investiments);
        await _storage.WriteAsync(json).ConfigureAwait(false);
    }

    private IEnumerable<Broker> GetBrokersByName(string brokerName) =>
        _investiments.Brokers.Where(b => b.Name == brokerName);

    private IEnumerable<Portfolio> GetPortfoliosByBroker(string brokerName) =>
        GetBrokersByName(brokerName).SelectMany(b => b.Portfolios);

    private IEnumerable<Asset> GetAssetsByBrokerInternal(string brokerName) =>
        GetPortfoliosByBroker(brokerName).SelectMany(p => p.Assets);

    private IEnumerable<Portfolio> GetPortfoliosByName(string portfolioName) =>
        _investiments.Brokers.SelectMany(b => b.Portfolios.Where(p => p.Name == portfolioName));

    private IEnumerable<Asset> GetAllAssetsInternal() =>
        _investiments.Brokers.SelectMany(b => b.Portfolios.SelectMany(p => p.Assets));
}
