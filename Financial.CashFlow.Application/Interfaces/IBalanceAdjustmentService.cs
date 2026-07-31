using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IBalanceAdjustmentService
{
    Task<BalanceAdjustmentDTO> AddAdjustmentAsync(string bankName, BalanceAdjustmentCreateDTO request);
    Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(string bankName, Guid id, BalanceAdjustmentUpdateDTO request);
    Task DeleteAdjustmentAsync(string bankName, Guid id);
    IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(string bankName);
}
