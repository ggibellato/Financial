using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IPotsService
{
    Task<PotOperationDTO> WithdrawAsync(Guid potsAccountId, PotWithdrawalRequestDTO request);
    Task<PotOperationDTO> UpdateWithdrawalAsync(Guid potsAccountId, Guid operationId, PotWithdrawalRequestDTO request);
    PotOperationDTO GetOperation(Guid operationId);
    Task DeleteOperationAsync(Guid operationId);
}
