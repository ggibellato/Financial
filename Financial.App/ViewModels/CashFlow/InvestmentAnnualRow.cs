namespace Financial.Presentation.App.ViewModels.CashFlow;

public sealed class InvestmentAnnualRow
{
    public required string Label { get; init; }
    public required decimal?[] MonthlyValues { get; init; }
    public bool IsEmphasized { get; init; }
}
