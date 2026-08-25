using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.Investment;

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
        ITransactionQueryService transactionQueryService,
        IXirrCalculationService xirrCalculationService,
        IProfitCalculationService profitCalculationService,
        IAssetPriceLookupService priceLookupService,
        IAssetPriceCrudService priceCrudService,
        IAssetMoveService assetMoveService,
        IPortfolioService portfolioService)
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
                profitCalculationService ?? throw new ArgumentNullException(nameof(profitCalculationService)),
                priceLookupService: priceLookupService ?? throw new ArgumentNullException(nameof(priceLookupService)),
                priceCrudService: priceCrudService ?? throw new ArgumentNullException(nameof(priceCrudService))),
            InvestmentScope.Active,
            assetMoveService ?? throw new ArgumentNullException(nameof(assetMoveService)),
            portfolioService ?? throw new ArgumentNullException(nameof(portfolioService)))
    {
    }
}
