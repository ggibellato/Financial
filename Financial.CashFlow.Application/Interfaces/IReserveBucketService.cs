using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IReserveBucketService
{
    IReadOnlyList<ReserveBucketDTO> GetReserveBuckets();
    Task<ReserveBucketDTO> CreateReserveBucketAsync(ReserveBucketCreateDTO request);
    Task<ReserveBucketDTO> UpdateReserveBucketAsync(Guid id, ReserveBucketUpdateDTO request);
}
