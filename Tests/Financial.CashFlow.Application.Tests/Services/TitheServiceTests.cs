using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class TitheServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<TitheService> Logger = NullLogger<TitheService>.Instance;
    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly Category Dizimo = Category.Create("Dizimo", isTithe: true);
    private static readonly Category Mercado = Category.Create("Mercado");

    private static IncomeSource Source(string name) => IncomeSource.Create(name, IncomeGroup.NonReportable);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly TitheService _sut;

    public TitheServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository does not repeat the whole construction sequence.</summary>
    private TitheService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new TitheService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new TitheService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetTitheSummary_CalculatesTenPercentOfMonthlyNetIncomeAcrossSources()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), 3200m, 2450m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 8), Source("Ariana"), null, 400m, Chase));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 15), Source("DividendoJuros"), null, 150m, Trading212));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
    }

    [Fact]
    public void GetTitheSummary_SubtractsDizimoExpensesFromCalculatedTithe()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 3000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Dizimo, Barclays, null));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_DizimoExceedingCalculatedTithe_ReturnsNegativeBalanceWithoutError()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Dizimo, Barclays, null));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(-100m);
    }

    [Fact]
    public void GetTitheSummary_NonDizimoExpenses_AreIgnored()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Mercado, Barclays, null));

        var result = _sut.GetTitheSummary(2026, 7);

        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_ExcludesIncomeAndExpensesOutsideSelectedMonth()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), Source("Gleison"), null, 5000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "July tithe", 50m, Dizimo, Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 5), "August tithe", 500m, Dizimo, Barclays, null));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(50m);
    }

    [Fact]
    public void GetTitheSummary_DizimoExpenseWithCountsAsTitheFalse_ExcludedFromDizimoTotal()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 3000m, Barclays));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 10), "Charitable offer", 200m, Dizimo, Barclays, null, countsAsTithe: false));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(300m);
    }

    [Fact]
    public void GetTitheSummary_DizimoExpenseWithCountsAsTitheTrue_IncludedInDizimoTotal()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 3000m, Barclays));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 10), "Tithe payment", 200m, Dizimo, Barclays, null, countsAsTithe: true));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_NonTitheCategoryExpenseWithCountsAsTitheFalse_StillIgnored()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 5), "Groceries", 50m, Mercado, Barclays, null, countsAsTithe: false));

        var result = _sut.GetTitheSummary(2026, 7);

        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_BankLessIncomeAndOfferExpenseTogether_ReflectsBothInSameMonth()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(
            new DateOnly(2026, 7, 15), Source("DividendoJuros"), null, 420m, null, "Chip ISA dividend"));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 20), "Charitable offer", 30m, Dizimo, Barclays, null, countsAsTithe: false));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(142m);
        result.TitheBalance.Should().Be(142m);
    }

    [Fact]
    public void GetTitheSummary_IncludesBankLessIncomeInCalculatedTithe()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 15), Source("DividendoJuros"), null, 420m, null, "Chip ISA dividend"));

        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(142m);
    }

    [Fact]
    public void GetTitheSummary_NoIncomeNoExpenses_ReturnsZeros()
    {
        var result = _sut.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(0m);
        result.TitheBalance.Should().Be(0m);
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new TitheService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
