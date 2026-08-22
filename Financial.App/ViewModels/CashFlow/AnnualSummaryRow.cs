namespace Financial.Presentation.App.ViewModels.CashFlow;

public sealed class AnnualSummaryRow
{
    public required string Label { get; init; }
    public required decimal[] MonthlyValues { get; init; }
    public required decimal Average { get; init; }
    public required decimal AnnualTotal { get; init; }
    public bool IsSpacer { get; init; }
    public bool IsEmphasized { get; init; }

    public static AnnualSummaryRow Spacer() => new()
    {
        Label = string.Empty, MonthlyValues = new decimal[12], Average = 0m, AnnualTotal = 0m, IsSpacer = true,
    };
}
