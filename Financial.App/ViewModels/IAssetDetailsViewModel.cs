using Financial.Application.DTOs;

namespace Financial.Presentation.App.ViewModels;

public interface IAssetDetailsViewModel
{
    void LoadAssetDetails(AssetDetailsDTO details);
    void LoadBrokerCredits(string brokerName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits);
    void LoadPortfolioCredits(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits);
    void Clear();
    Task EnsureTodayInfoLoadedAsync();
    void UpdateCreditsPlotWidth(double plotWidth);
}

