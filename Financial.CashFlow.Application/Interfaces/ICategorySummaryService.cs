using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICategorySummaryService
{
    CategoryTotalsAnnualDTO GetCategoryTotalsAnnualForYear(int year);
}
