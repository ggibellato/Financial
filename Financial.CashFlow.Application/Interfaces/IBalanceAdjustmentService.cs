using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IBalanceAdjustmentService
{
    Task<BalanceAdjustmentDTO> AddAdjustmentAsync(Guid accountId, BalanceAdjustmentCreateDTO request);
    Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(Guid accountId, Guid id, BalanceAdjustmentUpdateDTO request);
    Task DeleteAdjustmentAsync(Guid accountId, Guid id);
}
