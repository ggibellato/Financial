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
    public async Task GetTitheSummaryAsync_CalculatesTenPercentOfMonthlyNetIncomeAcrossSources()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), 3200m, 2450m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 8), Source("Ariana"), null, 400m, Chase));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 15), Source("DividendoJuros"), null, 150m, Trading212));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_SubtractsDizimoExpensesFromCalculatedTithe()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 3000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Dizimo, Barclays, null));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(100m);
        result.CarryForward.Should().BeNull();
    }

    [Fact]
    public async Task GetTitheSummaryAsync_DizimoExceedingCalculatedTithe_ReturnsNegativeBalanceWithoutError()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Dizimo, Barclays, null));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(-100m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_NonDizimoExpenses_AreIgnored()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Mercado, Barclays, null));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_ExcludesIncomeAndExpensesOutsideSelectedMonth()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), Source("Gleison"), null, 5000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "July tithe", 50m, Dizimo, Barclays, null));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 5), "August tithe", 500m, Dizimo, Barclays, null));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(50m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_DizimoExpenseWithCountsAsTitheFalse_ExcludedFromDizimoTotal()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 3000m, Barclays));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 10), "Charitable offer", 200m, Dizimo, Barclays, null, countsAsTithe: false));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(300m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_DizimoExpenseWithCountsAsTitheTrue_IncludedInDizimoTotal()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 3000m, Barclays));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 10), "Tithe payment", 200m, Dizimo, Barclays, null, countsAsTithe: true));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_NonTitheCategoryExpenseWithCountsAsTitheFalse_StillIgnored()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 5), "Groceries", 50m, Mercado, Barclays, null, countsAsTithe: false));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_BankLessIncomeAndOfferExpenseTogether_ReflectsBothInSameMonth()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(
            new DateOnly(2026, 7, 15), Source("DividendoJuros"), null, 420m, null, "Chip ISA dividend"));
        _repository.Expenses.Add(Expense.Create(
            new DateOnly(2026, 7, 20), "Charitable offer", 30m, Dizimo, Barclays, null, countsAsTithe: false));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(142m);
        result.TitheBalance.Should().Be(142m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_IncludesBankLessIncomeInCalculatedTithe()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 15), Source("DividendoJuros"), null, 420m, null, "Chip ISA dividend"));

        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(142m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_NoIncomeNoExpenses_ReturnsZeros()
    {
        var result = await _sut.GetTitheSummaryAsync(2026, 7);

        result.CalculatedTithe.Should().Be(0m);
        result.TitheBalance.Should().Be(0m);
        result.CarryForward.Should().BeNull();
    }

    // --- Carry-forward: these use dates computed relative to "today" (like CardStatementServiceTests
    // does for DateTime.Today-dependent behavior), since TitheCarryForwardEffectiveFrom auto-anchors
    // to the real current month the first time it's read as unset.

    private static DateOnly ThisMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private static DateOnly NextMonth => ThisMonth.AddMonths(1);
    private static DateOnly MonthAfterNext => ThisMonth.AddMonths(2);

    [Fact]
    public async Task GetTitheSummaryAsync_LaunchMonth_NeverOffersCarryForwardRegardlessOfHistoricalData()
    {
        var lastMonth = ThisMonth.AddMonths(-1);
        _repository.Incomes.Add(Income.Create(new DateOnly(lastMonth.Year, lastMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));

        var result = await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);

        result.CarryForward.Should().BeNull();
    }

    [Fact]
    public async Task GetTitheSummaryAsync_NextMonth_CarriesInThisMonthsPositiveBalanceByDefault()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));

        // Establishes the effective-from anchor at "this month" first, matching how a real user
        // reaches "next month" only after the app has already been used this month.
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);

        var result = await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);

        result.CarryForward.Should().NotBeNull();
        result.CarryForward!.Amount.Should().Be(100m);
        result.CarryForward.Included.Should().BeTrue();
        result.CarryForward.FromYear.Should().Be(ThisMonth.Year);
        result.CarryForward.FromMonth.Should().Be(ThisMonth.Month);
        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_PredecessorZeroBalance_NoCarryForwardOffered()
    {
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);

        var result = await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);

        result.CarryForward.Should().BeNull();
    }

    [Fact]
    public async Task GetTitheSummaryAsync_PredecessorOverpaid_NoCarryForwardOffered()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 15), "Extra tithe", 500m, Dizimo, Barclays, null));
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);

        var result = await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);

        result.CarryForward.Should().BeNull();
    }

    [Fact]
    public async Task GetTitheSummaryAsync_CalculatedTithe_NeverIncludesCarriedAmount()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);
        _repository.Incomes.Add(Income.Create(new DateOnly(NextMonth.Year, NextMonth.Month, 1), Source("Gleison"), null, 2000m, Barclays));

        var result = await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);

        result.CalculatedTithe.Should().Be(200m);
        result.TitheBalance.Should().Be(300m);
    }

    [Fact]
    public async Task UpdateCarryForwardInclusionAsync_SetIncludedFalse_RemovesFromBalance()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);
        await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);

        var result = await _sut.UpdateCarryForwardInclusionAsync(NextMonth.Year, NextMonth.Month, false);

        result.CarryForward!.Included.Should().BeFalse();
        result.TitheBalance.Should().Be(0m);
    }

    [Fact]
    public async Task UpdateCarryForwardInclusionAsync_ReIncludeAfterExclude_RestoresOriginalSnapshotAmount()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);
        await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);
        await _sut.UpdateCarryForwardInclusionAsync(NextMonth.Year, NextMonth.Month, false);

        var result = await _sut.UpdateCarryForwardInclusionAsync(NextMonth.Year, NextMonth.Month, true);

        result.CarryForward!.Amount.Should().Be(100m);
        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_EditingResolvedSourceMonth_DoesNotChangeLaterSnapshottedCarry()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);
        await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);

        // Editing "this month" after "next month" already snapshotted its carry-in must not change it.
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 2), Source("Gleison"), null, 9000m, Barclays));

        var result = await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);

        result.CarryForward!.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryAsync_DeclinedCarryForward_NeverReoffersToALaterMonth()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);
        await _sut.GetTitheSummaryAsync(NextMonth.Year, NextMonth.Month);
        await _sut.UpdateCarryForwardInclusionAsync(NextMonth.Year, NextMonth.Month, false);

        var result = await _sut.GetTitheSummaryAsync(MonthAfterNext.Year, MonthAfterNext.Month);

        result.CarryForward.Should().BeNull();
    }

    [Fact]
    public async Task GetTitheSummaryAsync_CascadingUnresolvedChain_WalksBackCorrectly()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(ThisMonth.Year, ThisMonth.Month, 1), Source("Gleison"), null, 1000m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(NextMonth.Year, NextMonth.Month, 1), Source("Gleison"), null, 500m, Barclays));
        await _sut.GetTitheSummaryAsync(ThisMonth.Year, ThisMonth.Month);

        // Jump straight to the month after next without ever resolving "next month" directly.
        var result = await _sut.GetTitheSummaryAsync(MonthAfterNext.Year, MonthAfterNext.Month);

        // "next month": base 50 + carried 100 from "this month" = 150, all unpaid and included by default.
        result.CarryForward!.Amount.Should().Be(150m);
        result.TitheBalance.Should().Be(150m);
    }

    [Fact]
    public async Task UpdateCarryForwardInclusionAsync_NoRecordForMonth_ThrowsArgumentException()
    {
        var act = async () => await _sut.UpdateCarryForwardInclusionAsync(ThisMonth.Year, ThisMonth.Month, false);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task UpdateCarryForwardInclusionAsync_MonthOutOfRange_ThrowsArgumentException(int month)
    {
        var act = async () => await _sut.UpdateCarryForwardInclusionAsync(2026, month, false);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new TitheService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
