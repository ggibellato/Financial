using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

public interface INavigationService
{
    TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active);
    AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<BrokerNodeDTO> GetBrokers(InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName);
}
