using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class ReserveBucketService : IReserveBucketService
{
    private readonly ICashFlowRepository _repository;

    public ReserveBucketService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<ReserveBucketDTO> GetReserveBuckets() =>
        _repository.GetReserveBuckets().Select(ToDto).ToList();

    private static ReserveBucketDTO ToDto(ReserveBucket bucket) => new()
    {
        Id = bucket.Id,
        Name = bucket.Name,
        IsActive = bucket.IsActive,
        SplitPercentage = bucket.SplitPercentage
    };
}
