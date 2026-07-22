using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IReserveService
{
    Task<IncomeSplitResultDTO> PostIncomeSplitAsync(IncomeSplitRequestDTO request);
    Task<ReserveMovementDTO> PostWithdrawalAsync(WithdrawalRequestDTO request);
    IReadOnlyList<ReserveBucketBalanceDTO> GetBucketBalances();
    IReadOnlyList<ReserveMovementDTO> GetMovementHistory();
}
