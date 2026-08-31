using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IIncomeSourceService
{
    IReadOnlyList<IncomeSourceDTO> GetIncomeSources();
    Task<IncomeSourceDTO> CreateIncomeSourceAsync(IncomeSourceCreateDTO request);
    Task<IncomeSourceDTO> UpdateIncomeSourceAsync(Guid id, IncomeSourceUpdateDTO request);
    Task DeleteIncomeSourceAsync(Guid id);
}
