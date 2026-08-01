namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// A single row of the Annual Summary page's Category Totals sub-tab. Used for both the fixed
/// income rows (Salary, Salary after taxes, Tax difference, Dividendo/Juros), the dynamic
/// per-category rows, spacer rows, and the emphasized Resultado/Total despesas rows — one flat
/// row shape mirrors AnnualSummaryPage.tsx's own flat JSX row sequence.
/// </summary>
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
