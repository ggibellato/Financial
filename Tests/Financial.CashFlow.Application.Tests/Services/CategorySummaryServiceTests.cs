using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CategorySummaryServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly Microsoft.Extensions.Logging.ILogger<CategorySummaryService> Logger = NullLogger<CategorySummaryService>.Instance;
    private static readonly Microsoft.Extensions.Logging.ILogger<IncomeSummaryService> IncomeLogger = NullLogger<IncomeSummaryService>.Instance;

    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly CreditCard BarclaysPlatinumVisa8003 = CreditCard.Create("BarclaysPlatinumVisa8003");
    private static readonly CreditCard BaAmex = CreditCard.Create("BaAmex");

    private static IncomeSource Source(string name) => IncomeSource.Create(name, IncomeGroup.NonReportable);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CategorySummaryService _sut;

    public CategorySummaryServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository or dependency does not repeat the whole construction sequence. The
    /// internal IIncomeSummaryService collaborator gets its own throwaway tracer, so a composite
    /// call's spans stay attributable to CategorySummaryService alone in _tracer.Spans - matching
    /// what the single pre-split class recorded.</summary>
    private CategorySummaryService CreateService(StubCashFlowRepository? repository = null, TimeProvider? timeProvider = null)
    {
        var repo = repository ?? _repository;
        var incomeSummaryService = new IncomeSummaryService(repo, new RecordingTelemetryTracer(), IncomeLogger, timeProvider);
        return new(repo, incomeSummaryService, _tracer, Logger, timeProvider);
    }

    private static IIncomeSummaryService CreateIncomeSummaryService(StubCashFlowRepository repository) =>
        new IncomeSummaryService(repository, new RecordingTelemetryTracer(), IncomeLogger);

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
        Action act = () => new CategorySummaryService(null!, CreateIncomeSummaryService(_repository), _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullIncomeSummaryService_Throws()
    {
        Action act = () => new CategorySummaryService(_repository, null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("incomeSummaryService");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CategorySummaryService(_repository, CreateIncomeSummaryService(_repository), null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CategorySummaryService(_repository, CreateIncomeSummaryService(_repository), _tracer, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_RecordsSuccessfulSpan()
    {
        _sut.GetCategoryTotalsAnnualForYear(2026);

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.CategorySummaryService.GetCategoryTotalsAnnualForYear");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("CashFlow");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("AnnualSummary");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public void GetCategoryTotalsForYear_ReturnsAllFourteenCategories()
    {
        var result = _sut.GetCategoryTotalsForYear(2026);

        result.Should().HaveCount(_repository.Categories.Count);
    }

    [Fact]
    public void GetCategoryTotalsForYear_AnnualTotalEqualsSumOfMonthlyTotals()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Mar", 50m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 25m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetCategoryTotalsForYear(2026);

        var mercado = result.Single(c => c.Category == "Mercado");
        mercado.MonthlyTotals[0].Should().Be(100m);
        mercado.MonthlyTotals[2].Should().Be(50m);
        mercado.MonthlyTotals[11].Should().Be(25m);
        mercado.AnnualTotal.Should().Be(mercado.MonthlyTotals.Sum());
        mercado.AnnualTotal.Should().Be(175m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_ExcludesExpensesFromOtherYears()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Last year", 999m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetCategoryTotalsForYear(2026);

        result.Single(c => c.Category == "Mercado").AnnualTotal.Should().Be(0m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_DeactivatedCategory_StillIncludesItsHistoricalTotal()
    {
        var deactivatedCategory = Category.Create("RetiredCategory", isActive: false);
        _repository.Categories.Add(deactivatedCategory);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Old category spend", 75m, deactivatedCategory, Barclays, null));

        var result = _sut.GetCategoryTotalsForYear(2026);

        result.Should().ContainSingle(c => c.Category == "RetiredCategory" && c.AnnualTotal == 75m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_CategoryWithNoExpenses_ReturnsAllZeroMonthsAndZeroAnnualTotal()
    {
        var result = _sut.GetCategoryTotalsForYear(2026);

        var estudo = result.Single(c => c.Category == "Estudo");
        estudo.MonthlyTotals.Should().OnlyContain(v => v == 0m);
        estudo.AnnualTotal.Should().Be(0m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_UnpaidCardCharge_CountsTowardInvoiceMonthNotChargeMonth()
    {
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 29), "Cutoff charge", 40m, CategoryByName(_repository, "Mercado"), null,
           BarclaysPlatinumVisa8003, new DateOnly(2026, 8, 1)));

        var result = _sut.GetCategoryTotalsForYear(2026);

        var mercado = result.Single(c => c.Category == "Mercado");
        using (new AssertionScope())
        {
            mercado.MonthlyTotals[6].Should().Be(0m);
            mercado.MonthlyTotals[7].Should().Be(40m);
        }
    }

    [Fact]
    public void GetCategoryTotalsForYear_SettledCardCharge_CountsTowardPostSettlementDateMonth()
    {
        var settled = Expense.Create(new DateOnly(2026, 7, 10), "Settled charge", 40m, CategoryByName(_repository, "Mercado"), null, BarclaysPlatinumVisa8003);
        settled.Settle(Trading212, new DateOnly(2026, 8, 3));
        _repository.Expenses.Add(settled);

        var result = _sut.GetCategoryTotalsForYear(2026);

        var mercado = result.Single(c => c.Category == "Mercado");
        using (new AssertionScope())
        {
            mercado.MonthlyTotals[6].Should().Be(0m);
            mercado.MonthlyTotals[7].Should().Be(40m);
        }
    }

    [Fact]
    public void GetCategoryTotalsForYear_MixOfUnpaidSettledAndBank_NoExpenseCountedInMoreThanOneMonth()
    {
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 29), "Unpaid cutoff", 10m, CategoryByName(_repository, "Mercado"), null,
           BarclaysPlatinumVisa8003, new DateOnly(2026, 8, 1)));
        var settled = Expense.Create(new DateOnly(2026, 7, 12), "Settled", 20m, CategoryByName(_repository, "Mercado"), null, BaAmex);
        settled.Settle(Trading212, new DateOnly(2026, 7, 20));
        _repository.Expenses.Add(settled);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 15), "Bank", 30m, CategoryByName(_repository, "Mercado"), Chase, null));

        var result = _sut.GetCategoryTotalsForYear(2026);

        var mercado = result.Single(c => c.Category == "Mercado");
        using (new AssertionScope())
        {
            mercado.MonthlyTotals[6].Should().Be(50m);
            mercado.MonthlyTotals[7].Should().Be(10m);
            mercado.AnnualTotal.Should().Be(60m);
        }
    }

    [Fact]
    public void GetCategoryTotalsForYear_DecemberChargeInvoicedInJanuary_CountsTowardFollowingYearNotChargeYear()
    {
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2025, 12, 30), "Year-end cutoff", 40m, CategoryByName(_repository, "Mercado"), null,
           BarclaysPlatinumVisa8003, new DateOnly(2026, 1, 1)));

        var resultFor2025 = _sut.GetCategoryTotalsForYear(2025);
        var resultFor2026 = _sut.GetCategoryTotalsForYear(2026);

        using (new AssertionScope())
        {
            resultFor2025.Single(c => c.Category == "Mercado").AnnualTotal.Should().Be(0m);
            resultFor2026.Single(c => c.Category == "Mercado").MonthlyTotals[0].Should().Be(40m);
        }
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_InvestmentTotalResolvesViaIsInvestmentFlagNotCategoryName()
    {
        // Uses a category named nothing like "Investimento" to prove the Resultado calculation
        // resolves the investment series via the IsInvestment flag, not a hardcoded name lookup.
        // Replaces (rather than adds to) the seeded "Investimento" category, since only one
        // category is ever expected to carry the flag at a time.
        _repository.Categories.RemoveAll(c => c.Name == "Investimento");
        var customInvestmentCategory = Category.Create("MinhasAcoes", isInvestment: true);
        _repository.Categories.Add(customInvestmentCategory);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Stock purchase", 40m, customInvestmentCategory, Barclays, null));

        var result = _sut.GetCategoryTotalsAnnualForYear(2026);

        // No income, 40 in total despesas (the investment expense counts toward despesas too),
        // 40 in the investment series itself: Resultado = 0 - 40 + 40 = 0. A name-based lookup
        // would find no expenses under "Investimento" and yield -40 instead.
        result.ResultadoMonthly[0].Should().Be(0m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasMonthlyEqualsSumOfAllCategoriesPerMonth()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Investing", 30m, CategoryByName(_repository, "Investimento"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 2, 5), "Groceries", 50m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetCategoryTotalsAnnualForYear(2026);

        using (new AssertionScope())
        {
            result.TotalDespesasMonthly[0].Should().Be(130m);
            result.TotalDespesasMonthly[1].Should().Be(50m);
            result.TotalDespesasMonthly.Skip(2).Should().OnlyContain(v => v == 0m);
        }
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_ResultadoMonthlyExcludesDividendoJurosAndIncludesInvestimento()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Investing", 30m, CategoryByName(_repository, "Investimento"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        // DividendoJuros is seeded deliberately: the corrected formula must exclude it entirely.
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));

        var result = _sut.GetCategoryTotalsAnnualForYear(2026);

        // Resultado = SalaryAfterTaxes(800) - TotalDespesas(130) + Investimento(30) = 700, not 720.
        result.ResultadoMonthly[0].Should().Be(700m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_AnnualTotalsEqualSumOfMonthlyValues()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 6, 5), "Investing", 30m, CategoryByName(_repository, "Investimento"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 5), Source("Gleison"), 1000m, 800m, Barclays));

        var result = _sut.GetCategoryTotalsAnnualForYear(2026);

        result.TotalDespesasAnnualTotal.Should().Be(result.TotalDespesasMonthly.Sum());
        result.ResultadoAnnualTotal.Should().Be(result.ResultadoMonthly.Sum());
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_NoRecordedData_ReturnsAllZeroSeries()
    {
        var result = _sut.GetCategoryTotalsAnnualForYear(2026);

        using (new AssertionScope())
        {
            result.CategoryTotals.Should().HaveCount(_repository.Categories.Count)
                .And.OnlyContain(c => c.AnnualTotal == 0m);
            result.IncomeSummary.SalaryAnnualTotal.Should().Be(0m);
            result.TotalDespesasMonthly.Should().OnlyContain(v => v == 0m);
            result.TotalDespesasAnnualTotal.Should().Be(0m);
            result.ResultadoMonthly.Should().OnlyContain(v => v == 0m);
            result.ResultadoAnnualTotal.Should().Be(0m);
        }
    }

    [Fact]
    public void GetCategoryTotalsForYear_AverageDividesByTwelveForAnOrdinaryYear()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Jan first", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 20), "Jan second", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 2, 10), "Feb", 400m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetCategoryTotalsForYear(2025);

        result.Single(c => c.Category == "Mercado").Average.Should().Be(50m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_AverageDividesByElevenFor2017()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Jan first", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 20), "Jan second", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 2, 10), "Feb", 400m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = _sut.GetCategoryTotalsForYear(2017);

        result.Single(c => c.Category == "Mercado").Average.Should().Be(54.55m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_AverageForCurrentYearExcludesInProgressMonth()
    {
        var service = CreateService(timeProvider: new FakeTimeProvider(PinnedNow));
        _repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear, 1, 5), "Completed months", 100m * PinnedMonthsElapsed, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, CategoryByName(_repository, "Mercado"), Barclays, null));

        var result = service.GetCategoryTotalsForYear(CurrentYear);

        // The in-progress current-month entry (9999) must be excluded entirely from the average,
        // not treated as a completed month with a low value.
        result.Single(c => c.Category == "Mercado").Average.Should().Be(100m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasAndResultadoAveragesDivideByTwelveForAnOrdinaryYear()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Investing", 30m, CategoryByName(_repository, "Investimento"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));

        var result = _sut.GetCategoryTotalsAnnualForYear(2025);

        // Total despesas: (Mercado 100 + Investimento 30) / 12 = 10.83.
        result.TotalDespesasAverage.Should().Be(10.83m);
        // Resultado: the per-month series (SalaryAfterTaxes 800 - TotalDespesas 130 + Investimento 30 = 700
        // in January, zero elsewhere) is averaged as a whole, i.e. 700 / 12 = 58.33.
        result.ResultadoAverage.Should().Be(58.33m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasAndResultadoAveragesDivideByElevenFor2017()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Investing", 30m, CategoryByName(_repository, "Investimento"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2017, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2017, 1, 5), Source("DividendoJuros"), null, 20m, Barclays));

        var result = _sut.GetCategoryTotalsAnnualForYear(2017);

        result.TotalDespesasAverage.Should().Be(11.82m);
        result.ResultadoAverage.Should().Be(63.64m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasAndResultadoAveragesForCurrentYearExcludeInProgressMonth()
    {
        var service = CreateService(timeProvider: new FakeTimeProvider(PinnedNow));
        _repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear, 1, 5), "Completed months", 100m * PinnedMonthsElapsed, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, 1, 5), Source("Gleison"), 1000m * PinnedMonthsElapsed, 800m * PinnedMonthsElapsed, Barclays));
        _repository.Incomes.Add(Income.Create(PinnedToday, Source("Gleison"), 9999m * PinnedMonthsElapsed, 9999m * PinnedMonthsElapsed, Barclays));

        var result = service.GetCategoryTotalsAnnualForYear(CurrentYear);

        result.TotalDespesasAverage.Should().Be(100m);
        result.ResultadoAverage.Should().Be(700m);
    }

    private static StubCashFlowRepository CreateRepository()
    {
        var repository = new StubCashFlowRepository(seedDefaultIncomeSources: true, seedDefaultCategories: true);
        SeededInvestmentAccounts.SeedInto(repository);
        return repository;
    }
}
