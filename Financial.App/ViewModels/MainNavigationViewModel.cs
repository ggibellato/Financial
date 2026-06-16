using System;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Main ViewModel for the navigation view, coordinating the tree and detail panels
/// </summary>
public class MainNavigationViewModel : MainNavigationViewModelBase<AssetDetailsViewModel>
{
    public MainNavigationViewModel(INavigationService navigationService, ITransactionService transactionService, ICreditService creditService, IAssetPriceService assetPriceService)
        : base(
            navigationService ?? throw new ArgumentNullException(nameof(navigationService)),
            new AssetDetailsViewModel(
                transactionService ?? throw new ArgumentNullException(nameof(transactionService)),
                creditService ?? throw new ArgumentNullException(nameof(creditService)),
                assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService))))
    {
    }
}




