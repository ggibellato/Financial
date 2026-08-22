namespace Financial.Presentation.App.ViewModels.CashFlow;

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
