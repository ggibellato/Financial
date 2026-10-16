using Financial.CashFlow.Domain.ValueObjects;

namespace Financial.CashFlow.Domain.Rules;

/// <summary>
/// Computes Resultado (R-D-Inv): salary after taxes, minus total expenses, plus the Investimento
/// category (which is already counted once inside total expenses, so it must be added back -
/// money moved into an investment account isn't consumption).
/// </summary>
public static class AnnualResultCalculator
{
    public static decimal ComputeResultado(decimal salaryAfterTaxes, decimal totalDespesas, decimal investimentoCategoryValue) =>
        salaryAfterTaxes - totalDespesas + investimentoCategoryValue;

    public static MonthlySeries ComputeResultado(MonthlySeries salaryAfterTaxes, MonthlySeries totalDespesas, MonthlySeries investimento) =>
        MonthlySeries.FromMonths(month =>
            ComputeResultado(salaryAfterTaxes[month - 1], totalDespesas[month - 1], investimento[month - 1]));
}
