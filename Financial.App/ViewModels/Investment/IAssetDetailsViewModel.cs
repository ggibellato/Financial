using Financial.Investment.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.Investment;

public interface IAssetDetailsViewModel
{
    bool IsPortfolioView { get; }
    bool IsBrokerView { get; }
    TransactionsTabViewModel Transactions { get; }
    CreditsTabViewModel Credits { get; }
    PriceHistoryTabViewModel PriceHistory { get; }
    void LoadAssetDetails(AssetDetailsDTO details, decimal? realizedPortfolioWeight = null);
    void LoadBrokerSummary(string brokerName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits);
    Task LoadBrokerBreakdown(string brokerName);
    void LoadPortfolioCredits(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits);
    void LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems);
    void Clear();
    Task EnsureTodayInfoLoadedAsync();
}

