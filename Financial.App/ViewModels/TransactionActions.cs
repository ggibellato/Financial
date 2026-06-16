using System;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;

namespace Financial.Presentation.App.ViewModels;

public sealed class TransactionActions
{
    private readonly ITransactionService? _service;
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;

    public TransactionActions(
        ITransactionService? service,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
    {
        _service = service;
        _hasContext = hasContext ?? throw new ArgumentNullException(nameof(hasContext));
        _brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _portfolioName = portfolioName ?? throw new ArgumentNullException(nameof(portfolioName));
        _assetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
        _applyDetails = applyDetails ?? throw new ArgumentNullException(nameof(applyDetails));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
    }

    public void Add(Func<TransactionDialogData?> showDialog)
    {
        if (!_hasContext())
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

        var updatedDetails = _service.AddTransaction(new TransactionCreateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
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

    public void Update(TransactionDTO? selectedTransaction, Func<TransactionDialogData?> showDialog)
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

        var updatedDetails = _service.UpdateTransaction(new TransactionUpdateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
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

    public void Delete(TransactionDTO? selectedTransaction, Func<bool> confirmDialog)
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

        var updatedDetails = _service.DeleteTransaction(new TransactionDeleteDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = selectedTransaction.Id
        });

        if (updatedDetails == null)
        {
            ShowWarning("Transaction could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    private void ShowInfo(string message)
    {
        _showMessage(message, "Transaction", MessageBoxImage.Information);
    }

    private void ShowWarning(string message)
    {
        _showMessage(message, "Transaction", MessageBoxImage.Warning);
    }
}

public readonly record struct TransactionDialogData(
    Guid TransactionId,
    DateTime Date,
    string Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees);
