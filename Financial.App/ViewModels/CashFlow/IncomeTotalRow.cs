namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>One row of the Income grid: a source's aggregated gross/net totals for the month, mirroring useMonthly.ts's IncomeTotal.</summary>
public sealed class IncomeTotalRow
{
    public required string Source { get; init; }
    public decimal? GrossValue { get; init; }
    public required decimal NetValue { get; init; }
}
