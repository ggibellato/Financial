using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IBalanceAdjustmentService
{
    Task<BalanceAdjustmentDTO> AddAdjustmentAsync(Guid bankId, BalanceAdjustmentCreateDTO request);
    Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(Guid bankId, Guid id, BalanceAdjustmentUpdateDTO request);
    Task DeleteAdjustmentAsync(Guid bankId, Guid id);
    IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(Guid bankId);
}
