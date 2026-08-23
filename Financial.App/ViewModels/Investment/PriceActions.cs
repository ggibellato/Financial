using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public sealed class PriceActions : AssetActionsBase
{
    private readonly IPriceService? _service;

    public PriceActions(
        IPriceService? service,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
        : base(hasContext, brokerName, portfolioName, assetName, applyDetails, showMessage, "Price")
    {
        _service = service;
    }

    public async Task Set(Func<Task<PriceDialogData?>> showForm)
    {
        if (!HasContext())
        {
            ShowInfo("Select an asset before setting a price.");
            return;
        }

        if (_service == null)
        {
            return;
        }

        var dialogData = await showForm();
        if (dialogData == null)
        {
            return;
        }

        var updatedDetails = await _service.SetPriceAsync(new SetAssetPriceDTO
        {
            BrokerName = GetBrokerName(),
            PortfolioName = GetPortfolioName(),
            AssetName = GetAssetName(),
            Date = dialogData.Value.Date,
            Price = dialogData.Value.Price
        });

        if (updatedDetails == null)
        {
            ShowWarning("Price could not be saved. Check the values and try again.");
            return;
        }

        ApplyDetails(updatedDetails);
    }

    public async Task Delete(AssetPriceSnapshotDTO? selectedEntry, Func<bool> confirmDialog)
    {
        if (selectedEntry == null)
        {
            return;
        }

        if (_service == null)
        {
            return;
        }

        if (!selectedEntry.IsManual)
        {
            ShowWarning("Only manually-entered prices can be deleted.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        var updatedDetails = await _service.DeletePriceAsync(new DeleteAssetPriceDTO
        {
            BrokerName = GetBrokerName(),
            PortfolioName = GetPortfolioName(),
            AssetName = GetAssetName(),
            Date = selectedEntry.Date
        });

        if (updatedDetails == null)
        {
            ShowWarning("Price could not be deleted. Check the values and try again.");
            return;
        }

        ApplyDetails(updatedDetails);
    }
}

public readonly record struct PriceDialogData(DateOnly Date, decimal Price);
