using Financial.Application.DTOs;
using Financial.Application.Enums;

namespace Financial.Application.Interfaces;

public interface INavigationService
{
    TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active);
    AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<BrokerNodeDTO> GetBrokers(InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName);
}
