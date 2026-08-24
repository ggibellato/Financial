using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
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

            var (incomeSource, bank) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description, request.SplitToReserve);

            var income = Income.Create(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description, request.SplitToReserve);
            var movements = request.SplitToReserve
                ? BuildSplitMovements(income, request.NetValue, request.Date, request.Description)
                : [];

            try
            {
                await _repository.ApplyAndSaveAsync(() =>
                {
                    _repository.AddIncome(income);
                    foreach (var movement in movements)
                    {
                        _repository.AddReserveMovement(movement);
                    }

                    return true;
                }).ConfigureAwait(false);
            }
            catch
            {
                await _repository.ApplyAndSaveAsync(() =>
                {
                    _repository.DeleteIncome(income.Id);
                    foreach (var movement in movements)
                    {
                        _repository.DeleteReserveMovement(movement.Id);
                    }

                    return false;
                }).ConfigureAwait(false);

                throw;
            }

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

            var (incomeSource, bank) = ValidateFields(request.IncomeSourceId, request.BankId, request.Description, request.SplitToReserve);

            var oldDate = income.Date;
            var oldIncomeSource = income.IncomeSource;
            var oldGrossValue = income.GrossValue;
            var oldNetValue = income.NetValue;
            var oldBank = income.Bank;
            var oldDescription = income.Description;
            var oldSplitToReserve = income.SplitToReserve;
            var oldLinkedMovements = GetLinkedMovements(income.Id);

            var newMovements = request.SplitToReserve
                ? BuildSplitMovements(income, request.NetValue, request.Date, request.Description)
                : [];

            try
            {
                await _repository.ApplyAndSaveAsync(() =>
                {
                    income.UpdateDetails(request.Date, incomeSource, request.GrossValue, request.NetValue, bank, request.Description, request.SplitToReserve);
                    foreach (var movement in oldLinkedMovements)
                    {
                        _repository.DeleteReserveMovement(movement.Id);
                    }

                    foreach (var movement in newMovements)
                    {
                        _repository.AddReserveMovement(movement);
                    }

                    return true;
                }).ConfigureAwait(false);
            }
            catch
            {
                await _repository.ApplyAndSaveAsync(() =>
                {
                    income.UpdateDetails(oldDate, oldIncomeSource, oldGrossValue, oldNetValue, oldBank, oldDescription, oldSplitToReserve);
                    foreach (var movement in newMovements)
                    {
                        _repository.DeleteReserveMovement(movement.Id);
                    }

                    foreach (var movement in oldLinkedMovements)
                    {
                        _repository.AddReserveMovement(movement);
                    }

                    return false;
                }).ConfigureAwait(false);

                throw;
            }

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

            var linkedMovements = GetLinkedMovements(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                foreach (var movement in linkedMovements)
                {
                    _repository.DeleteReserveMovement(movement.Id);
                }

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

    private List<ReserveMovement> GetLinkedMovements(Guid incomeId) =>
        _repository.GetReserveMovements().Where(m => m.Income?.Id == incomeId).ToList();

    private (IncomeSource IncomeSource, Bank? Bank) ValidateFields(Guid incomeSourceId, Guid? bankId, string? description, bool splitToReserve)
    {
        if (!EntityIdResolver.TryResolve(incomeSourceId, _repository.GetIncomeSources(), s => s.Id, out var resolvedIncomeSource))
        {
            throw new ArgumentException($"Income source '{incomeSourceId}' is not recognized.");
        }

        if (splitToReserve && !resolvedIncomeSource!.AutoSplitToReserve)
        {
            throw new ArgumentException("This income source does not support automatic reserve splitting.");
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

    private List<ReserveMovement> BuildSplitMovements(Income linkedIncome, decimal netValue, DateOnly date, string? description) =>
        ReserveService.CreateSplitMovements(
            _repository.GetReserveBuckets().Where(b => b.IsActive),
            ComputeSplitBase(netValue),
            date,
            description ?? string.Empty,
            linkedIncome);

    private static decimal ComputeSplitBase(decimal netValue) =>
        Math.Round(TitheRule.NetOfTithe(netValue), 2, MidpointRounding.AwayFromZero);

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
        Description = income.Description,
        SplitToReserve = income.SplitToReserve
    };
}
