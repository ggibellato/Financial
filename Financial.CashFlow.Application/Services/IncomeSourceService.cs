using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeSourceService : IIncomeSourceService
{
    private readonly ICashFlowRepository _repository;

    public IncomeSourceService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<IncomeSourceDTO> GetIncomeSources() =>
        _repository.GetIncomeSources().Select(ToDto).ToList();

    private static IncomeSourceDTO ToDto(IncomeSource source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        IsActive = source.IsActive,
        Group = source.Group.ToString()
    };
}
