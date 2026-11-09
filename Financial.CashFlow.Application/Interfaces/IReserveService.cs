using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IReserveService
{
    Task<ReserveMovementDTO> UpdateMovementAsync(Guid id, ReserveMovementUpdateDTO request);
    Task DeleteMovementAsync(Guid id);
}
