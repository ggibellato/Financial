using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
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
            var nonZeroSnapshotAccountIds = _repository.GetInvestmentSnapshots()
                .Where(s => s.Value != 0m)
                .Select(s => s.Account.Id)
                .ToHashSet();
            var result = _repository.GetInvestmentAccounts()
                .Select(account => ToDto(account, nonZeroSnapshotAccountIds.Contains(account.Id)))
                .ToList();

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

            _repository.GetInvestmentAccounts().EnsureNameIsUnique(request.Name, null, a => a.Name, a => a.Id, "An investment account");

            var source = ParseSource(request.Source);
            var creditCard = ResolveCreditCard(source, request.CreditCardId);
            var potsAccount = ResolvePotsAccount(source, request.PotsAccountId);

            var account = InvestmentAccount.Create(request.Name, request.IsActive, request.IsLiability, source, creditCard, potsAccount);

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

            var account = _repository.GetInvestmentAccounts().FirstOrThrow(a => a.Id == id, "Investment account", id);

            _repository.GetInvestmentAccounts().EnsureNameIsUnique(request.Name, id, a => a.Name, a => a.Id, "An investment account");

            var source = ParseSource(request.Source);
            var creditCard = ResolveCreditCard(source, request.CreditCardId);
            var potsAccount = ResolvePotsAccount(source, request.PotsAccountId);

            await _repository.ApplyAndSaveAsync(() =>
            {
                account.Update(request.Name, request.IsActive, request.IsLiability, source, creditCard, potsAccount);
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
            _ = _repository.GetInvestmentAccounts().FirstOrThrow(a => a.Id == id, "Investment account", id);

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

    private static InvestmentAccountSource ParseSource(string source)
    {
        if (!InvestmentAccountSourceParser.TryParse(source, out var parsed))
        {
            throw new ArgumentException($"Source '{source}' is not recognized.");
        }

        return parsed;
    }

    private Account? ResolveCreditCard(InvestmentAccountSource source, Guid? creditCardId)
    {
        if (source != InvestmentAccountSource.CreditCard)
        {
            return null;
        }

        if (!EntityIdResolver.TryResolve(creditCardId, _repository.GetCreditCardAccounts(), c => c.Id, out var creditCard))
        {
            throw new ArgumentException($"Credit card '{creditCardId}' is not recognized.");
        }

        return creditCard;
    }

    private Account? ResolvePotsAccount(InvestmentAccountSource source, Guid? potsAccountId)
    {
        if (source != InvestmentAccountSource.PotsAccountTotal || potsAccountId is null)
        {
            return null;
        }

        if (!EntityIdResolver.TryResolve(potsAccountId, _repository.GetAccounts(), a => a.Id, out var potsAccount))
        {
            throw new ArgumentException($"Pots account '{potsAccountId}' is not recognized.");
        }

        return potsAccount;
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

    private InvestmentAccountDTO ToDto(InvestmentAccount account) => ToDto(account, HasNonZeroInvestmentSnapshot(account.Id));

    private static InvestmentAccountDTO ToDto(InvestmentAccount account, bool hasNonZeroInvestmentSnapshot) => new()
    {
        Id = account.Id,
        Name = account.Name,
        IsActive = account.IsActive,
        IsLiability = account.IsLiability,
        HasNonZeroInvestmentSnapshot = hasNonZeroInvestmentSnapshot,
        Source = account.Source.ToString(),
        CreditCardId = account.CreditCard?.Id,
        PotsAccountId = account.PotsAccount?.Id
    };
}
