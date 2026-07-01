using Financial.Domain.Entities;

namespace Financial.Application.Interfaces;

public interface IRepository
{
    IEnumerable<Asset> GetAssetsByBroker(string name);
    IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio);
    IEnumerable<Broker> GetBrokerList();
    Asset? GetAsset(string brokerName, string portfolioName, string assetName);

    Task SaveChangesAsync();
}
