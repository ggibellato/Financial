using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IHistoricAverageService
{
    IReadOnlyList<CategoryAnnualAverageDTO> GetHistoricSummaryAverageFromYear(int year);
}
