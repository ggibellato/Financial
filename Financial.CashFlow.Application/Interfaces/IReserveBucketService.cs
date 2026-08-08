using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IReserveBucketService
{
    IReadOnlyList<ReserveBucketDTO> GetReserveBuckets();
}
