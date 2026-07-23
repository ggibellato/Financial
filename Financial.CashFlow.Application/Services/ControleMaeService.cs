using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

public sealed class ControleMaeService : IControleMaeService
{
    private readonly ICashFlowRepository _repository;
    private readonly IExchangeRateProvider _exchangeRateProvider;

    public ControleMaeService(ICashFlowRepository repository, IExchangeRateProvider exchangeRateProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
    }

    public async Task<MaeLedgerEntryDTO> CreateEntryAsync(CreateMaeLedgerEntryDTO request)
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
        _repository.AddMaeLedgerEntry(entry);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(entry);
    }

    public IReadOnlyList<MaeLedgerEntryDTO> GetEntriesFromDate(DateOnly fromDate) =>
        _repository.GetMaeLedgerEntries()
            .Where(e => e.Date >= fromDate)
            .OrderBy(e => e.Date)
            .Select(ToDto)
            .ToList();

    public MaeLedgerTotalsDTO GetTotals()
    {
        var entries = _repository.GetMaeLedgerEntries();
        return new MaeLedgerTotalsDTO
        {
            TotalBrlValue = entries.Sum(e => e.BrlValue ?? 0m),
            TotalGbpValue = entries.Sum(e => e.GbpValue ?? 0m)
        };
    }

    public async Task<MaeLedgerEntryDTO> UpdateEntryValuesAsync(Guid id, UpdateMaeLedgerEntryValuesDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = _repository.GetMaeLedgerEntries().FirstOrDefault(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Mae ledger entry '{id}' was not found.");

        entry.UpdateValues(request.BrlValue, request.GbpValue);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(entry);
    }

    public async Task DeleteEntryAsync(Guid id)
    {
        _ = _repository.GetMaeLedgerEntries().FirstOrDefault(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Mae ledger entry '{id}' was not found.");

        _repository.DeleteMaeLedgerEntry(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
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
