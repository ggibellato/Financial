using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IAnnualSummaryService
{
    IReadOnlyList<CategoryAnnualTotalDTO> GetCategoryTotalsForYear(int year);
    InvestmentDiffsAnnualDTO GetInvestmentDiffsForYear(int year);
    IncomeAnnualSummaryDTO GetIncomeSummaryForYear(int year);
    IReadOnlyList<CategoryAnnualAverageDTO> GetHistoricSummaryAverageFromYear(int year);
}
