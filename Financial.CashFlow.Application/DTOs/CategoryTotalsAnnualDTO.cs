namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Combined read model for the Annual Summary page's Category Totals tab: category totals,
/// income summary, and the server-computed Total despesas / Resultado (R-D-Inv) figures.
/// </summary>
public sealed class CategoryTotalsAnnualDTO
{
    public required IReadOnlyList<CategoryAnnualTotalDTO> CategoryTotals { get; init; }
    public required IncomeAnnualSummaryDTO IncomeSummary { get; init; }

    /// <summary>Sum of all category rows' MonthlyTotals, per month.</summary>
    public required decimal[] TotalDespesasMonthly { get; init; }
    public required decimal TotalDespesasAnnualTotal { get; init; }

    /// <summary>Salary after taxes minus Total despesas plus the Investimento category value, per month (no Dividendo/Juros term).</summary>
    public required decimal[] ResultadoMonthly { get; init; }
    public required decimal ResultadoAnnualTotal { get; init; }
}
