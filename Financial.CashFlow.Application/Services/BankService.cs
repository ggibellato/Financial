using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.CashFlow.Application.Services;

public sealed class BankService : IBankService
{
    private readonly ICashFlowRepository _repository;

    public BankService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<BankDTO> GetBanks() =>
        _repository.GetBanks()
            .Select(bank => new BankDTO { Name = bank.Name, RoundUpEnabled = bank.RoundUpEnabled })
            .ToList();
}
