using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class YearlySummaryServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new YearlySummaryService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetCategoryTotalsForYear_ReturnsAllFourteenCategories()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

        result.Should().HaveCount(Enum.GetValues<Category>().Length);
    }

    [Fact]
    public void GetCategoryTotalsForYear_YearlyTotalEqualsSumOfMonthlyTotals()
    {
        var repository = new StubCashFlowRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, PaymentSource.Barclays, null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Mar", 50m, Category.Mercado, PaymentSource.Barclays, null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 25m, Category.Mercado, PaymentSource.Barclays, null));
        var service = new YearlySummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

        var mercado = result.Single(c => c.Category == "Mercado");
        mercado.MonthlyTotals[0].Should().Be(100m);
        mercado.MonthlyTotals[2].Should().Be(50m);
        mercado.MonthlyTotals[11].Should().Be(25m);
        mercado.YearlyTotal.Should().Be(mercado.MonthlyTotals.Sum());
        mercado.YearlyTotal.Should().Be(175m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_ExcludesExpensesFromOtherYears()
    {
        var repository = new StubCashFlowRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Last year", 999m, Category.Mercado, PaymentSource.Barclays, null));
        var service = new YearlySummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

        result.Single(c => c.Category == "Mercado").YearlyTotal.Should().Be(0m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_CategoryWithNoExpenses_ReturnsAllZeroMonthsAndZeroYearlyTotal()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

        var estudo = result.Single(c => c.Category == "Estudo");
        estudo.MonthlyTotals.Should().OnlyContain(v => v == 0m);
        estudo.YearlyTotal.Should().Be(0m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_ReturnsAllElevenAccounts()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2026);

        result.Accounts.Should().HaveCount(Enum.GetValues<InvestmentAccount>().Length);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_MonthlyDiffsEqualThisMonthMinusPrevMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 2, 1200m));
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 3, 1100m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2026);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs.Should().HaveCount(11);
        chaseSave.MonthlyDiffs[0].Should().Be(200m);
        chaseSave.MonthlyDiffs[1].Should().Be(-100m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_MissingSnapshotForAMonth_ContributesZero()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 1, 500m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2026);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyValues[1].Should().Be(0m);
        chaseSave.MonthlyDiffs[0].Should().Be(-500m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_NetPositionSubtractsLiabilitiesFromAssets()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.PlatinumVisa8003, 2026, 1, 300m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2026);

        result.NetPosition.MonthlyValues[0].Should().Be(700m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_FullYearNetChangeEqualsDecemberMinusJanuary()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 12, 1800m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2026);

        result.NetPosition.FullYearNetChange.Should().Be(800m);
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<Expense> Expenses { get; } = new();
        public List<InvestmentSnapshot> Snapshots { get; } = new();

        public IEnumerable<Expense> GetExpenses() => Expenses;
        public void AddExpense(Expense expense) => Expenses.Add(expense);
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBillTemplate> GetRecurringBillTemplates() => Array.Empty<RecurringBillTemplate>();
        public void AddRecurringBillTemplate(RecurringBillTemplate template) { }

        public IEnumerable<RecurringBillInstance> GetRecurringBillInstances() => Array.Empty<RecurringBillInstance>();
        public void AddRecurringBillInstance(RecurringBillInstance instance) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Snapshots;
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => Snapshots.Add(snapshot);

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
