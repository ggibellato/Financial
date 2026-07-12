using Financial.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Main ViewModel for the navigation view, coordinating the tree and detail panels
/// </summary>
public class MainNavigationViewModel : MainNavigationViewModelBase<AssetDetailsViewModel>
{
    public MainNavigationViewModel(
        INavigationService navigationService,
        ICreditQueryService creditQueryService,
        ISummaryService summaryService,
        IPortfolioAssetSummaryService portfolioAssetSummaryService,
        ITransactionService transactionService,
        ICreditService creditService,
        IAssetPriceService assetPriceService,
        IBrokerBreakdownService brokerBreakdownService,
        ITransactionQueryService transactionQueryService)
        : base(
            navigationService ?? throw new ArgumentNullException(nameof(navigationService)),
            creditQueryService ?? throw new ArgumentNullException(nameof(creditQueryService)),
            summaryService ?? throw new ArgumentNullException(nameof(summaryService)),
            portfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(portfolioAssetSummaryService)),
            new AssetDetailsViewModel(
                transactionService ?? throw new ArgumentNullException(nameof(transactionService)),
                creditService ?? throw new ArgumentNullException(nameof(creditService)),
                assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService)),
                brokerBreakdownService ?? throw new ArgumentNullException(nameof(brokerBreakdownService)),
                transactionQueryService ?? throw new ArgumentNullException(nameof(transactionQueryService))))
    {
    }
}
