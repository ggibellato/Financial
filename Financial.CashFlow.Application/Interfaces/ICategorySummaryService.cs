using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICategorySummaryService
{
    IReadOnlyList<CategoryAnnualTotalDTO> GetCategoryTotalsForYear(int year);
    CategoryTotalsAnnualDTO GetCategoryTotalsAnnualForYear(int year);
}
