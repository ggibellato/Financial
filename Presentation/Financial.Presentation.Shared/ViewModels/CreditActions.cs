using System;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;

namespace Financial.Presentation.Shared.ViewModels;

public sealed class CreditActions
{
    private readonly ICreditService? _service;
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;

    public CreditActions(
        ICreditService? service,
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

    public void Add(Func<CreditDialogData?> showDialog)
    {
        if (!_hasContext())
        {
            ShowInfo("Select an asset before adding a credit.");
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

        if (!CreditTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Credit type must be 'Dividend' or 'Rent'.");
            return;
        }

        var updatedDetails = _service.AddCredit(new CreditCreateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Value = dialogData.Value.Value
        });

        if (updatedDetails == null)
        {
            ShowWarning("Credit could not be added. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public void Update(CreditDTO? selectedCredit, Func<CreditDialogData?> showDialog)
    {
        if (_service == null || selectedCredit == null)
        {
            return;
        }

        if (selectedCredit.Id == Guid.Empty)
        {
            ShowWarning("Select a saved credit to update.");
            return;
        }

        var dialogData = showDialog();
        if (dialogData == null)
        {
            return;
        }

        if (!CreditTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Credit type must be 'Dividend' or 'Rent'.");
            return;
        }

        var updatedDetails = _service.UpdateCredit(new CreditUpdateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = dialogData.Value.CreditId,
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Value = dialogData.Value.Value
        });

        if (updatedDetails == null)
        {
            ShowWarning("Credit could not be updated. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public void Delete(CreditDTO? selectedCredit, Func<bool> confirmDialog)
    {
        if (selectedCredit == null)
        {
            return;
        }

        if (_service == null)
        {
            return;
        }

        if (selectedCredit.Id == Guid.Empty)
        {
            ShowWarning("Select a saved credit to delete.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        var updatedDetails = _service.DeleteCredit(new CreditDeleteDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = selectedCredit.Id
        });

        if (updatedDetails == null)
        {
            ShowWarning("Credit could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    private void ShowInfo(string message)
    {
        _showMessage(message, "Credit", MessageBoxImage.Information);
    }

    private void ShowWarning(string message)
    {
        _showMessage(message, "Credit", MessageBoxImage.Warning);
    }
}

public readonly record struct CreditDialogData(
    Guid CreditId,
    DateTime Date,
    string Type,
    decimal Value);
