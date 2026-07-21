using Financial.Application.Enums;
using Financial.Domain.Entities;

namespace Financial.Application.Interfaces;

public interface IRepository
{
    IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active);
    Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active);

    Task SaveChangesAsync();
}
