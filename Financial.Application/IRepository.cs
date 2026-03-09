using Financial.Model;
using Financial.Application.DTO;

namespace FinancialModel.Application;

public interface IRepository
{
    List<string> GetAllAssetsFullName();
    IEnumerable<Asset> GetAssetsByBroker(string name);
    IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio);
    IEnumerable<Asset> GetAssetsByPortfolio(string name);
    IEnumerable<Asset> GetAssetsByAssetName(string name);
    IEnumerable<Broker> GetBrokerList();

    BrokerInfoDTO GetBrokerInfo(string brokerName);
    AssetInfoDTO GetAssetInfo(string brokerName, string portfolio, string assetName);
}
