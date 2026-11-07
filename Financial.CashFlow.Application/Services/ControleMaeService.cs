using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ControleMaeService : IControleMaeService
{
    private const string EntityType = "SettlementTransaction";

    private readonly ICashFlowRepository _repository;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ControleMaeService> _logger;

    public ControleMaeService(ICashFlowRepository repository, IExchangeRateProvider exchangeRateProvider, TimeProvider timeProvider, ITelemetryTracer tracer, ILogger<ControleMaeService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MaeLedgerEntryDTO> CreateEntryAsync(MaeLedgerEntryCreateDTO request)
    {
        using var span = StartSpan("CreateEntry");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ArgumentException("Description is required.");
            }

            if (request.SourceValue == 0)
            {
                throw new ArgumentException("Source value must not be zero.");
            }

            var mae = MaeAccount.Find(_repository.GetAccounts())
                ?? throw new ArgumentException("Create a Settlement account first.");

            if (!CurrencyParser.TryParse(request.SourceCurrency, out var sourceCurrency) || !mae.Currencies.Contains(sourceCurrency))
            {
                throw new ArgumentException($"Currency '{request.SourceCurrency}' is not recognized.");
            }

            var today = _timeProvider.GetLocalToday();
            if (request.Date > today)
            {
                throw new ArgumentException("Date must not be in the future.");
            }

            var amount = Math.Abs(request.SourceValue);
            var convertedValue = await ConvertedValueAsync(mae, request.Date, sourceCurrency, amount).ConfigureAwait(false);
            var entry = SettlementTransaction.Create(
                mae, request.Date, request.Description, request.Note, SettlementTransaction.PayerOf(request.SourceValue), sourceCurrency, amount, convertedValue);
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddSettlementTransaction(entry);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, entry.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateEntry");
            return ToDto(entry);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<MaeLedgerEntryDTO> GetEntriesFromDate(DateOnly fromDate)
    {
        using var span = StartSpan("GetEntriesFromDate");
        try
        {
            var result = MaeEntries()
                .Where(e => e.Date >= fromDate)
                .OrderBy(e => e.Date)
                .Select(ToDto)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetEntriesFromDate");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public MaeLedgerTotalsDTO GetTotals()
    {
        using var span = StartSpan("GetTotals");
        try
        {
            var mae = MaeAccount.Find(_repository.GetAccounts());
            var entries = _repository.GetSettlementTransactions();
            var result = mae is null
                ? new MaeLedgerTotalsDTO { TotalBrlValue = 0m, TotalGbpValue = 0m }
                : new MaeLedgerTotalsDTO
                {
                    TotalBrlValue = AccountBalanceRule.Balance(mae, Currency.BRL, DateOnly.MaxValue, entries),
                    TotalGbpValue = AccountBalanceRule.Balance(mae, Currency.GBP, DateOnly.MaxValue, entries)
                };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTotals");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<MaeLedgerEntryDTO> UpdateEntryValuesAsync(Guid id, MaeLedgerEntryValuesUpdateDTO request)
    {
        using var span = StartSpan("UpdateEntryValues");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var entry = MaeEntries().FirstOrThrow(e => e.Id == id, "Mae ledger entry", id);
            var (paidValue, otherValue) = entry.Currency == Currency.BRL
                ? (request.BrlValue, request.GbpValue)
                : (request.GbpValue, request.BrlValue);
            var paid = RequireOneSignedPaidValue(entry.Currency, paidValue, otherValue);

            await _repository.ApplyAndSaveAsync(() =>
            {
                entry.UpdateValues(SettlementTransaction.PayerOf(paid), Math.Abs(paid), otherValue is null ? null : Math.Abs(otherValue.Value));
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateEntryValues");
            return ToDto(entry);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteEntryAsync(Guid id)
    {
        using var span = StartSpan("DeleteEntry");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            _ = MaeEntries().FirstOrThrow(e => e.Id == id, "Mae ledger entry", id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteSettlementTransaction(id);
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

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(ControleMaeService), operationName, EntityType);
    }

    private IEnumerable<SettlementTransaction> MaeEntries()
    {
        var mae = MaeAccount.Find(_repository.GetAccounts());
        return mae is null ? [] : _repository.GetSettlementTransactions().Where(entry => entry.Account.Id == mae.Id);
    }

    private async Task<decimal?> ConvertedValueAsync(Account account, DateOnly date, Currency sourceCurrency, decimal amount)
    {
        var targetCurrency = SettlementTransaction.ConvertedCurrencyOf(account, sourceCurrency);
        if (targetCurrency is null)
        {
            return null;
        }

        var rate = await _exchangeRateProvider.GetHistoricalRateAsync(date, sourceCurrency, targetCurrency.Value).ConfigureAwait(false);
        return rate.HasValue ? amount * rate.Value : null;
    }

    private static decimal RequireOneSignedPaidValue(Currency paidCurrency, decimal? paidValue, decimal? otherValue)
    {
        if (paidValue is null or 0)
        {
            throw new ArgumentException($"{paidCurrency} value is required and must not be zero.");
        }

        if (!SettlementTransaction.HaveOneSign(paidValue.Value, otherValue))
        {
            throw new ArgumentException("BRL and GBP values must both be positive or both be negative.");
        }

        return paidValue.Value;
    }

    private static MaeLedgerEntryDTO ToDto(SettlementTransaction entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Description = entry.Description,
        Note = entry.Note,
        SourceCurrency = entry.Currency.ToString(),
        BrlValue = entry.SignedValueIn(Currency.BRL),
        GbpValue = entry.SignedValueIn(Currency.GBP)
    };
}
