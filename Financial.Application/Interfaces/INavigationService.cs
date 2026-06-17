using Financial.Application.DTOs;
using System.Collections.Generic;

namespace Financial.Application.Interfaces;

public interface INavigationService
{
    TreeNodeDTO GetNavigationTree();
    AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName);
    IEnumerable<BrokerNodeDTO> GetBrokers();
}
