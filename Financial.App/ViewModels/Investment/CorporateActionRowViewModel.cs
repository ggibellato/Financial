using System.Globalization;
using System.Windows.Media;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Wpf.Ui.Controls;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// Display-ready wrapper around one <see cref="CorporateActionDTO"/> for the history grid -
/// TypeLabel/ResultingChange port Financial.Web's CorporateActionsTab.tsx logic verbatim.
/// </summary>
public sealed class CorporateActionRowViewModel
{
    public CorporateActionRowViewModel(CorporateActionDTO record, string affectedAssetName)
    {
        Record = record ?? throw new ArgumentNullException(nameof(record));
        AffectedAssetName = record.LinkedAssetName ?? affectedAssetName;

        HasCalculationStatus = Record.CalculationStatus.HasValue;
        if (HasCalculationStatus)
        {
            var (background, foreground, symbol, filled, label) = TaxWorkbookEntryRowViewModel.ResolveStatus(Record.CalculationStatus!.Value);
            StatusBrush = background;
            StatusForeground = foreground;
            StatusSymbol = symbol;
            StatusSymbolFilled = filled;
            StatusLabel = label;
            StatusAccessibleLabel = label;
        }
    }

    public CorporateActionDTO Record { get; }
    public Guid Id => Record.Id;
    public DateTime EffectiveDate => Record.EffectiveDate;
    public string? Note => Record.Note;
    public string AffectedAssetName { get; }

    public string TypeLabel => Record.Type switch
    {
        CorporateAction.CorporateActionType.Split => "Split",
        CorporateAction.CorporateActionType.Merger => "Merger",
        CorporateAction.CorporateActionType.SpinOff => "Spin-off",
        _ => Record.Type.ToString()
    };

    public string ResultingChange => Record.Type switch
    {
        CorporateAction.CorporateActionType.Split when Record.RatioFactor.HasValue =>
            $"Quantity/average cost rescaled {FormatRatioFactor(Record.RatioFactor.Value)}",
        CorporateAction.CorporateActionType.Merger when Record.Role == CorporateAction.CorporateActionRole.Source =>
            "Position closed and converted",
        CorporateAction.CorporateActionType.Merger when Record.Role == CorporateAction.CorporateActionRole.Target =>
            Record.ConvertedQuantity.HasValue
                ? $"+{Record.ConvertedQuantity.Value.ToString("N2", CultureInfo.InvariantCulture)} units received"
                : "Units received from merger",
        CorporateAction.CorporateActionType.SpinOff when Record.Role == CorporateAction.CorporateActionRole.Parent =>
            "Cost basis reduced by spin-off",
        CorporateAction.CorporateActionType.SpinOff when Record.Role == CorporateAction.CorporateActionRole.New =>
            Record.ConvertedQuantity.HasValue
                ? $"+{Record.ConvertedQuantity.Value.ToString("N2", CultureInfo.InvariantCulture)} units received"
                : "Units received from spin-off",
        _ => "—"
    };

    public bool HasCalculationStatus { get; }
    public bool HasNoCalculationStatus => !HasCalculationStatus;
    public SolidColorBrush? StatusBrush { get; }
    public SolidColorBrush? StatusForeground { get; }
    public SymbolRegular StatusSymbol { get; }
    public bool StatusSymbolFilled { get; }
    public string StatusLabel { get; } = string.Empty;
    public string StatusAccessibleLabel { get; } = string.Empty;

    private static string FormatRatioFactor(decimal ratioFactor)
    {
        var rounded = Math.Round(ratioFactor, 4, MidpointRounding.AwayFromZero);
        return $"×{rounded.ToString("0.####", CultureInfo.InvariantCulture)}";
    }
}
