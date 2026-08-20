using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeService : IIncomeService
{
    private const string EntityType = "Income";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<IncomeService> _logger;

    public IncomeService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<IncomeService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IncomeDTO> AddIncomeAsync(IncomeCreateDTO request)
    {
        using var span = StartSpan("AddIncome");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var (incomeSource, bank) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description);

            var income = Income.Create(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description);
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddIncome(income);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, income.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddIncome");
            return ToDto(income);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<IncomeDTO> UpdateIncomeAsync(Guid id, IncomeUpdateDTO request)
    {
        using var span = StartSpan("UpdateIncome");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var income = FindIncomeOrThrow(id);

            var (incomeSource, bank) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description);

            await _repository.ApplyAndSaveAsync(() =>
            {
                income.UpdateDetails(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateIncome");
            return ToDto(income);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteIncomeAsync(Guid id)
    {
        using var span = StartSpan("DeleteIncome");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            FindIncomeOrThrow(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteIncome(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteIncome");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<IncomeDTO> GetIncomesByMonth(int year, int month)
    {
        using var span = StartSpan("GetIncomesByMonth");
        try
        {
            var result = _repository.GetIncomes()
                .Where(i => i.Date.Year == year && i.Date.Month == month)
                .Select(ToDto)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetIncomesByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(IncomeService), operationName, EntityType);
    }

    private Income FindIncomeOrThrow(Guid id) =>
        _repository.GetIncomes().FirstOrThrow(i => i.Id == id, "Income", id);

    private (IncomeSource IncomeSource, Bank? Bank) ValidateFields(Guid incomeSourceId, Guid? bankId, string? description)
    {
        if (!EntityIdResolver.TryResolve(incomeSourceId, _repository.GetIncomeSources(), s => s.Id, out var resolvedIncomeSource))
        {
            throw new ArgumentException($"Income source '{incomeSourceId}' is not recognized.");
        }

        DescriptionValidator.EnsureWithinLimit(description);

        Bank? resolvedBank = null;
        if (bankId is not null)
        {
            if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
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
