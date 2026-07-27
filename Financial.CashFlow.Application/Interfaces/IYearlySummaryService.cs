using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IYearlySummaryService
{
    IReadOnlyList<CategoryYearlyTotalDTO> GetCategoryTotalsForYear(int year);
    InvestmentDiffsYearlyDTO GetInvestmentDiffsForYear(int year);
    IncomeYearlySummaryDTO GetIncomeSummaryForYear(int year);
    IReadOnlyList<CategoryAnnualAverageDTO> GetHistoricCategoriesAverageFromYear(int year);
    IReadOnlyList<IncomeAnnualAverageDTO> GetHistoricIncomeAverageFromYear(int year);
}
