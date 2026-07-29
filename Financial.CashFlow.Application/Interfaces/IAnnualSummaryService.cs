using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IAnnualSummaryService
{
    IReadOnlyList<CategoryAnnualGroupValueDTO> GetHistoricSummaryAverageFromYear(int year);
    CategoryTotalsAnnualDTO GetCategoryTotalsAnnualForYear(int year);
    InvestmentAnnualResultDTO GetInvestmentAnnualResultForYear(int year);
}
