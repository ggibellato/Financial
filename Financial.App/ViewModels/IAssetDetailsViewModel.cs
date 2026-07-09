using Financial.Application.DTOs;

namespace Financial.Presentation.App.ViewModels;

public interface IAssetDetailsViewModel
{
    bool IsPortfolioView { get; }
    bool IsBrokerView { get; }
    void LoadAssetDetails(AssetDetailsDTO details);
    void LoadBrokerSummary(string brokerName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits);
    Task LoadBrokerBreakdown(string brokerName);
    void LoadPortfolioCredits(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits);
    void LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems);
    void Clear();
    Task EnsureTodayInfoLoadedAsync();
    void UpdateCreditsPlotWidth(double plotWidth);
}

