using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeService : IIncomeService
{
    private readonly ICashFlowRepository _repository;

    public IncomeService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IncomeDTO> AddIncomeAsync(IncomeCreateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (incomeSource, bank) = ValidateFields(request.IncomeSource, request.Bank);

        var income = Income.Create(request.Date, incomeSource, request.GrossValue, request.NetValue, bank.Name);
        _repository.AddIncome(income);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(income);
    }

    public async Task<IncomeDTO> UpdateIncomeAsync(Guid id, IncomeUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var income = FindIncomeOrThrow(id);

        var (incomeSource, bank) = ValidateFields(request.IncomeSource, request.Bank);

        income.UpdateDetails(request.Date, incomeSource, request.GrossValue, request.NetValue, bank.Name);
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

    private (IncomeSource IncomeSource, Bank Bank) ValidateFields(string incomeSource, string bank)
    {
        if (!IncomeSourceParser.TryParse(incomeSource, out var parsedIncomeSource))
        {
            throw new ArgumentException($"Income source '{incomeSource}' is not recognized.");
        }

        if (!BankNameResolver.TryResolve(bank, _repository.GetBanks(), out var resolvedBank))
        {
            throw new ArgumentException($"Bank '{bank}' is not recognized.");
        }

        return (parsedIncomeSource, resolvedBank!);
    }

    private static IncomeDTO ToDto(Income income) => new()
    {
        Id = income.Id,
        Date = income.Date,
        IncomeSource = income.IncomeSource.ToString(),
        GrossValue = income.GrossValue,
        NetValue = income.NetValue,
        Bank = income.Bank
    };
}
