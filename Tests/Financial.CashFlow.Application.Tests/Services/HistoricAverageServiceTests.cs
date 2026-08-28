using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class HistoricAverageServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly Microsoft.Extensions.Logging.ILogger<HistoricAverageService> Logger = NullLogger<HistoricAverageService>.Instance;

    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);

    private static IncomeSource Source(string name) => IncomeSource.Create(name, IncomeGroup.NonReportable);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly HistoricAverageService _sut;

    public HistoricAverageServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private HistoricAverageService CreateService(StubCashFlowRepository? repository = null, TimeProvider? timeProvider = null) =>
        new(repository ?? _repository, _tracer, Logger, timeProvider);

    private static Category CategoryByName(StubCashFlowRepository repository, string name) =>
        repository.Categories.First(c => c.Name == name);

    // Pinned to a fixed mid-year date so "current year" averaging tests don't have to branch on
    // (or silently no-op during) the real wall-clock month - June guarantees 5 completed months.
    private static readonly DateTimeOffset PinnedNow = new(CurrentYear, 6, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PinnedToday = DateOnly.FromDateTime(PinnedNow.UtcDateTime);
    private const int PinnedMonthsElapsed = 5;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new HistoricAverageService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new HistoricAverageService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new HistoricAverageService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ReturnsEmptyList_WhenNoExpensesForSpecifiedYear()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2027, 4, 5), "Should not be there", 1000m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetHistoricSummaryAverageFromYear(2020);

        result.Count.Should().Be(0);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ReturnsYearsUpToAndIncludingSpecifiedYear()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2027, 4, 5), "Should not be there", 1000m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetHistoricSummaryAverageFromYear(2026);

        result.Count.Should().Be(1);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ReturnsTheYearsInOrderDescending()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Mar", 50m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 25m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 55m, CategoryByName(_repository, "Gleison"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 6, 5), "Jun", 120m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2023, 3, 5), "Mar", 52m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetHistoricSummaryAverageFromYear(2026);

        using (new AssertionScope())
        {
            result[0].Year.Should().Be(2026);
            result[1].Year.Should().Be(2025);
            result[2].Year.Should().Be(2023);
        }
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_AveragesCategoryValuesForFullYear()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Jan first", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 20), "Jan second", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 2, 10), "Feb", 400m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetHistoricSummaryAverageFromYear(2026);

        var mercadoAverage = result[0].AnnualAverages.Single(a => a.Category == "Mercado").Value;

        mercadoAverage.Should().Be(50m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_AveragesCategoryValuesFor2017Year()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Jan first", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 20), "Jan second", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 2, 10), "Feb", 400m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetHistoricSummaryAverageFromYear(2026);

        var mercadoAverage = result[0].AnnualAverages.Single(a => a.Category == "Mercado").Value;

        mercadoAverage.Should().Be(54.55m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_MergesIncomeAveragesIntoMatchingYearsInDescendingOrder()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(DateTime.UtcNow.Year + 1, 4, 5), "Should not be there", 10m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 4, 5), "2025", 10m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2023, 4, 5), "2023", 10m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(DateTime.UtcNow.Year+1, 4, 5), Source("Gleison"), 9999m, 9999m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 4, 5), Source("Gleison"), 1200m, 1200m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2023, 4, 5), Source("Gleison"), 900m, 900m, Barclays));

        var result = _sut.GetHistoricSummaryAverageFromYear(2026);

        using (new AssertionScope())
        {
            result.Count.Should().Be(2);
            result[0].Year.Should().Be(2025);
            result[1].Year.Should().Be(2023);
            result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(100m);
            result[1].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(75m);
        }
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_AveragesIncomePerMonthNotPerTransaction()
    {
        var service = CreateService(timeProvider: new FakeTimeProvider(PinnedNow));

        _repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, PinnedNow.Month, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, PinnedNow.Month - 1, 5), Source("Gleison"), 2400m, 900m, Barclays));

        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 20), Source("Gleison"), 500m, 400m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 2, 5), Source("Gleison"), 3000m, 2400m, Barclays));

        _repository.Incomes.Add(Income.Create(new DateOnly(2017, 7, 5), Source("Gleison"), 1100m, 110m, Barclays));

        var result = service.GetHistoricSummaryAverageFromYear(CurrentYear);

        // Only the completed month (May, one month before the pinned June "now") counts toward the
        // current year's average; the in-progress June entry is excluded entirely.
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(2400m / PinnedMonthsElapsed);
        result[0].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(900m / PinnedMonthsElapsed);

        result[1].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(375m);
        result[1].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(300m);

        result[2].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(100m);
        result[2].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(10m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_SumsIncomeSourcesPerMonthBeforeAveragingWhenActiveMonthsDiffer()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1000m, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 2, 5), Source("Gleison"), 1000m, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 3, 5), Source("Gleison"), 1000m, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Ariana"), 500m, 500m, Barclays));

        var result = _sut.GetHistoricSummaryAverageFromYear(2025);

        // Combined per-month salary: Jan 1500, Feb 1000, Mar 1000 → total 3500, averaged over all 12
        // months of a completed past year (NumberOfMonthsForAverage returns 12 for any non-current year).
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(Math.Round(3500m / 12m, HistoricAverageService.AverageDecimalPlaces));
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_IncludesYearsWithIncomeButNoExpenses()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1200m, 600m, Barclays));

        var result = _sut.GetHistoricSummaryAverageFromYear(2026);

        result.Count.Should().Be(1);
        result[0].Year.Should().Be(2025);
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(100m);
        result[0].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(50m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ZeroFillsIncomeRowsForAYearWithExpensesButNoIncome()
    {
        // Regression test: a year with expense/category data but zero recorded income (e.g. the
        // Incomes collection is empty) must still show Salary/Salary after taxes/Tax difference/
        // Dividendo-Juros as zero-value rows, not omit them entirely - mirroring how a category
        // with no expenses that year is zero-filled rather than dropped.
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetHistoricSummaryAverageFromYear(2025);

        using (new AssertionScope())
        {
            result.Should().ContainSingle(r => r.Year == 2025);
            var yearAverage = result.Single(r => r.Year == 2025);
            yearAverage.AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(0m);
            yearAverage.AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(0m);
            yearAverage.AnnualAverages.Single(a => a.Category == "Tax difference").Value.Should().Be(0m);
            yearAverage.AnnualAverages.Single(a => a.Category == "Dividendo/Juros").Value.Should().Be(0m);
        }
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ComputesTotalDespesasAsSumOfCategoryRowsOnly()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Investing", 30m, CategoryByName(_repository, "Investimento"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));

        var result = _sut.GetHistoricSummaryAverageFromYear(2025);

        // Total despesas must be the sum of the 14 expense category rows only (Mercado 100 + Investimento 30 = 130),
        // never the income rows (Salary/Salary after taxes/Tax difference/Dividendo/Juros) merged in ahead of them.
        result[0].AnnualAverages.Single(a => a.Category == "Total despesas").Value.Should().Be(10.83m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ComputesResultadoFromSalaryAfterTaxesTotalDespesasAndInvestimentoExcludingDividendoJuros()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Investing", 30m, CategoryByName(_repository, "Investimento"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        // DividendoJuros is seeded deliberately: unlike Category Totals' own Resultado, this sub-tab's
        // Resultado excludes Dividendo/Juros entirely, so this income must NOT affect the expected value.
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));

        var result = _sut.GetHistoricSummaryAverageFromYear(2025);

        // Resultado (R-D-Inv) = SalaryAfterTaxes(800/12) - TotalDespesas(130/12) + Investimento(30/12) = 700
        result[0].AnnualAverages.Single(a => a.Category == "Resultado (R-D-Inv)").Value.Should().Be(58.34m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_LotteryIncomeContributesToNoRow()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 120m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Lottery"), null, 500m, Barclays));

        var result = _sut.GetHistoricSummaryAverageFromYear(2025);

        // Lottery must not leak into Dividendo/Juros (or any other row) - it must have the same
        // effect as if it were never recorded at all.
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(83.33m);
        result[0].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(66.67m);
        result[0].AnnualAverages.Single(a => a.Category == "Dividendo/Juros").Value.Should().Be(1.67m);
        result[0].AnnualAverages.Single(a => a.Category == "Resultado (R-D-Inv)").Value.Should().Be(56.67m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_UnresolvedIncomeSource_DefaultsToNonReportableAndContributesToNoRow()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 120m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("NotASeededSource"), null, 500m, Barclays));

        var act = () => _sut.GetHistoricSummaryAverageFromYear(2025);

        // An unresolved source name must not leak into Dividendo/Juros (or any other row) and must
        // not throw - it must have the same effect as an income seeded to the NonReportable group.
        var result = act.Should().NotThrow().Which;
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(83.33m);
        result[0].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(66.67m);
        result[0].AnnualAverages.Single(a => a.Category == "Dividendo/Juros").Value.Should().Be(1.67m);
        result[0].AnnualAverages.Single(a => a.Category == "Resultado (R-D-Inv)").Value.Should().Be(56.67m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ZeroFillsCategoriesWithNoExpensesThatYear()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 1200m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetHistoricSummaryAverageFromYear(2025);

        // Every one of the 14 seeded categories must appear, even with zero recorded expenses that year.
        foreach (var category in _repository.Categories)
        {
            var entry = result[0].AnnualAverages.Single(a => a.Category == category.Name);
            entry.Value.Should().Be(category.Name == "Mercado" ? 100m : 0m);
        }
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_RowOrderMatchesCategoryTotalsFixedOrder()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));

        var result = _sut.GetHistoricSummaryAverageFromYear(2026);

        var expectedOrder = new List<string> { "Salary", "Salary after taxes", "Tax difference", "Dividendo/Juros" }
            .Concat(_repository.Categories.Select(c => c.Name))
            .Concat(["Resultado (R-D-Inv)", "Total despesas"]);

        result[0].AnnualAverages.Select(a => a.Category).Should().Equal(expectedOrder);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ExcludesInProgressCurrentMonthFromCurrentYearAverage()
    {
        var service = CreateService(timeProvider: new FakeTimeProvider(PinnedNow));
        _repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear, 1, 5), "Completed months", 100m * PinnedMonthsElapsed, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, 1, 5), Source("Gleison"), 1000m * PinnedMonthsElapsed, 800m * PinnedMonthsElapsed, Barclays));
        _repository.Incomes.Add(Income.Create(PinnedToday, Source("Gleison"), 9999m * PinnedMonthsElapsed, 9999m * PinnedMonthsElapsed, Barclays));

        var result = service.GetHistoricSummaryAverageFromYear(CurrentYear);

        var currentYearRow = result.Single(r => r.Year == CurrentYear);
        // Only the completed months' figures count; the in-progress current-month entries (9999)
        // must be excluded entirely, not treated as a completed month with a low value.
        using (new AssertionScope())
        {
            currentYearRow.AnnualAverages.Single(a => a.Category == "Mercado").Value.Should().Be(100m);
            currentYearRow.AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(1000m);
            currentYearRow.AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(800m);
        }
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_OmitsCurrentYearEntirelyWhenOnlyInProgressMonthIsRecorded()
    {
        var service = CreateService(timeProvider: new FakeTimeProvider(PinnedNow));
        _repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear - 1, 12, 5), "Prior year", 50m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = service.GetHistoricSummaryAverageFromYear(CurrentYear);

        // The current year has no fully completed month yet, so it must not appear at all;
        // the range starts at the previous year instead.
        result.Should().NotContain(r => r.Year == CurrentYear);
        result.Should().ContainSingle(r => r.Year == CurrentYear - 1);
    }

    private static StubCashFlowRepository CreateRepository()
    {
        var repository = new StubCashFlowRepository(seedDefaultIncomeSources: true, seedDefaultCategories: true);
        SeededInvestmentAccounts.SeedInto(repository);
        return repository;
    }
}
