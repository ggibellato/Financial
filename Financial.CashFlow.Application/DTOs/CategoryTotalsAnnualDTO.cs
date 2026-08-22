namespace Financial.CashFlow.Application.DTOs;

public sealed class CategoryTotalsAnnualDTO
{
    public required IReadOnlyList<CategoryAnnualTotalDTO> CategoryTotals { get; init; }
    public required IncomeAnnualSummaryDTO IncomeSummary { get; init; }

    /// <summary>Sum of all category rows' MonthlyTotals, per month.</summary>
    public required decimal[] TotalDespesasMonthly { get; init; }
    public required decimal TotalDespesasAnnualTotal { get; init; }
    public required decimal TotalDespesasAverage { get; init; }

    /// <summary>Salary after taxes minus Total despesas plus the Investimento category value, per month (no Dividendo/Juros term).</summary>
    public required decimal[] ResultadoMonthly { get; init; }
    public required decimal ResultadoAnnualTotal { get; init; }
    public required decimal ResultadoAverage { get; init; }
}
