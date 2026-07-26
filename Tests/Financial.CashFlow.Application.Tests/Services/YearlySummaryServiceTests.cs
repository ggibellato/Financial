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
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Mar", 50m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 25m, Category.Mercado, "Barclays", null));
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
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Last year", 999m, Category.Mercado, "Barclays", null));
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

        result.Accounts.Should().HaveCount(repository.Accounts.Count);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_MonthlyDiffsEqualThisMonthMinusPrevMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", 2026, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", 2026, 2, 1200m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", 2026, 3, 1100m));
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
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", 2026, 1, 500m));
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
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", 2026, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", 2026, 1, 300m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2026);

        result.NetPosition.MonthlyValues[0].Should().Be(700m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_FullYearNetChangeEqualsDecemberMinusJanuary()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", 2026, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", 2026, 12, 1800m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2026);

        result.NetPosition.FullYearNetChange.Should().Be(800m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_SalaryRowSumsGleisonAndArianaGrossValuesPerMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 8), IncomeSource.Ariana, 400m, 350m, "Chase"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 2, 1), IncomeSource.Gleison, 3300m, 2500m, "Barclays"));
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly[0].Should().Be(3600m);
        result.SalaryMonthly[1].Should().Be(3300m);
        result.SalaryYearlyTotal.Should().Be(result.SalaryMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_SalaryAfterTaxesRowSumsGleisonAndArianaNetValuesPerMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 8), IncomeSource.Ariana, 400m, 350m, "Chase"));
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryAfterTaxesMonthly[0].Should().Be(2800m);
        result.SalaryAfterTaxesYearlyTotal.Should().Be(result.SalaryAfterTaxesMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_TaxDifferenceRowEqualsSalaryMinusSalaryAfterTaxes()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.TaxDifferenceMonthly[0].Should().Be(750m);
        result.TaxDifferenceYearlyTotal.Should().Be(result.TaxDifferenceMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_DividendoJurosRowSumsOnlyThatSourcesNetValues()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 1), IncomeSource.DividendoJuros, null, 15.50m, "Trading212"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 5), IncomeSource.DividendoJuros, null, 4.50m, "Trading212"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 10), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.DividendoJurosMonthly[2].Should().Be(20m);
        result.DividendoJurosYearlyTotal.Should().Be(20m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_LotteryEntriesContributeToNoRow()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 4, 1), IncomeSource.Lottery, null, 500m, "Chase"));
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly.Should().OnlyContain(v => v == 0m);
        result.SalaryAfterTaxesMonthly.Should().OnlyContain(v => v == 0m);
        result.DividendoJurosMonthly.Should().OnlyContain(v => v == 0m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_EntryWithNullGrossValue_ContributesZeroToSalary()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 5, 1), IncomeSource.Ariana, null, 350m, "Chase"));
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly[4].Should().Be(0m);
        result.SalaryAfterTaxesMonthly[4].Should().Be(350m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_ExcludesIncomeFromOtherYears()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryYearlyTotal.Should().Be(0m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_WithNoIncome_ReturnsAllZeros()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly.Should().OnlyContain(v => v == 0m);
        result.SalaryAfterTaxesMonthly.Should().OnlyContain(v => v == 0m);
        result.TaxDifferenceMonthly.Should().OnlyContain(v => v == 0m);
        result.DividendoJurosMonthly.Should().OnlyContain(v => v == 0m);
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        private static readonly (string Name, bool IsLiability)[] SeededAccounts =
        [
            ("BlueRewardsSaver", false),
            ("PlatinumVisa8003", true),
            ("PlatinumVisa6007", true),
            ("ChaseMaster4023", true),
            ("BaAmex", true),
            ("PaypalCredit", true),
            ("ChipCashIsaGleison", false),
            ("ChaseSave", false),
            ("ChipCashIsaAriana", false),
            ("Trading212Invested", false),
            ("ReservasPessoais", true)
        ];

        public List<Expense> Expenses { get; } = new();
        public List<InvestmentSnapshot> Snapshots { get; } = new();
        public List<InvestmentAccount> Accounts { get; } =
            SeededAccounts.Select(a => InvestmentAccount.Create(a.Name, isActive: true, isLiability: a.IsLiability)).ToList();
        public List<Income> Incomes { get; } = new();

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

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Snapshots;
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => Snapshots.Add(snapshot);

        public IEnumerable<InvestmentAccount> GetInvestmentAccounts() => Accounts;
        public void AddInvestmentAccount(InvestmentAccount account) => Accounts.Add(account);

        public IEnumerable<Bank> GetBanks() => Array.Empty<Bank>();

        public IEnumerable<Income> GetIncomes() => Incomes;
        public void AddIncome(Income income) { }
        public void DeleteIncome(Guid id) { }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
