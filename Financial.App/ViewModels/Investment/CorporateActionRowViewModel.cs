using System.Globalization;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// Display-ready wrapper around one <see cref="CorporateActionDTO"/> for the history grid -
/// TypeLabel/ResultingChange port Financial.Web's CorporateActionsTab.tsx logic verbatim.
/// Merger/SpinOff branches are placeholders ("—") until Stage 3/4 (P53-F06 PR3/PR4) add those types.
/// </summary>
public sealed class CorporateActionRowViewModel
{
    public CorporateActionRowViewModel(CorporateActionDTO record, string affectedAssetName)
    {
        Record = record ?? throw new ArgumentNullException(nameof(record));
        AffectedAssetName = record.LinkedAssetName ?? affectedAssetName;
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

    public string ResultingChange =>
        Record.Type == CorporateAction.CorporateActionType.Split && Record.RatioFactor.HasValue
            ? $"Quantity/average cost rescaled {FormatRatioFactor(Record.RatioFactor.Value)}"
            : "—";

    public bool IsSplit => Record.Type == CorporateAction.CorporateActionType.Split;

    private static string FormatRatioFactor(decimal ratioFactor)
    {
        var rounded = Math.Round(ratioFactor, 4, MidpointRounding.AwayFromZero);
        return $"×{rounded.ToString("0.####", CultureInfo.InvariantCulture)}";
    }
}
