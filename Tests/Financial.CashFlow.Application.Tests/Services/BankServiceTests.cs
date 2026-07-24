using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class BankServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BankService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetBanks_MapsEveryRepositoryBankToADto()
    {
        var repository = new StubCashFlowRepository();
        repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        repository.Banks.Add(Bank.Create("Trading212", roundUpEnabled: true));
        var service = new BankService(repository);

        var result = service.GetBanks();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(b => b.Name == "Barclays" && !b.RoundUpEnabled);
        result.Should().ContainSingle(b => b.Name == "Trading212" && b.RoundUpEnabled);
    }

    [Fact]
    public void GetBanks_WithNoBanks_ReturnsEmptyList()
    {
        var service = new BankService(new StubCashFlowRepository());

        var result = service.GetBanks();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithValidRequest_UpdatesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        var service = new BankService(repository);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = 1250.75m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var result = await service.UpdateOpeningBalanceAsync("Barclays", request);

        result.OpeningBalance.Should().Be(1250.75m);
        result.OpeningBalanceDate.Should().Be(new DateOnly(2026, 7, 1));
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_ResolvesNameCaseInsensitively()
    {
        var repository = new StubCashFlowRepository();
        repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        var service = new BankService(repository);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = 10m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var result = await service.UpdateOpeningBalanceAsync("barclays", request);

        result.Name.Should().Be("Barclays");
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithUnknownName_ThrowsKeyNotFoundException()
    {
        var service = new BankService(new StubCashFlowRepository());
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = 10m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var act = async () => await service.UpdateOpeningBalanceAsync("NotABank", request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithNegativeBalance_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository();
        repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        var service = new BankService(repository);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = -1m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var act = async () => await service.UpdateOpeningBalanceAsync("Barclays", request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetBankBalancesByMonth_CombinesOpeningBalanceIncomeAndExpenses()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, null, 500m, "Barclays"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Mercado, "Barclays", null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 550m);
    }

    [Fact]
    public void GetBankBalancesByMonth_SubtractsRoundUpAmountFromExpenseValue()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Trading212", roundUpEnabled: true);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        var expense = Expense.Create(new DateOnly(2026, 7, 5), "TfL", 9.40m, Category.Extras, "Trading212", null);
        expense.SetRoundUpAmount(0.60m);
        repository.Expenses.Add(expense);
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == -8.80m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityBeforeOpeningBalanceDate()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 6, 30), IncomeSource.Gleison, null, 500m, "Barclays"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 6, 30), "Groceries", 50m, Category.Mercado, "Barclays", null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityAfterSelectedMonth()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), IncomeSource.Gleison, null, 500m, "Barclays"));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_WithNoActivity_ReturnsOpeningBalance()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(250m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 250m);
    }

    [Fact]
    public void GetBankBalancesByMonth_IgnoresActivityTaggedToADifferentBank()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, null, 500m, "Chase"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Mercado, "Chase", null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 0m);
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<Bank> Banks { get; } = new();
        public List<Expense> Expenses { get; } = new();
        public List<Income> Incomes { get; } = new();
        public int SaveChangesCallCount { get; private set; }

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

        public IEnumerable<Bank> GetBanks() => Banks;

        public IEnumerable<Income> GetIncomes() => Incomes;
        public void AddIncome(Income income) { }
        public void DeleteIncome(Guid id) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
