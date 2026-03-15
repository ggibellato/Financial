using System;
using Financial.Application.Interfaces;
using Financial.Presentation.Shared.ViewModels;

namespace Financial.Presentation.Tools.ViewModels;

/// <summary>
/// Main ViewModel for the navigation view, coordinating the tree and detail panels
/// </summary>
public class MainNavigationViewModel : MainNavigationViewModelBase<AssetDetailsViewModel>
{
    public MainNavigationViewModel(INavigationService navigationService, IOperationService operationService, ICreditService creditService, IAssetPriceService assetPriceService)
        : base(
            navigationService ?? throw new ArgumentNullException(nameof(navigationService)),
            new AssetDetailsViewModel(
                operationService ?? throw new ArgumentNullException(nameof(operationService)),
                creditService ?? throw new ArgumentNullException(nameof(creditService)),
                assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService))))
    {
    }
}


