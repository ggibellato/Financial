using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ExpenseServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ExpenseService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task AddExpenseAsync_WithValidRequest_SavesAndReturnsExpense()
    {
        var repository = new StubCashFlowRepository();
        var service = new ExpenseService(repository);
        var request = ToCreateDto(ValidCreateRequest());

        var result = await service.AddExpenseAsync(request);

        result.Description.Should().Be("Weekly groceries");
        result.Value.Should().Be(54.32m);
        result.Category.Should().Be("Mercado");
        result.PaymentSource.Should().Be("Barclays");
        result.CardTag.Should().BeNull();
        repository.Expenses.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddExpenseAsync_WithCardTag_SavesCardTag()
    {
        var repository = new StubCashFlowRepository();
        var service = new ExpenseService(repository);
        var request = ValidCreateRequest() with { CardTag = "BarclaysPlatinumVisa8003" };

        var result = await service.AddExpenseAsync(ToCreateDto(request));

        result.CardTag.Should().Be("BarclaysPlatinumVisa8003");
    }

    [Fact]
    public async Task AddExpenseAsync_WithZeroValue_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { Value = 0m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*zero*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithMissingCategory_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { Category = "NotACategory" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Category*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidPaymentSource_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "NotASource" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidCardTag_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { CardTag = "NotACard" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Credit card*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithBlankDescription_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { Description = "  " });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Description is required*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithDescriptionOver200Characters_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());
        var request = ToCreateDto(ValidCreateRequest() with { Description = new string('a', 201) });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*200 characters*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithExistingId_UpdatesInPlace()
    {
        var repository = new StubCashFlowRepository();
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest()));

        var updateRequest = new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            Description = "Updated",
            Value = 10m,
            Category = "Casa",
            PaymentSource = "Chase",
            CardTag = null
        };
        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.Id.Should().Be(added.Id);
        result.Description.Should().Be("Updated");
        result.Category.Should().Be("Casa");
        repository.Expenses.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());
        var updateRequest = ToUpdateDto(ValidCreateRequest());

        var act = async () => await service.UpdateExpenseAsync(Guid.NewGuid(), updateRequest);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest()));

        await service.DeleteExpenseAsync(added.Id);

        repository.Expenses.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new ExpenseService(new StubCashFlowRepository());

        var act = async () => await service.DeleteExpenseAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetExpensesByMonth_ReturnsOnlyExpensesInThatMonth()
    {
        var repository = new StubCashFlowRepository();
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = service.GetExpensesByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_SumsValuesPerCategoryForThatMonth()
    {
        var repository = new StubCashFlowRepository();
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Mercado", Value = 10m }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Mercado", Value = 5m }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Casa", Value = 20m }));

        var result = service.GetCategoryTotalsByMonth(2026, 7);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 15m);
        result.Should().ContainSingle(t => t.Category == "Casa" && t.TotalValue == 20m);
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_NegativeValue_CountsTowardTotal()
    {
        var repository = new StubCashFlowRepository();
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Reserva", Value = 100m }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Reserva", Value = -30m }));

        var result = service.GetCategoryTotalsByMonth(2026, 7);

        result.Should().ContainSingle(t => t.Category == "Reserva" && t.TotalValue == 70m);
    }

    private static ExpenseCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 15),
        "Weekly groceries",
        54.32m,
        "Mercado",
        "Barclays",
        null);

    private static ExpenseCreateDTO ToCreateDto(ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        Category = r.Category,
        PaymentSource = r.PaymentSource,
        CardTag = r.CardTag
    };

    private static ExpenseUpdateDTO ToUpdateDto(ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        Category = r.Category,
        PaymentSource = r.PaymentSource,
        CardTag = r.CardTag
    };

    private sealed record ExpenseCreateRequest(
        DateOnly Date, string Description, decimal Value, string Category, string PaymentSource, string? CardTag);

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<Expense> Expenses { get; } = new();
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Expense> GetExpenses() => Expenses;
        public void AddExpense(Expense expense) => Expenses.Add(expense);
        public void DeleteExpense(Guid id) => Expenses.RemoveAll(e => e.Id == id);

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

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
