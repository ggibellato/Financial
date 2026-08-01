namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// A single row of the Annual Summary page's Investments sub-tab: one per account (label
/// suffixed " (-)" for liabilities), plus a Total row and a Month Result row (both from
/// NetPosition). No Average/Annual Total columns — unlike AnnualSummaryRow. Cells are
/// nullable since the first month's diff can be null (no prior-year data).
/// </summary>
public sealed class InvestmentAnnualRow
{
    public required string Label { get; init; }
    public required decimal?[] MonthlyValues { get; init; }
    public bool IsEmphasized { get; init; }
}
