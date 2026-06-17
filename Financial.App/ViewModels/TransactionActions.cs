using System;
using System.Threading.Tasks;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;

namespace Financial.Presentation.App.ViewModels;

public sealed class TransactionActions : AssetActionsBase
{
    private readonly ITransactionService? _service;

    public TransactionActions(
        ITransactionService? service,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
        : base(hasContext, brokerName, portfolioName, assetName, applyDetails, showMessage, "Transaction")
    {
        _service = service;
    }

    public async Task Add(Func<TransactionDialogData?> showDialog)
    {
        if (!HasContext())
        {
            ShowInfo("Select an asset before adding a transaction.");
            return;
        }

        if (_service == null)
        {
            return;
        }

        var dialogData = showDialog();
        if (dialogData == null)
        {
            return;
        }

        if (!TransactionTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Transaction type must be 'Buy' or 'Sell'.");
            return;
        }

        var updatedDetails = await _service.AddTransactionAsync(new TransactionCreateDTO
        {
            BrokerName = GetBrokerName(),
            PortfolioName = GetPortfolioName(),
            AssetName = GetAssetName(),
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Quantity = dialogData.Value.Quantity,
            UnitPrice = dialogData.Value.UnitPrice,
            Fees = dialogData.Value.Fees
        });

        if (updatedDetails == null)
        {
            ShowWarning("Transaction could not be added. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public async Task Update(TransactionDTO? selectedTransaction, Func<TransactionDialogData?> showDialog)
    {
        if (_service == null || selectedTransaction == null)
        {
            return;
        }

        if (selectedTransaction.Id == Guid.Empty)
        {
            ShowWarning("Select a saved transaction to update.");
            return;
        }

        var dialogData = showDialog();
        if (dialogData == null)
        {
            return;
        }

        if (!TransactionTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Transaction type must be 'Buy' or 'Sell'.");
            return;
        }

        var updatedDetails = await _service.UpdateTransactionAsync(new TransactionUpdateDTO
        {
            BrokerName = GetBrokerName(),
            PortfolioName = GetPortfolioName(),
            AssetName = GetAssetName(),
            Id = dialogData.Value.TransactionId,
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Quantity = dialogData.Value.Quantity,
            UnitPrice = dialogData.Value.UnitPrice,
            Fees = dialogData.Value.Fees
        });

        if (updatedDetails == null)
        {
            ShowWarning("Transaction could not be updated. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public async Task Delete(TransactionDTO? selectedTransaction, Func<bool> confirmDialog)
    {
        if (selectedTransaction == null)
        {
            return;
        }

        if (_service == null)
        {
            return;
        }

        if (selectedTransaction.Id == Guid.Empty)
        {
            ShowWarning("Select a saved transaction to delete.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        var updatedDetails = await _service.DeleteTransactionAsync(new TransactionDeleteDTO
        {
            BrokerName = GetBrokerName(),
            PortfolioName = GetPortfolioName(),
            AssetName = GetAssetName(),
            Id = selectedTransaction.Id
        });

        if (updatedDetails == null)
        {
            ShowWarning("Transaction could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

}

public readonly record struct TransactionDialogData(
    Guid TransactionId,
    DateTime Date,
    string Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees);
