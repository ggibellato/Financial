using System.Globalization;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class SettlementService : ISettlementService
{
    private const string EntityType = "SettlementTransaction";

    private readonly ICashFlowRepository _repository;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(ICashFlowRepository repository, IExchangeRateProvider exchangeRateProvider, TimeProvider timeProvider, ITelemetryTracer tracer, ILogger<SettlementService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SettlementEntryDTO> AddEntryAsync(Guid accountId, SettlementEntryCreateDTO request)
    {
        using var span = StartSpan("AddEntry");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = UsableSettlementAccount(accountId);
            EnsureNotInTheFuture(request.Date);
            var entry = SettlementTransaction.Create(
                account, request.Date, request.Description, request.Note, ParsePaidBy(request.PaidBy), CurrencyOf(account, request.Currency), request.Amount, null);
            var warning = await ApplyConvertedValueAsync(entry).ConfigureAwait(false);

            return await SaveNewAsync(entry, warning, span, "AddEntry").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<SettlementEntryDTO> SettleAsync(Guid accountId, SettleRequestDTO request)
    {
        using var span = StartSpan("Settle");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = UsableSettlementAccount(accountId);
            EnsureCanBeSettledNow(account);
            EnsureNotInTheFuture(request.Date);
            var entry = SettlementTransaction.Create(
                account, request.Date, request.Description, request.Note, ParsePaidBy(request.PaidBy), CurrencyOf(account, request.Currency), request.Amount, request.ConvertedValue);
            var warning = RoundedAwayWarning(request.ConvertedValue, entry);

            return await SaveNewAsync(entry, warning, span, "Settle").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<SettlementEntryDTO> UpdateEntryAsync(Guid accountId, Guid entryId, SettlementEntryUpdateDTO request)
    {
        using var span = StartSpan("UpdateEntry");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, entryId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var entry = EntryOf(UsableSettlementAccount(accountId), entryId);
            EnsureNotInTheFuture(request.Date);
            var paidBy = ParsePaidBy(request.PaidBy);

            await _repository.ApplyAndSaveAsync(() =>
            {
                entry.Update(request.Date, request.Description, request.Note, paidBy, request.Amount, request.ConvertedValue);
                return true;
            }).ConfigureAwait(false);

            var warning = RoundedAwayWarning(request.ConvertedValue, entry);
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateEntry");
            return ToDto(entry, warning);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteEntryAsync(Guid accountId, Guid entryId)
    {
        using var span = StartSpan("DeleteEntry");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, entryId.ToString());
        try
        {
            var entry = EntryOf(UsableSettlementAccount(accountId), entryId);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteSettlementTransaction(entry.Id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteEntry");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    internal static SettlementEntryDTO ToDto(SettlementTransaction entry, string? warning) => new()
    {
        Id = entry.Id,
        AccountId = entry.Account.Id,
        Date = entry.Date,
        Description = entry.Description,
        Note = entry.Note,
        PaidBy = entry.PaidBy.ToString(),
        Currency = entry.Currency.ToString(),
        Amount = entry.Amount,
        ConvertedCurrency = SettlementTransaction.ConvertedCurrencyOf(entry.Account, entry.Currency)?.ToString(),
        ConvertedValue = entry.ConvertedValue,
        Warning = warning
    };

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(SettlementService), operationName, EntityType);
    }

    private Account UsableSettlementAccount(Guid accountId)
    {
        var account = _repository.GetAccounts().FirstOrThrow(account => account.Id == accountId, "Account", accountId);
        SettlementTransaction.EnsureSettlementAccount(account);
        ActiveAccountRule.EnsureUsable(null, account);
        return account;
    }

    private void EnsureCanBeSettledNow(Account account)
    {
        if (!AccountActions.AllowedFor(account).Contains(AccountAction.Settle))
        {
            throw new ArgumentException($"{account.Name} can't be settled.");
        }

        var currentBalances = AccountBalanceRule.Balances(account, DateOnly.MaxValue, AccountTransactionReader.Read(_repository));
        if (AccountActions.DisabledFor(account, currentBalances).TryGetValue(AccountAction.Settle, out var reason))
        {
            throw new ArgumentException(reason);
        }
    }

    private async Task<SettlementEntryDTO> SaveNewAsync(SettlementTransaction entry, string? warning, ITelemetrySpan span, string operationName)
    {
        await _repository.ApplyAndSaveAsync(() =>
        {
            _repository.AddSettlementTransaction(entry);
            return true;
        }).ConfigureAwait(false);

        span.SetAttribute(TelemetryAttributeKeys.EntityId, entry.Id.ToString());
        span.MarkSuccess();
        _logger.LogInformation("{Operation} completed", operationName);
        return ToDto(entry, warning);
    }

    private SettlementTransaction EntryOf(Account account, Guid entryId) =>
        _repository.GetSettlementTransactions().FirstOrThrow(entry => entry.Id == entryId && entry.Account.Id == account.Id, "Settlement entry", entryId);

    private void EnsureNotInTheFuture(DateOnly date) => TransactionDateRule.EnsureNotInFuture(date, _timeProvider.GetLocalToday());

    private async Task<string?> ApplyConvertedValueAsync(SettlementTransaction entry)
    {
        var convertedCurrency = SettlementTransaction.ConvertedCurrencyOf(entry.Account, entry.Currency);
        if (convertedCurrency is null)
        {
            return null;
        }

        var rate = await _exchangeRateProvider.GetHistoricalRateAsync(entry.Date, entry.Currency, convertedCurrency.Value).ConfigureAwait(false);
        if (rate is null)
        {
            return $"No exchange rate found for {entry.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}; the {convertedCurrency} value is empty.";
        }

        entry.UpdateValues(entry.PaidBy, entry.Amount, SettlementTransaction.ConvertedValueAt(entry.Amount, rate.Value));
        return entry.ConvertedValue is null ? RoundsToZeroWarning(entry) : null;
    }

    private static string? RoundedAwayWarning(decimal? requestedConvertedValue, SettlementTransaction entry) =>
        requestedConvertedValue is not null && entry.ConvertedValue is null ? RoundsToZeroWarning(entry) : null;

    private static string RoundsToZeroWarning(SettlementTransaction entry) =>
        $"The {SettlementTransaction.ConvertedCurrencyOf(entry.Account, entry.Currency)} value rounds to 0.00, so it was left empty.";

    private static SettlementPayer ParsePaidBy(string? paidBy) => paidBy switch
    {
        nameof(SettlementPayer.You) => SettlementPayer.You,
        nameof(SettlementPayer.Other) => SettlementPayer.Other,
        _ => throw new ArgumentException("Paid by must be You or Other.")
    };

    private static Currency CurrencyOf(Account account, string? code)
    {
        if (code is null)
        {
            return account.Currencies.Count == 1 ? account.Currency : throw new ArgumentException("Choose the currency.");
        }

        return CurrencyParser.Parse(code);
    }
}
