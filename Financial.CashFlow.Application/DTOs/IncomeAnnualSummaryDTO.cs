namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for the Annual Summary page's Income Summary table.
/// </summary>
public sealed class IncomeAnnualSummaryDTO
{
    /// <summary>Row 2 "Salary": sum of Gleison + Ariana gross values, per month.</summary>
    public required decimal[] SalaryMonthly { get; init; }
    public required decimal SalaryAnnualTotal { get; init; }

    /// <summary>Row 3 "Salary after taxes": sum of Gleison + Ariana net values, per month.</summary>
    public required decimal[] SalaryAfterTaxesMonthly { get; init; }
    public required decimal SalaryAfterTaxesAnnualTotal { get; init; }

    /// <summary>Row 4 "Tax difference": SalaryMonthly minus SalaryAfterTaxesMonthly, per month.</summary>
    public required decimal[] TaxDifferenceMonthly { get; init; }
    public required decimal TaxDifferenceAnnualTotal { get; init; }

    /// <summary>Row 6 "Dividendo/Juros": sum of DividendoJuros net values, per month.</summary>
    public required decimal[] DividendoJurosMonthly { get; init; }
    public required decimal DividendoJurosAnnualTotal { get; init; }
}
