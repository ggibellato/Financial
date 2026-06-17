using System;
using System.Threading.Tasks;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;

namespace Financial.Presentation.App.ViewModels;

public sealed class CreditActions : AssetActionsBase
{
    private readonly ICreditService? _service;

    public CreditActions(
        ICreditService? service,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
        : base(hasContext, brokerName, portfolioName, assetName, applyDetails, showMessage, "Credit")
    {
        _service = service;
    }

    public async Task Add(Func<CreditDialogData?> showDialog)
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

        var updatedDetails = await _service.AddCreditAsync(new CreditCreateDTO
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

    public async Task Update(CreditDTO? selectedCredit, Func<CreditDialogData?> showDialog)
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

        var updatedDetails = await _service.UpdateCreditAsync(new CreditUpdateDTO
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

    public async Task Delete(CreditDTO? selectedCredit, Func<bool> confirmDialog)
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

        var updatedDetails = await _service.DeleteCreditAsync(new CreditDeleteDTO
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

}

public readonly record struct CreditDialogData(
    Guid CreditId,
    DateTime Date,
    string Type,
    decimal Value);
