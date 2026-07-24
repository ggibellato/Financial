using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class IncomeServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new IncomeService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task AddIncomeAsync_WithValidRequest_SavesAndReturnsIncome()
    {
        var repository = new StubCashFlowRepository();
        var service = new IncomeService(repository);

        var result = await service.AddIncomeAsync(ToCreateDto(ValidCreateRequest()));

        result.Date.Should().Be(new DateOnly(2026, 7, 25));
        result.IncomeSource.Should().Be("Gleison");
        result.GrossValue.Should().Be(3200.00m);
        result.NetValue.Should().Be(2450.00m);
        result.Bank.Should().Be("Barclays");
        repository.Incomes.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutGrossValue_SavesNull()
    {
        var service = new IncomeService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { GrossValue = null });

        var result = await service.AddIncomeAsync(request);

        result.GrossValue.Should().BeNull();
    }

    [Fact]
    public async Task AddIncomeAsync_MultipleEntriesForSameSourceAndMonth_AllPersist()
    {
        var repository = new StubCashFlowRepository();
        var service = new IncomeService(repository);
        var request = ValidCreateRequest() with { IncomeSource = "Ariana", GrossValue = null };

        await service.AddIncomeAsync(ToCreateDto(request with { Date = new DateOnly(2026, 7, 1) }));
        await service.AddIncomeAsync(ToCreateDto(request with { Date = new DateOnly(2026, 7, 8) }));
        await service.AddIncomeAsync(ToCreateDto(request with { Date = new DateOnly(2026, 7, 15) }));

        repository.Incomes.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddIncomeAsync_WithGrossValueBelowNetValue_ThrowsArgumentException()
    {
        var service = new IncomeService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { GrossValue = 100m, NetValue = 200m });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddIncomeAsync_WithNegativeNetValue_ThrowsArgumentException()
    {
        var service = new IncomeService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { GrossValue = null, NetValue = -1m });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddIncomeAsync_WithUnrecognizedIncomeSource_ThrowsArgumentException()
    {
        var service = new IncomeService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { IncomeSource = "NotASource" });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Income source*not recognized*");
    }

    [Fact]
    public async Task AddIncomeAsync_WithUnrecognizedBank_ThrowsArgumentException()
    {
        var service = new IncomeService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { Bank = "NotABank" });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Bank*not recognized*");
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithExistingId_UpdatesInPlace()
    {
        var repository = new StubCashFlowRepository();
        var service = new IncomeService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(ValidCreateRequest()));

        var updateRequest = ToUpdateDto(ValidCreateRequest() with { NetValue = 500m, GrossValue = null, IncomeSource = "Lottery" });
        var result = await service.UpdateIncomeAsync(added.Id, updateRequest);

        result.Id.Should().Be(added.Id);
        result.NetValue.Should().Be(500m);
        result.IncomeSource.Should().Be("Lottery");
        repository.Incomes.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new IncomeService(new StubCashFlowRepository());

        var act = async () => await service.UpdateIncomeAsync(Guid.NewGuid(), ToUpdateDto(ValidCreateRequest()));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var service = new IncomeService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(ValidCreateRequest()));

        await service.DeleteIncomeAsync(added.Id);

        repository.Incomes.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new IncomeService(new StubCashFlowRepository());

        var act = async () => await service.DeleteIncomeAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetIncomesByMonth_ReturnsOnlyIncomesInThatMonth()
    {
        var service = new IncomeService(new StubCashFlowRepository());
        await service.AddIncomeAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddIncomeAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = service.GetIncomesByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    private static IncomeCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 25),
        "Gleison",
        3200.00m,
        2450.00m,
        "Barclays");

    private static IncomeCreateDTO ToCreateDto(IncomeCreateRequest r) => new()
    {
        Date = r.Date,
        IncomeSource = r.IncomeSource,
        GrossValue = r.GrossValue,
        NetValue = r.NetValue,
        Bank = r.Bank
    };

    private static IncomeUpdateDTO ToUpdateDto(IncomeCreateRequest r) => new()
    {
        Date = r.Date,
        IncomeSource = r.IncomeSource,
        GrossValue = r.GrossValue,
        NetValue = r.NetValue,
        Bank = r.Bank
    };

    private sealed record IncomeCreateRequest(
        DateOnly Date, string IncomeSource, decimal? GrossValue, decimal NetValue, string Bank);

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<Income> Incomes { get; } = new();
        public List<Bank> Banks { get; } = new()
        {
            Bank.Create("Barclays", roundUpEnabled: false),
            Bank.Create("Trading212", roundUpEnabled: true),
            Bank.Create("Chase", roundUpEnabled: true)
        };
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBill> GetRecurringBills() => Array.Empty<RecurringBill>();
        public void AddRecurringBill(RecurringBill bill) { }
        public void DeleteRecurringBill(Guid id) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }
        public void DeleteMaeLedgerEntry(Guid id) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public IEnumerable<Bank> GetBanks() => Banks;

        public IEnumerable<Income> GetIncomes() => Incomes;
        public void AddIncome(Income income) => Incomes.Add(income);
        public void DeleteIncome(Guid id) => Incomes.RemoveAll(i => i.Id == id);

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
