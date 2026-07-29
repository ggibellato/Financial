using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

/// <summary>
/// Groups an income source for reporting. Anything that isn't Salary (Gleison/Ariana) or
/// DividendoJuros is NonReportable - e.g. Lottery, which must never be folded into another
/// group's total.
/// </summary>
public static class IncomeClassifier
{
    public static IncomeGroup Classify(IncomeSource incomeSource) => incomeSource switch
    {
        IncomeSource.Gleison or IncomeSource.Ariana => IncomeGroup.Salary,
        IncomeSource.DividendoJuros => IncomeGroup.DividendoJuros,
        _ => IncomeGroup.NonReportable
    };
}
