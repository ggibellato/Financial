using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ControleMaeService : IControleMaeService
{
    private const string EntityType = "MaeLedgerEntry";

    private readonly ICashFlowRepository _repository;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ControleMaeService> _logger;

    public ControleMaeService(ICashFlowRepository repository, IExchangeRateProvider exchangeRateProvider, ITelemetryTracer tracer, ILogger<ControleMaeService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MaeLedgerEntryDTO> CreateEntryAsync(CreateMaeLedgerEntryDTO request)
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

            if (!CurrencyParser.TryParse(request.SourceCurrency, out var sourceCurrency))
            {
                throw new ArgumentException($"Currency '{request.SourceCurrency}' is not recognized.");
            }

            var today = DateOnly.FromDateTime(DateTime.Now);
            if (request.Date > today)
            {
                throw new ArgumentException("Date must not be in the future.");
            }

            var targetCurrency = sourceCurrency == Currency.BRL ? Currency.GBP : Currency.BRL;
            var rate = await _exchangeRateProvider.GetHistoricalRateAsync(request.Date, sourceCurrency, targetCurrency)
                .ConfigureAwait(false);
            var convertedValue = rate.HasValue ? request.SourceValue * rate.Value : (decimal?)null;

            var (brlValue, gbpValue) = sourceCurrency == Currency.BRL
                ? ((decimal?)request.SourceValue, convertedValue)
                : (convertedValue, (decimal?)request.SourceValue);

            var entry = MaeLedgerEntry.Create(request.Date, request.Description, request.Note, sourceCurrency, brlValue, gbpValue);
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddMaeLedgerEntry(entry);
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
            var result = _repository.GetMaeLedgerEntries()
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
            decimal totalBrl = 0m, totalGbp = 0m;
            foreach (var entry in _repository.GetMaeLedgerEntries())
            {
                totalBrl += entry.BrlValue ?? 0m;
                totalGbp += entry.GbpValue ?? 0m;
            }

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTotals");
            return new MaeLedgerTotalsDTO { TotalBrlValue = totalBrl, TotalGbpValue = totalGbp };
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<MaeLedgerEntryDTO> UpdateEntryValuesAsync(Guid id, UpdateMaeLedgerEntryValuesDTO request)
    {
        using var span = StartSpan("UpdateEntryValues");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var entry = _repository.GetMaeLedgerEntries().FirstOrThrow(e => e.Id == id, "Mae ledger entry", id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                entry.UpdateValues(request.BrlValue, request.GbpValue);
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
            _ = _repository.GetMaeLedgerEntries().FirstOrThrow(e => e.Id == id, "Mae ledger entry", id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteMaeLedgerEntry(id);
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

    private static MaeLedgerEntryDTO ToDto(MaeLedgerEntry entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Description = entry.Description,
        Note = entry.Note,
        SourceCurrency = entry.SourceCurrency.ToString(),
        BrlValue = entry.BrlValue,
        GbpValue = entry.GbpValue
    };
}
