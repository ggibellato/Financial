using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Interfaces;

public interface IRepository
{
    IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active);
    Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active);

    Task SaveChangesAsync();
}
