using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class BankService : IBankService
{
    private readonly ICashFlowRepository _repository;

    public BankService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<BankDTO> GetBanks() =>
        _repository.GetBanks().Select(ToDto).ToList();

    public async Task<BankDTO> UpdateOpeningBalanceAsync(string name, BankOpeningBalanceUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!BankNameResolver.TryResolve(name, _repository.GetBanks(), out var bank))
        {
            throw new KeyNotFoundException($"Bank '{name}' was not found.");
        }

        bank!.SetOpeningBalance(request.OpeningBalance, request.OpeningBalanceDate);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(bank);
    }

    private static BankDTO ToDto(Bank bank) => new()
    {
        Name = bank.Name,
        RoundUpEnabled = bank.RoundUpEnabled,
        OpeningBalance = bank.OpeningBalance,
        OpeningBalanceDate = bank.OpeningBalanceDate
    };
}
