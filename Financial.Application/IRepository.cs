using Financial.Model;

namespace FinancialModel.Application;

public interface IRepository
{
    List<string> GetAllAssetsFullName();
    IEnumerable<Asset> GetAssetsByBroker(string name);
    IEnumerable<Asset> GetAssetsByBrokerPortifolio(string broker, string protifolio);
    IEnumerable<Asset> GetAssetsByPortifolio(string name);
    IEnumerable<Asset> GetAssetsByAssetName(string name);
    IEnumerable<Broker> GetBrokerList();
    decimal GetTotalBoughtByBroker(string brokerName);
    decimal GetTotalSoldByBroker(string brokerName);
    decimal GetTotalCreditsByBroker(string brokerName);
}
