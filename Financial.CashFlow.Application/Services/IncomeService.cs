using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeService : IIncomeService
{
    private const int MaxDescriptionLength = 200;

    private readonly ICashFlowRepository _repository;

    public IncomeService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IncomeDTO> AddIncomeAsync(IncomeCreateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (incomeSource, bank) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description);

        var income = Income.Create(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description);
        _repository.AddIncome(income);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(income);
    }

    public async Task<IncomeDTO> UpdateIncomeAsync(Guid id, IncomeUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var income = FindIncomeOrThrow(id);

        var (incomeSource, bank) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description);

        income.UpdateDetails(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(income);
    }

    public async Task DeleteIncomeAsync(Guid id)
    {
        FindIncomeOrThrow(id);

        _repository.DeleteIncome(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public IReadOnlyList<IncomeDTO> GetIncomesByMonth(int year, int month) =>
        _repository.GetIncomes()
            .Where(i => i.Date.Year == year && i.Date.Month == month)
            .Select(ToDto)
            .ToList();

    private Income FindIncomeOrThrow(Guid id) =>
        _repository.GetIncomes().FirstOrDefault(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Income '{id}' was not found.");

    private (IncomeSource IncomeSource, Bank? Bank) ValidateFields(Guid incomeSourceId, Guid? bankId, string? description)
    {
        if (!IncomeSourceNameResolver.TryResolve(incomeSourceId, _repository.GetIncomeSources(), out var resolvedIncomeSource))
        {
            throw new ArgumentException($"Income source '{incomeSourceId}' is not recognized.");
        }

        if (description is not null && description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Description must not exceed {MaxDescriptionLength} characters.");
        }

        Bank? resolvedBank = null;
        if (bankId is not null)
        {
            if (!BankNameResolver.TryResolve(bankId, _repository.GetBanks(), out var bank))
            {
                throw new ArgumentException($"Bank '{bankId}' is not recognized.");
            }

            resolvedBank = bank!;
        }

        return (resolvedIncomeSource!, resolvedBank);
    }

    private static IncomeDTO ToDto(Income income) => new()
    {
        Id = income.Id,
        Date = income.Date,
        IncomeSourceId = income.IncomeSource.Id,
        IncomeSourceName = income.IncomeSource.Name,
        GrossValue = income.GrossValue,
        NetValue = income.NetValue,
        BankId = income.Bank?.Id,
        BankName = income.Bank?.Name,
        Description = income.Description
    };
}
