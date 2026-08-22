namespace Financial.Presentation.App.ViewModels.CashFlow;

public sealed class IncomeTotalRow
{
    public required string Source { get; init; }
    public decimal? GrossValue { get; init; }
    public required decimal NetValue { get; init; }
}
