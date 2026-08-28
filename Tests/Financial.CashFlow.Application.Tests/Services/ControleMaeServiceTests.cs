using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ControleMaeServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<ControleMaeService> Logger = NullLogger<ControleMaeService>.Instance;
    private const decimal DefaultRate = 1.5m;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly ControleMaeService _sut;

    public ControleMaeServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private ControleMaeService CreateService(StubCashFlowRepository? repository = null, IExchangeRateProvider? exchangeRateProvider = null) =>
        new(repository ?? _repository, exchangeRateProvider ?? new StubExchangeRateProvider(DefaultRate), _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ControleMaeService(null!, new StubExchangeRateProvider(DefaultRate), _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullExchangeRateProvider_Throws()
    {
        Action act = () => new ControleMaeService(_repository, null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("exchangeRateProvider");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new ControleMaeService(_repository, new StubExchangeRateProvider(DefaultRate), null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task CreateEntryAsync_WithSuccessfulRateLookup_PopulatesBothCurrenciesAndSaves()
    {
        var provider = new StubExchangeRateProvider(0.146m);
        var service = CreateService(exchangeRateProvider: provider);

        var result = await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "School supplies",
            Note = "Term start",
            SourceCurrency = "BRL",
            SourceValue = 350m
        });

        using (new AssertionScope())
        {
            result.BrlValue.Should().Be(350m);
            result.GbpValue.Should().Be(51.1m);
            _repository.MaeLedgerEntries.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateEntryAsync_WithFailedRateLookup_StillSavesWithOnlyEnteredCurrency()
    {
        var provider = new StubExchangeRateProvider(null);
        var service = CreateService(exchangeRateProvider: provider);

        var result = await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Medical appointment",
            SourceCurrency = "GBP",
            SourceValue = 40m
        });

        using (new AssertionScope())
        {
            result.GbpValue.Should().Be(40m);
            result.BrlValue.Should().BeNull();
            _repository.MaeLedgerEntries.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task CreateEntryAsync_WithFutureDate_ThrowsBeforeTouchingRepositoryOrProvider()
    {
        var provider = new StubExchangeRateProvider(DefaultRate);
        var service = CreateService(exchangeRateProvider: provider);
        var futureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        var act = async () => await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = futureDate,
            Description = "Future entry",
            SourceCurrency = "BRL",
            SourceValue = 100m
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.MaeLedgerEntries.Should().BeEmpty();
            provider.CallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateEntryAsync_WithBlankDescription_Throws()
    {
        var act = async () => await _sut.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "   ",
            SourceCurrency = "BRL",
            SourceValue = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateEntryAsync_WithUnrecognizedCurrency_Throws()
    {
        var act = async () => await _sut.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Test",
            SourceCurrency = "USD",
            SourceValue = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateEntryAsync_WithZeroValue_Throws()
    {
        var act = async () => await _sut.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Test",
            SourceCurrency = "BRL",
            SourceValue = 0m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetEntriesFromDate_ReturnsOnlyEntriesOnOrAfterDate()
    {
        _repository.MaeLedgerEntries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 6, 30), "Before", string.Empty, Currency.BRL, 10m, 1m));
        _repository.MaeLedgerEntries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "OnDate", string.Empty, Currency.BRL, 10m, 1m));
        _repository.MaeLedgerEntries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 8, 10), "After", string.Empty, Currency.BRL, 10m, 1m));

        var result = _sut.GetEntriesFromDate(new DateOnly(2026, 7, 1));

        result.Should().HaveCount(2);
        result.Select(e => e.Description).Should().ContainInOrder("OnDate", "After");
    }

    [Fact]
    public void GetTotals_SumsBrlAndGbpAcrossAllEntriesRegardlessOfDate()
    {
        _repository.MaeLedgerEntries.Add(MaeLedgerEntry.Create(new DateOnly(2020, 1, 1), "Old", string.Empty, Currency.BRL, 100m, 10m));
        _repository.MaeLedgerEntries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 7, 10), "Recent", string.Empty, Currency.GBP, null, 5m));

        var result = _sut.GetTotals();

        result.TotalBrlValue.Should().Be(100m);
        result.TotalGbpValue.Should().Be(15m);
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_UpdatesOnlyCurrencyValues()
    {
        var entry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "Medical appointment", "Note", Currency.GBP, null, 40m);
        _repository.MaeLedgerEntries.Add(entry);

        var result = await _sut.UpdateEntryValuesAsync(entry.Id, new UpdateMaeLedgerEntryValuesDTO
        {
            BrlValue = 320.50m,
            GbpValue = 40m
        });

        using (new AssertionScope())
        {
            result.BrlValue.Should().Be(320.50m);
            result.GbpValue.Should().Be(40m);
            entry.Date.Should().Be(new DateOnly(2026, 7, 1));
            entry.Description.Should().Be("Medical appointment");
            entry.Note.Should().Be("Note");
        }
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateEntryValuesAsync(Guid.NewGuid(), new UpdateMaeLedgerEntryValuesDTO
        {
            BrlValue = 10m,
            GbpValue = 1m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteEntryAsync_ExistingId_RemovesEntryAndSaves()
    {
        var entry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "School supplies", string.Empty, Currency.BRL, 350m, 51.1m);
        _repository.MaeLedgerEntries.Add(entry);

        await _sut.DeleteEntryAsync(entry.Id);

        _repository.MaeLedgerEntries.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteEntryAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteEntryAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private sealed class StubExchangeRateProvider : IExchangeRateProvider
    {
        private readonly decimal? _rate;

        public StubExchangeRateProvider(decimal? rate)
        {
            _rate = rate;
        }

        public int CallCount { get; private set; }

        public Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
        {
            CallCount++;
            return Task.FromResult(_rate);
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new ControleMaeService(_repository, new StubExchangeRateProvider(DefaultRate), _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
