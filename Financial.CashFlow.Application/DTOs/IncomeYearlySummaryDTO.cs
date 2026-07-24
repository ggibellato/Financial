namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for the Yearly Summary page's Income Summary table.
/// </summary>
public sealed class IncomeYearlySummaryDTO
{
    /// <summary>Row 2 "Salary": sum of Gleison + Ariana gross values, per month.</summary>
    public required decimal[] SalaryMonthly { get; init; }
    public required decimal SalaryYearlyTotal { get; init; }

    /// <summary>Row 3 "Salary after taxes": sum of Gleison + Ariana net values, per month.</summary>
    public required decimal[] SalaryAfterTaxesMonthly { get; init; }
    public required decimal SalaryAfterTaxesYearlyTotal { get; init; }

    /// <summary>Row 4 "Tax difference": SalaryMonthly minus SalaryAfterTaxesMonthly, per month.</summary>
    public required decimal[] TaxDifferenceMonthly { get; init; }
    public required decimal TaxDifferenceYearlyTotal { get; init; }

    /// <summary>Row 6 "Dividendo/Juros": sum of DividendoJuros net values, per month.</summary>
    public required decimal[] DividendoJurosMonthly { get; init; }
    public required decimal DividendoJurosYearlyTotal { get; init; }
}
