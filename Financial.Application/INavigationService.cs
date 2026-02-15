using Financial.Application.DTO;

namespace FinancialModel.Application;

/// <summary>
/// Service for navigating the financial data hierarchy (Investments → Brokers → Portfolios → Assets)
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the complete navigation tree structure starting from the root (Investments)
    /// </summary>
    /// <returns>Root node containing the entire broker/portfolio/asset hierarchy</returns>
    TreeNodeDTO GetNavigationTree();

    /// <summary>
    /// Gets detailed information for a specific asset including operations and credits
    /// </summary>
    /// <param name="brokerName">Name of the broker</param>
    /// <param name="portfolioName">Name of the portfolio</param>
    /// <param name="assetName">Name of the asset</param>
    /// <returns>Asset details with operations and credits, or null if not found</returns>
    AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName);

    /// <summary>
    /// Gets a list of all brokers
    /// </summary>
    /// <returns>Collection of broker nodes</returns>
    IEnumerable<BrokerNodeDTO> GetBrokers();
}
