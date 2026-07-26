using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class TitheServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new TitheService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetTitheSummary_CalculatesTenPercentOfMonthlyNetIncomeAcrossSources()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 8), IncomeSource.Ariana, null, 400m, "Chase"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 15), IncomeSource.DividendoJuros, null, 150m, "Trading212"));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
    }

    [Fact]
    public void GetTitheSummary_SubtractsDizimoExpensesFromCalculatedTithe()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, null, 3000m, "Barclays"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Category.Dizimo, "Barclays", null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_DizimoExceedingCalculatedTithe_ReturnsNegativeBalanceWithoutError()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, null, 1000m, "Barclays"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Category.Dizimo, "Barclays", null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(-100m);
    }

    [Fact]
    public void GetTitheSummary_NonDizimoExpenses_AreIgnored()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, null, 1000m, "Barclays"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Mercado, "Barclays", null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_ExcludesIncomeAndExpensesOutsideSelectedMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, null, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), IncomeSource.Gleison, null, 5000m, "Barclays"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "July tithe", 50m, Category.Dizimo, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 5), "August tithe", 500m, Category.Dizimo, "Barclays", null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(50m);
    }

    [Fact]
    public void GetTitheSummary_NoIncomeNoExpenses_ReturnsZeros()
    {
        var service = new TitheService(new StubCashFlowRepository());

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(0m);
        result.TitheBalance.Should().Be(0m);
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<Income> Incomes { get; } = new();
        public List<Expense> Expenses { get; } = new();
        public List<Bank> Banks { get; } = new();

        public IEnumerable<Expense> GetExpenses() => Expenses;
        public void AddExpense(Expense expense) => Expenses.Add(expense);
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
        public void AddIncome(Income income) => Incomes.Add(income);
        public void DeleteIncome(Guid id) { }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
