using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class BalanceAdjustmentServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BalanceAdjustmentService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithNoPriorActivity_ComputesDeltaAgainstOpeningBalance()
    {
        var repository = new StubCashFlowRepository();
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository);

        var result = await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m,
            Note = "Matched statement"
        });

        using (new AssertionScope())
        {
            result.Bank.Should().Be("Barclays");
            result.TargetBalance.Should().Be(150m);
            result.Delta.Should().Be(50m);
            result.Note.Should().Be("Matched statement");
            repository.BalanceAdjustments.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithPriorIncomesAndExpenses_ComputesCorrectDelta()
    {
        var repository = new StubCashFlowRepository();
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Lottery, null, 200m, "Barclays"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Mercado, "Barclays", null));
        var service = new BalanceAdjustmentService(repository);

        var result = await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 240m
        });

        // Balance as of date = 100 (opening) + 200 (income) - 50 (expense) = 250; delta = 240 - 250 = -10
        result.Delta.Should().Be(-10m);
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithExistingAdjustmentForSameBank_StacksDelta()
    {
        var repository = new StubCashFlowRepository();
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository);
        var first = await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 6, 1),
            TargetBalance = 150m
        });
        first.Delta.Should().Be(50m);

        // Balance as of the second date = 100 (opening) + 50 (first adjustment's delta) = 150; target 130 => delta -20
        var second = await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 130m
        });

        second.Delta.Should().Be(-20m);
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithUnresolvableBank_ThrowsArgumentException()
    {
        var service = new BalanceAdjustmentService(new StubCashFlowRepository());

        var act = async () => await service.AddAdjustmentAsync("NotABank", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Bank 'NotABank' was not found*");
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithNegativeTargetBalance_ThrowsArgumentException()
    {
        var service = new BalanceAdjustmentService(new StubCashFlowRepository());

        var act = async () => await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = -1m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithExistingId_RecomputesAndPersistsDelta()
    {
        var repository = new StubCashFlowRepository();
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository);
        var added = await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });
        added.Delta.Should().Be(50m);

        var result = await service.UpdateAdjustmentAsync("Barclays", added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 120m,
            Note = "Corrected"
        });

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.TargetBalance.Should().Be(120m);
            result.Delta.Should().Be(20m);
            result.Note.Should().Be("Corrected");
            repository.BalanceAdjustments.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new BalanceAdjustmentService(new StubCashFlowRepository());

        var act = async () => await service.UpdateAdjustmentAsync("Barclays", Guid.NewGuid(), new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository);
        var added = await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });

        await service.DeleteAdjustmentAsync("Barclays", added.Id);

        repository.BalanceAdjustments.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new BalanceAdjustmentService(new StubCashFlowRepository());

        var act = async () => await service.DeleteAdjustmentAsync("Barclays", Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetAdjustmentsByBank_ReturnsOnlyThatBanksAdjustments()
    {
        var repository = new StubCashFlowRepository();
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        repository.SetOpeningBalance("Chase", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository);
        await service.AddAdjustmentAsync("Barclays", new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 7, 1), TargetBalance = 150m });
        await service.AddAdjustmentAsync("Chase", new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 7, 1), TargetBalance = 120m });

        var result = service.GetAdjustmentsByBank("Barclays");

        result.Should().ContainSingle().Which.Bank.Should().Be("Barclays");
    }

    [Fact]
    public void GetAdjustmentsByBank_WithUnrecognizedBank_ReturnsEmptyList()
    {
        var service = new BalanceAdjustmentService(new StubCashFlowRepository());

        var result = service.GetAdjustmentsByBank("NotABank");

        result.Should().BeEmpty();
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<BalanceAdjustment> BalanceAdjustments { get; } = new();
        public List<Income> Incomes { get; } = new();
        public List<Expense> Expenses { get; } = new();
        public List<Bank> Banks { get; } = new()
        {
            Bank.Create("Barclays", roundUpEnabled: false),
            Bank.Create("Trading212", roundUpEnabled: true),
            Bank.Create("Chase", roundUpEnabled: true)
        };
        public int SaveChangesCallCount { get; private set; }

        public void SetOpeningBalance(string bankName, decimal openingBalance, DateOnly openingBalanceDate) =>
            Banks.First(b => b.Name == bankName).SetOpeningBalance(openingBalance, openingBalanceDate);

        public IEnumerable<Expense> GetExpenses() => Expenses;
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

        public IEnumerable<InvestmentAccount> GetInvestmentAccounts() => Array.Empty<InvestmentAccount>();
        public void AddInvestmentAccount(InvestmentAccount account) { }

        public IEnumerable<Bank> GetBanks() => Banks;

        public IEnumerable<Income> GetIncomes() => Incomes;
        public void AddIncome(Income income) { }
        public void DeleteIncome(Guid id) { }

        public IEnumerable<Transfer> GetTransfers() => Array.Empty<Transfer>();
        public void AddTransfer(Transfer transfer) { }
        public void UpdateTransfer(Transfer transfer) { }
        public void DeleteTransfer(Guid id) { }

        public IEnumerable<BalanceAdjustment> GetBalanceAdjustments() => BalanceAdjustments;
        public void AddBalanceAdjustment(BalanceAdjustment adjustment) => BalanceAdjustments.Add(adjustment);
        public void UpdateBalanceAdjustment(BalanceAdjustment adjustment)
        {
            var index = BalanceAdjustments.FindIndex(a => a.Id == adjustment.Id);
            if (index >= 0)
            {
                BalanceAdjustments[index] = adjustment;
            }
        }
        public void DeleteBalanceAdjustment(Guid id) => BalanceAdjustments.RemoveAll(a => a.Id == id);

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
