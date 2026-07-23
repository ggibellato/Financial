using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ControleMaeServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ControleMaeService(null!, new StubExchangeRateProvider(1.5m));
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullExchangeRateProvider_Throws()
    {
        Action act = () => new ControleMaeService(new StubCashFlowRepository(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("exchangeRateProvider");
    }

    [Fact]
    public async Task CreateEntryAsync_WithSuccessfulRateLookup_PopulatesBothCurrenciesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var provider = new StubExchangeRateProvider(0.146m);
        var service = new ControleMaeService(repository, provider);

        var result = await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "School supplies",
            Note = "Term start",
            SourceCurrency = "BRL",
            SourceValue = 350m
        });

        result.BrlValue.Should().Be(350m);
        result.GbpValue.Should().Be(51.1m);
        repository.Entries.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateEntryAsync_WithFailedRateLookup_StillSavesWithOnlyEnteredCurrency()
    {
        var repository = new StubCashFlowRepository();
        var provider = new StubExchangeRateProvider(null);
        var service = new ControleMaeService(repository, provider);

        var result = await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Medical appointment",
            SourceCurrency = "GBP",
            SourceValue = 40m
        });

        result.GbpValue.Should().Be(40m);
        result.BrlValue.Should().BeNull();
        repository.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateEntryAsync_WithFutureDate_ThrowsBeforeTouchingRepositoryOrProvider()
    {
        var repository = new StubCashFlowRepository();
        var provider = new StubExchangeRateProvider(1.5m);
        var service = new ControleMaeService(repository, provider);
        var futureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        var act = async () => await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
        {
            Date = futureDate,
            Description = "Future entry",
            SourceCurrency = "BRL",
            SourceValue = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>();
        repository.Entries.Should().BeEmpty();
        provider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateEntryAsync_WithBlankDescription_Throws()
    {
        var service = new ControleMaeService(new StubCashFlowRepository(), new StubExchangeRateProvider(1.5m));

        var act = async () => await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
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
        var service = new ControleMaeService(new StubCashFlowRepository(), new StubExchangeRateProvider(1.5m));

        var act = async () => await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
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
        var service = new ControleMaeService(new StubCashFlowRepository(), new StubExchangeRateProvider(1.5m));

        var act = async () => await service.CreateEntryAsync(new CreateMaeLedgerEntryDTO
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
        var repository = new StubCashFlowRepository();
        repository.Entries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 6, 30), "Before", string.Empty, Currency.BRL, 10m, 1m));
        repository.Entries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "OnDate", string.Empty, Currency.BRL, 10m, 1m));
        repository.Entries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 8, 10), "After", string.Empty, Currency.BRL, 10m, 1m));
        var service = new ControleMaeService(repository, new StubExchangeRateProvider(1.5m));

        var result = service.GetEntriesFromDate(new DateOnly(2026, 7, 1));

        result.Should().HaveCount(2);
        result.Select(e => e.Description).Should().ContainInOrder("OnDate", "After");
    }

    [Fact]
    public void GetTotals_SumsBrlAndGbpAcrossAllEntriesRegardlessOfDate()
    {
        var repository = new StubCashFlowRepository();
        repository.Entries.Add(MaeLedgerEntry.Create(new DateOnly(2020, 1, 1), "Old", string.Empty, Currency.BRL, 100m, 10m));
        repository.Entries.Add(MaeLedgerEntry.Create(new DateOnly(2026, 7, 10), "Recent", string.Empty, Currency.GBP, null, 5m));
        var service = new ControleMaeService(repository, new StubExchangeRateProvider(1.5m));

        var result = service.GetTotals();

        result.TotalBrlValue.Should().Be(100m);
        result.TotalGbpValue.Should().Be(15m);
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_UpdatesOnlyCurrencyValues()
    {
        var repository = new StubCashFlowRepository();
        var entry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "Medical appointment", "Note", Currency.GBP, null, 40m);
        repository.Entries.Add(entry);
        var service = new ControleMaeService(repository, new StubExchangeRateProvider(1.5m));

        var result = await service.UpdateEntryValuesAsync(entry.Id, new UpdateMaeLedgerEntryValuesDTO
        {
            BrlValue = 320.50m,
            GbpValue = 40m
        });

        result.BrlValue.Should().Be(320.50m);
        result.GbpValue.Should().Be(40m);
        entry.Date.Should().Be(new DateOnly(2026, 7, 1));
        entry.Description.Should().Be("Medical appointment");
        entry.Note.Should().Be("Note");
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new ControleMaeService(new StubCashFlowRepository(), new StubExchangeRateProvider(1.5m));

        var act = async () => await service.UpdateEntryValuesAsync(Guid.NewGuid(), new UpdateMaeLedgerEntryValuesDTO
        {
            BrlValue = 10m,
            GbpValue = 1m
        });

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

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<MaeLedgerEntry> Entries { get; } = new();
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBillTemplate> GetRecurringBillTemplates() => Array.Empty<RecurringBillTemplate>();
        public void AddRecurringBillTemplate(RecurringBillTemplate template) { }
        public void DeleteRecurringBillTemplate(Guid id) { }

        public IEnumerable<RecurringBillInstance> GetRecurringBillInstances() => Array.Empty<RecurringBillInstance>();
        public void AddRecurringBillInstance(RecurringBillInstance instance) { }
        public void DeleteRecurringBillInstance(Guid id) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Entries;
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) => Entries.Add(entry);

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
