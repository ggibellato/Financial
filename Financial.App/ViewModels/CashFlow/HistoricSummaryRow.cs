namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// A single row of the Annual Summary page's Historic Summary Average sub-tab: one per
/// category, with one value per available year. Columns are built dynamically in the View's
/// code-behind from the ViewModel's AvailableYears list, bound via ValuesByYear[year].
/// </summary>
public sealed class HistoricSummaryRow
{
    private static readonly HashSet<string> SpacerAfterCategories = ["Tax difference", "Dividendo/Juros", "Reserva"];
    private static readonly HashSet<string> EmphasizedCategories = ["Resultado (R-D-Inv)", "Total despesas"];

    public required string Category { get; init; }
    public required Dictionary<int, decimal> ValuesByYear { get; init; }
    public bool IsSpacer { get; init; }
    public bool IsEmphasized { get; init; }

    public static bool HasSpacerAfter(string category) => SpacerAfterCategories.Contains(category);

    public static bool IsEmphasizedCategory(string category) => EmphasizedCategories.Contains(category);

    public static HistoricSummaryRow Spacer() => new()
    {
        Category = string.Empty, ValuesByYear = [], IsSpacer = true,
    };
}
