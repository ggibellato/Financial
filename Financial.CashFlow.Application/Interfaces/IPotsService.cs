using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IPotsService
{
    Task<PotOperationDTO> DepositAsync(Guid potsAccountId, PotDepositRequestDTO request);
    Task<PotOperationDTO> UpdateDepositAsync(Guid potsAccountId, Guid operationId, PotDepositRequestDTO request);
    Task<PotOperationDTO> WithdrawAsync(Guid potsAccountId, PotWithdrawalRequestDTO request);
    Task<PotOperationDTO> UpdateWithdrawalAsync(Guid potsAccountId, Guid operationId, PotWithdrawalRequestDTO request);
    PotOperationDTO GetOperation(Guid operationId);
    Task DeleteOperationAsync(Guid operationId);
}
