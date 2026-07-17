using Financial.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Navigation ViewModel for the Historic Investments tab, mirroring MainNavigationViewModel
/// but scoped to InvestmentScope.Historic throughout.
/// </summary>
public class MainNavigationViewModelHistoric : MainNavigationViewModelBase<AssetDetailsViewModel>
{
    public MainNavigationViewModelHistoric(
        INavigationService navigationService,
        ICreditQueryService creditQueryService,
        ISummaryService summaryService,
        IPortfolioAssetSummaryService portfolioAssetSummaryService,
        ITransactionService transactionService,
        ICreditService creditService,
        IAssetPriceService assetPriceService,
        IBrokerBreakdownService brokerBreakdownService,
        ITransactionQueryService transactionQueryService,
        IXirrCalculationService xirrCalculationService)
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
                transactionQueryService ?? throw new ArgumentNullException(nameof(transactionQueryService)),
                xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService)),
                InvestmentScope.Historic),
            InvestmentScope.Historic)
    {
    }
}
