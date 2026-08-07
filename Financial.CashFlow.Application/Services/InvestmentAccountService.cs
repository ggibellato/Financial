using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class InvestmentAccountService : IInvestmentAccountService
{
    private readonly ICashFlowRepository _repository;

    public InvestmentAccountService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<InvestmentAccountDTO> GetInvestmentAccounts() =>
        _repository.GetInvestmentAccounts().Select(ToDto).ToList();

    private static InvestmentAccountDTO ToDto(InvestmentAccount account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        IsActive = account.IsActive,
        IsLiability = account.IsLiability
    };
}
