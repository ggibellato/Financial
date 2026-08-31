using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeSourceService : IIncomeSourceService
{
    private const string EntityType = "IncomeSource";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<IncomeSourceService> _logger;

    public IncomeSourceService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<IncomeSourceService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<IncomeSourceDTO> GetIncomeSources()
    {
        using var span = StartSpan("GetIncomeSources");
        try
        {
            var result = _repository.GetIncomeSources().Select(ToDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetIncomeSources");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<IncomeSourceDTO> CreateIncomeSourceAsync(IncomeSourceCreateDTO request)
    {
        using var span = StartSpan("CreateIncomeSource");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Income source name is required.", nameof(request));
            }

            if (!IncomeGroupParser.TryParse(request.Group, out var group))
            {
                throw new ArgumentException($"Income group '{request.Group}' is not recognized.", nameof(request));
            }

            EnsureNameIsUnique(request.Name, excludingId: null);

            var incomeSource = IncomeSource.Create(request.Name, group, request.IsActive, request.AutoSplitToReserve);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddIncomeSource(incomeSource);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, incomeSource.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateIncomeSource");
            return ToDto(incomeSource);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<IncomeSourceDTO> UpdateIncomeSourceAsync(Guid id, IncomeSourceUpdateDTO request)
    {
        using var span = StartSpan("UpdateIncomeSource");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Income source name is required.", nameof(request));
            }

            if (!IncomeGroupParser.TryParse(request.Group, out var group))
            {
                throw new ArgumentException($"Income group '{request.Group}' is not recognized.", nameof(request));
            }

            if (!EntityIdResolver.TryResolve(id, _repository.GetIncomeSources(), s => s.Id, out var incomeSource))
            {
                throw new KeyNotFoundException($"Income source '{id}' was not found.");
            }

            EnsureNameIsUnique(request.Name, excludingId: id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                incomeSource!.Update(request.Name, group, request.IsActive, request.AutoSplitToReserve);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateIncomeSource");
            return ToDto(incomeSource);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteIncomeSourceAsync(Guid id)
    {
        using var span = StartSpan("DeleteIncomeSource");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(id, _repository.GetIncomeSources(), s => s.Id, out _))
            {
                throw new KeyNotFoundException($"Income source '{id}' was not found.");
            }

            EnsureNotReferenced(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteIncomeSource(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteIncomeSource");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private void EnsureNameIsUnique(string name, Guid? excludingId)
    {
        var collision = _repository.GetIncomeSources().FirstOrDefault(s => s.Name == name && s.Id != excludingId);
        if (collision is not null)
        {
            throw new DuplicateNameException($"An income source named \"{name}\" already exists.");
        }
    }

    private void EnsureNotReferenced(Guid incomeSourceId)
    {
        if (IsReferenced(incomeSourceId))
        {
            throw new EntityInUseException("Cannot delete an income source that is still used by an income entry.");
        }
    }

    /// <summary>Also drives <see cref="IncomeSourceDTO.HasReferences"/>, so the client can disable Delete
    /// before attempting it rather than only learning about the guard from a failed request.</summary>
    private bool IsReferenced(Guid incomeSourceId) =>
        _repository.GetIncomes().Any(i => i.IncomeSource.Id == incomeSourceId);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(IncomeSourceService), operationName, EntityType);
    }

    private IncomeSourceDTO ToDto(IncomeSource source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        IsActive = source.IsActive,
        Group = source.Group.ToString(),
        AutoSplitToReserve = source.AutoSplitToReserve,
        HasReferences = IsReferenced(source.Id)
    };
}
