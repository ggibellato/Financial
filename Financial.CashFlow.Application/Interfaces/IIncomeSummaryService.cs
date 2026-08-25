using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Domain.ValueObjects;

namespace Financial.CashFlow.Application.Interfaces;

public interface IIncomeSummaryService
{
    IncomeAnnualSummaryDTO GetIncomeSummaryForYear(int year);

    /// <summary>
    /// Returns the raw, un-rounded monthly SalaryAfterTaxes series (Display: every recorded
    /// income for the year; ForAverage: excluding the current calendar month) that
    /// <see cref="ICategorySummaryService.GetCategoryTotalsAnnualForYear"/> combines with expense
    /// totals to compute Resultado. Exposed separately from <see cref="GetIncomeSummaryForYear"/>
    /// because that DTO only carries the already-rounded average, and Resultado must combine the
    /// three raw monthly series before rounding once, not combine three already-rounded averages.
    /// </summary>
    (MonthlySeries Display, MonthlySeries ForAverage) GetSalaryAfterTaxesSeriesForYear(int year);
}
