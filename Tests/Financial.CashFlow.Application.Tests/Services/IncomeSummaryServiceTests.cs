using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class IncomeSummaryServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly Microsoft.Extensions.Logging.ILogger<IncomeSummaryService> Logger = NullLogger<IncomeSummaryService>.Instance;

    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);

    private static IncomeSource Source(string name) => IncomeSource.Create(name, IncomeGroup.NonReportable);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly IncomeSummaryService _sut;

    public IncomeSummaryServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository or dependency does not repeat the whole construction sequence.</summary>
    private IncomeSummaryService CreateService(StubCashFlowRepository? repository = null, TimeProvider? timeProvider = null) =>
        new(repository ?? _repository, _tracer, Logger, timeProvider);

    // Pinned to a fixed mid-year date so "current year" averaging tests don't have to branch on
    // (or silently no-op during) the real wall-clock month - June guarantees 5 completed months.
    private static readonly DateTimeOffset PinnedNow = new(CurrentYear, 6, 15, 0, 0, 0, TimeSpan.Zero);
    private const int PinnedMonthsElapsed = 5;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new IncomeSummaryService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new IncomeSummaryService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new IncomeSummaryService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetIncomeSummaryForYear_SalaryRowSumsGleisonAndArianaGrossValuesPerMonth()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), Source("Gleison"), 3200m, 2450m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 8), Source("Ariana"), 400m, 350m, Chase));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 2, 1), Source("Gleison"), 3300m, 2500m, Barclays));

        var result = _sut.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly[0].Should().Be(3600m);
        result.SalaryMonthly[1].Should().Be(3300m);
        result.SalaryAnnualTotal.Should().Be(result.SalaryMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_SalaryAfterTaxesRowSumsGleisonAndArianaNetValuesPerMonth()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), Source("Gleison"), 3200m, 2450m, Barclays));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 8), Source("Ariana"), 400m, 350m, Chase));

        var result = _sut.GetIncomeSummaryForYear(2026);

        result.SalaryAfterTaxesMonthly[0].Should().Be(2800m);
        result.SalaryAfterTaxesAnnualTotal.Should().Be(result.SalaryAfterTaxesMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_TaxDifferenceRowEqualsSalaryMinusSalaryAfterTaxes()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), Source("Gleison"), 3200m, 2450m, Barclays));

        var result = _sut.GetIncomeSummaryForYear(2026);

        result.TaxDifferenceMonthly[0].Should().Be(750m);
        result.TaxDifferenceAnnualTotal.Should().Be(result.TaxDifferenceMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_DividendoJurosRowSumsOnlyThatSourcesNetValues()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 1), Source("DividendoJuros"), null, 15.50m, Trading212));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 5), Source("DividendoJuros"), null, 4.50m, Trading212));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 10), Source("Gleison"), 3200m, 2450m, Barclays));

        var result = _sut.GetIncomeSummaryForYear(2026);

        result.DividendoJurosMonthly[2].Should().Be(20m);
        result.DividendoJurosAnnualTotal.Should().Be(20m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_LotteryEntriesContributeToNoRow()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 4, 1), Source("Lottery"), null, 500m, Chase));

        var result = _sut.GetIncomeSummaryForYear(2026);

        using (new AssertionScope())
        {
            result.SalaryMonthly.Should().OnlyContain(v => v == 0m);
            result.SalaryAfterTaxesMonthly.Should().OnlyContain(v => v == 0m);
            result.DividendoJurosMonthly.Should().OnlyContain(v => v == 0m);
        }
    }

    [Fact]
    public void GetIncomeSummaryForYear_UnresolvedIncomeSource_DefaultsToNonReportableAndContributesToNoRow()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 4, 1), Source("NotASeededSource"), null, 500m, Chase));

        var act = () => _sut.GetIncomeSummaryForYear(2026);

        var result = act.Should().NotThrow().Which;
        using (new AssertionScope())
        {
            result.SalaryMonthly.Should().OnlyContain(v => v == 0m);
            result.SalaryAfterTaxesMonthly.Should().OnlyContain(v => v == 0m);
            result.DividendoJurosMonthly.Should().OnlyContain(v => v == 0m);
        }
    }

    [Fact]
    public void GetIncomeSummaryForYear_EntryWithNullGrossValue_ContributesZeroToSalary()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 5, 1), Source("Ariana"), null, 350m, Chase));

        var result = _sut.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly[4].Should().Be(0m);
        result.SalaryAfterTaxesMonthly[4].Should().Be(350m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_ExcludesIncomeFromOtherYears()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 1), Source("Gleison"), 3200m, 2450m, Barclays));

        var result = _sut.GetIncomeSummaryForYear(2026);

        result.SalaryAnnualTotal.Should().Be(0m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_WithNoIncome_ReturnsAllZeros()
    {
        var result = _sut.GetIncomeSummaryForYear(2026);

        using (new AssertionScope())
        {
            result.SalaryMonthly.Should().OnlyContain(v => v == 0m);
            result.SalaryAfterTaxesMonthly.Should().OnlyContain(v => v == 0m);
            result.TaxDifferenceMonthly.Should().OnlyContain(v => v == 0m);
            result.DividendoJurosMonthly.Should().OnlyContain(v => v == 0m);
        }
    }

    [Fact]
    public void GetIncomeSummaryForYear_AveragesDivideByTwelveForAnOrdinaryYear()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), Source("Gleison"), 1200m, 600m, Barclays));

        var result = _sut.GetIncomeSummaryForYear(2025);

        result.SalaryAverage.Should().Be(100m);
        result.SalaryAfterTaxesAverage.Should().Be(50m);
        result.TaxDifferenceAverage.Should().Be(50m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_AveragesDivideByElevenFor2017()
    {
        _repository.Incomes.Add(Income.Create(new DateOnly(2017, 1, 5), Source("Gleison"), 1200m, 600m, Barclays));

        var result = _sut.GetIncomeSummaryForYear(2017);

        result.SalaryAverage.Should().Be(109.09m);
        result.SalaryAfterTaxesAverage.Should().Be(54.55m);
        result.TaxDifferenceAverage.Should().Be(54.55m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_AveragesForCurrentYearExcludeInProgressMonth()
    {
        var service = CreateService(timeProvider: new FakeTimeProvider(PinnedNow));
        _repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, 1, 5), Source("Gleison"), 1000m * PinnedMonthsElapsed, 800m * PinnedMonthsElapsed, Barclays));
        _repository.Incomes.Add(Income.Create(DateOnly.FromDateTime(PinnedNow.UtcDateTime), Source("Gleison"), 9999m * PinnedMonthsElapsed, 9999m * PinnedMonthsElapsed, Barclays));

        var result = service.GetIncomeSummaryForYear(CurrentYear);

        result.SalaryAverage.Should().Be(1000m);
        result.SalaryAfterTaxesAverage.Should().Be(800m);
    }

    private static StubCashFlowRepository CreateRepository()
    {
        var repository = new StubCashFlowRepository(seedDefaultIncomeSources: true, seedDefaultCategories: true);
        SeededInvestmentAccounts.SeedInto(repository);
        return repository;
    }
}
