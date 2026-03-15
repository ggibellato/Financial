using System;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;

namespace Financial.Presentation.Shared.ViewModels;

public sealed class OperationActions
{
    private readonly IOperationService? _service;
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;

    public OperationActions(
        IOperationService? service,
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

    public void Add(Func<OperationDialogData?> showDialog)
    {
        if (!_hasContext())
        {
            ShowInfo("Select an asset before adding an operation.");
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

        if (!OperationTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Operation type must be 'Buy' or 'Sell'.");
            return;
        }

        var updatedDetails = _service.AddOperation(new OperationCreateDTO
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
            ShowWarning("Operation could not be added. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public void Update(OperationDTO? selectedOperation, Func<OperationDialogData?> showDialog)
    {
        if (_service == null || selectedOperation == null)
        {
            return;
        }

        if (selectedOperation.Id == Guid.Empty)
        {
            ShowWarning("Select a saved operation to update.");
            return;
        }

        var dialogData = showDialog();
        if (dialogData == null)
        {
            return;
        }

        if (!OperationTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Operation type must be 'Buy' or 'Sell'.");
            return;
        }

        var updatedDetails = _service.UpdateOperation(new OperationUpdateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = dialogData.Value.OperationId,
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Quantity = dialogData.Value.Quantity,
            UnitPrice = dialogData.Value.UnitPrice,
            Fees = dialogData.Value.Fees
        });

        if (updatedDetails == null)
        {
            ShowWarning("Operation could not be updated. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public void Delete(OperationDTO? selectedOperation, Func<bool> confirmDialog)
    {
        if (selectedOperation == null)
        {
            return;
        }

        if (_service == null)
        {
            return;
        }

        if (selectedOperation.Id == Guid.Empty)
        {
            ShowWarning("Select a saved operation to delete.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        var updatedDetails = _service.DeleteOperation(new OperationDeleteDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = selectedOperation.Id
        });

        if (updatedDetails == null)
        {
            ShowWarning("Operation could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    private void ShowInfo(string message)
    {
        _showMessage(message, "Operation", MessageBoxImage.Information);
    }

    private void ShowWarning(string message)
    {
        _showMessage(message, "Operation", MessageBoxImage.Warning);
    }
}

public readonly record struct OperationDialogData(
    Guid OperationId,
    DateTime Date,
    string Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees);
