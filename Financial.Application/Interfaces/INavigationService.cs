using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface INavigationService
{
    TreeNodeDTO GetNavigationTree();
    AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName);
    IEnumerable<BrokerNodeDTO> GetBrokers();
    IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName);
}
