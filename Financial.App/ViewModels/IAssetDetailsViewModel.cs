using System.Threading.Tasks;
using Financial.Application.DTOs;

namespace Financial.Presentation.App.ViewModels;

public interface IAssetDetailsViewModel
{
    void LoadAssetDetails(AssetDetailsDTO details);
    void LoadBrokerCredits(string brokerName, IReadOnlyList<CreditDTO> credits);
    void LoadPortfolioCredits(string brokerName, string portfolioName, IReadOnlyList<CreditDTO> credits);
    void Clear();
    Task EnsureTodayInfoLoadedAsync();
}

