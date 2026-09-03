using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class InvestmentAccountService : IInvestmentAccountService
{
    private const string EntityType = "InvestmentAccount";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<InvestmentAccountService> _logger;

    public InvestmentAccountService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<InvestmentAccountService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<InvestmentAccountDTO> GetInvestmentAccounts()
    {
        using var span = StartSpan("GetInvestmentAccounts");
        try
        {
            var result = _repository.GetInvestmentAccounts().Select(ToDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetInvestmentAccounts");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<InvestmentAccountDTO> CreateInvestmentAccountAsync(InvestmentAccountCreateDTO request)
    {
        using var span = StartSpan("CreateInvestmentAccount");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Investment account name is required.", nameof(request));
            }

            EnsureNameIsUnique(request.Name, excludingId: null);

            var account = InvestmentAccount.Create(request.Name, request.IsActive, request.IsLiability);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddInvestmentAccount(account);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, account.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateInvestmentAccount");
            return ToDto(account);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<InvestmentAccountDTO> UpdateInvestmentAccountAsync(Guid id, InvestmentAccountUpdateDTO request)
    {
        using var span = StartSpan("UpdateInvestmentAccount");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Investment account name is required.", nameof(request));
            }

            if (!EntityIdResolver.TryResolve(id, _repository.GetInvestmentAccounts(), a => a.Id, out var account))
            {
                throw new KeyNotFoundException($"Investment account '{id}' was not found.");
            }

            EnsureNameIsUnique(request.Name, excludingId: id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                account!.Update(request.Name, request.IsActive, request.IsLiability);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateInvestmentAccount");
            return ToDto(account);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteInvestmentAccountAsync(Guid id)
    {
        using var span = StartSpan("DeleteInvestmentAccount");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(id, _repository.GetInvestmentAccounts(), a => a.Id, out _))
            {
                throw new KeyNotFoundException($"Investment account '{id}' was not found.");
            }

            EnsureNoNonZeroInvestmentSnapshotExists(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteInvestmentAccount(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteInvestmentAccount");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private void EnsureNameIsUnique(string name, Guid? excludingId)
    {
        var collision = _repository.GetInvestmentAccounts().FirstOrDefault(a => a.Name == name && a.Id != excludingId);
        if (collision is not null)
        {
            throw new DuplicateNameException($"An investment account named \"{name}\" already exists.");
        }
    }

    private void EnsureNoNonZeroInvestmentSnapshotExists(Guid accountId)
    {
        if (HasNonZeroInvestmentSnapshot(accountId))
        {
            throw new EntityInUseException("Cannot delete an investment account with a non-zero balance.");
        }
    }

    /// <summary>Whether any InvestmentSnapshot recorded for this account has a non-zero value.
    /// Checking every snapshot (not just the chronologically latest) matters because the import
    /// writes an explicit 0 for every not-yet-happened month of the current year, which would
    /// otherwise outrank a real balance recorded earlier that same year. Also drives
    /// <see cref="InvestmentAccountDTO.HasNonZeroInvestmentSnapshot"/>.</summary>
    private bool HasNonZeroInvestmentSnapshot(Guid accountId) =>
        _repository.GetInvestmentSnapshots().Any(s => s.Account.Id == accountId && s.Value != 0m);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(InvestmentAccountService), operationName, EntityType);
    }

    private InvestmentAccountDTO ToDto(InvestmentAccount account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        IsActive = account.IsActive,
        IsLiability = account.IsLiability,
        HasNonZeroInvestmentSnapshot = HasNonZeroInvestmentSnapshot(account.Id)
    };
}
