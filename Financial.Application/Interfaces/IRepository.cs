using Financial.Domain.Entities;

namespace Financial.Application.Interfaces;

public interface IRepository
{
    List<string> GetAllAssetsFullName();
    IEnumerable<Asset> GetAssetsByBroker(string name);
    IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio);
    IEnumerable<Asset> GetAssetsByPortfolio(string name);
    IEnumerable<Asset> GetAssetsByAssetName(string name);
    IEnumerable<Broker> GetBrokerList();
    Asset? GetAsset(string brokerName, string portfolioName, string assetName);

    void SaveChanges();
}
