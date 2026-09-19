using System.Windows.Media;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Wpf.Ui.Controls;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// Display-ready wrapper around one <see cref="TaxWorkbookEntryDTO"/>. Status colors are the literal
/// values Financial.Web's Fluent Badge renders for color="success"/"informative"/"warning"/"danger"
/// (resolved from @fluentui/react-theme's webLightTheme and the Badge component's own filled-* style
/// map, not a screenshot approximation), per docs/ui/decisions/ADR-005's "match the rendered pixel"
/// guidance - the same approach <see cref="Financial.Presentation.App.ViewModels.PaymentDueRowViewModel"/>
/// already established for its own status tiers.
/// </summary>
public sealed class TaxWorkbookEntryRowViewModel
{
    private static readonly SolidColorBrush FinalBackground = Freeze(0x10, 0x7C, 0x10);
    private static readonly SolidColorBrush FinalForeground = Freeze(0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush DefaultBackground = Freeze(0xEB, 0xEB, 0xEB);
    private static readonly SolidColorBrush DefaultForeground = Freeze(0x61, 0x61, 0x61);
    private static readonly SolidColorBrush IncompleteBackground = Freeze(0xFD, 0xE3, 0x00);
    private static readonly SolidColorBrush IncompleteForeground = Freeze(0x24, 0x24, 0x24);
    private static readonly SolidColorBrush RequiresReviewBackground = Freeze(0xD1, 0x34, 0x38);
    private static readonly SolidColorBrush RequiresReviewForeground = Freeze(0xFF, 0xFF, 0xFF);

    public TaxWorkbookEntryRowViewModel(TaxWorkbookEntryDTO entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Date = entry.Date;
        EventCategory = entry.EventCategory;
        Proceeds = entry.Proceeds;
        CostBasis = entry.CostBasis;
        GainLoss = entry.GainLoss;
        GrossAmount = entry.GrossAmount;
        WithheldAmount = entry.WithheldAmount;
        NetAmount = entry.NetAmount;
        EvidenceReference = entry.EvidenceReference;

        var (background, foreground, symbol, filled, label) = ResolveStatus(entry.CalculationStatus);
        StatusBrush = background;
        StatusForeground = foreground;
        StatusSymbol = symbol;
        StatusSymbolFilled = filled;
        StatusLabel = label;
        StatusAccessibleLabel = label;
    }

    public DateTime Date { get; }
    public EventCategory EventCategory { get; }
    public decimal? Proceeds { get; }
    public decimal? CostBasis { get; }
    public decimal? GainLoss { get; }
    public decimal? GrossAmount { get; }
    public decimal? WithheldAmount { get; }
    public decimal? NetAmount { get; }
    public Guid EvidenceReference { get; }
    public SolidColorBrush StatusBrush { get; }
    public SolidColorBrush StatusForeground { get; }
    public SymbolRegular StatusSymbol { get; }
    public bool StatusSymbolFilled { get; }
    public string StatusLabel { get; }
    public string StatusAccessibleLabel { get; }

    public static (SolidColorBrush Background, SolidColorBrush Foreground, SymbolRegular Symbol, bool Filled, string Label) ResolveStatus(
        CalculationStatus status) => status switch
        {
            CalculationStatus.Final => (FinalBackground, FinalForeground, SymbolRegular.CheckmarkCircle20, false, "Final"),
            CalculationStatus.Incomplete => (IncompleteBackground, IncompleteForeground, SymbolRegular.Clock20, false, "Incomplete"),
            CalculationStatus.RequiresReview => (RequiresReviewBackground, RequiresReviewForeground, SymbolRegular.AlertUrgent20, true, "Requires review"),
            _ => (DefaultBackground, DefaultForeground, SymbolRegular.Info20, false, status.ToString()),
        };

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
