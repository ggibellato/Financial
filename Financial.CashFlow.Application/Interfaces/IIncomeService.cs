using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IIncomeService
{
    Task<IncomeDTO> AddIncomeAsync(IncomeCreateDTO request);
    Task<IncomeDTO> UpdateIncomeAsync(Guid id, IncomeUpdateDTO request);
    Task DeleteIncomeAsync(Guid id);
    IReadOnlyList<IncomeDTO> GetIncomesByMonth(int year, int month);
}
