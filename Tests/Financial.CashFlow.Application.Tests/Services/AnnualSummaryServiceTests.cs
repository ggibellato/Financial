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

public class AnnualSummaryServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int PastYear = CurrentYear - 5;
    private static readonly Microsoft.Extensions.Logging.ILogger<AnnualSummaryService> Logger = NullLogger<AnnualSummaryService>.Instance;

    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly CreditCard BarclaysPlatinumVisa8003 = CreditCard.Create("BarclaysPlatinumVisa8003");
    private static readonly CreditCard BaAmex = CreditCard.Create("BaAmex");

    private static IncomeSource Source(string name) => IncomeSource.Create(name, IncomeGroup.NonReportable);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly AnnualSummaryService _sut;

    public AnnualSummaryServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository or dependency does not repeat the whole construction sequence.</summary>
    private AnnualSummaryService CreateService(StubCashFlowRepository? repository = null, TimeProvider? timeProvider = null) =>
        new(repository ?? _repository, _tracer, Logger, timeProvider);

    private static InvestmentAccount Account(StubCashFlowRepository repository, string name) =>
        repository.InvestmentAccounts.First(a => a.Name == name);

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
        Action act = () => new AnnualSummaryService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new AnnualSummaryService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_RecordsSuccessfulSpan()
    {
        _sut.GetCategoryTotalsAnnualForYear(2026);

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.AnnualSummaryService.GetCategoryTotalsAnnualForYear");
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
    public void GetInvestmentAnnualResultForYear_CurrentYear_ReturnsAllElevenActiveAccounts()
    {
        var result = _sut.GetInvestmentAnnualResultForYear(CurrentYear);

        result.Accounts.Should().HaveCount(_repository.InvestmentAccounts.Count);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_CurrentYear_ExcludesDisabledAccounts()
    {
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));

        var result = _sut.GetInvestmentAnnualResultForYear(CurrentYear);

        result.Accounts.Should().NotContain(a => a.Account == "EverydaySaver");
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_PastYear_ReturnsOnlyAccountsPresentThatYear()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 1, 100m));

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        result.Accounts.Should().ContainSingle().Which.Account.Should().Be("ChaseSave");
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_2023_ReturnsExactlyTheNineAccountsConfirmedPresentThatYear()
    {
        // Mirrors PRD P18 F04's named acceptance criterion: Resumo2023 is confirmed (from the
        // source spreadsheet inspection during PRD authoring) to contain exactly these 9 accounts.
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("HelpToBuyIsaGgs", isActive: false, isLiability: false));
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("HelpToBuyIsaAacs", isActive: false, isLiability: false));
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("ChipEasyAccess", isActive: false, isLiability: false));
        string[] presentIn2023 =
        [
            "EverydaySaver", "BlueRewardsSaver", "PlatinumVisa8003", "PlatinumVisa6007",
            "PaypalCredit", "HelpToBuyIsaGgs", "HelpToBuyIsaAacs", "ChipEasyAccess", "ChaseSave"
        ];
        foreach (var name in presentIn2023)
        {
            _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, name), 2023, 1, 100m));
        }
        // Accounts NOT present in 2023 (e.g. opened later, like Trading212Invested) get no snapshot that year.

        var result = _sut.GetInvestmentAnnualResultForYear(2023);

        result.Accounts.Select(a => a.Account).Should().BeEquivalentTo(presentIn2023);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_MonthlyDiffsEqualThisMonthMinusPrevMonth()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, 1, 1000m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, 2, 1200m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, 3, 1100m));

        var result = _sut.GetInvestmentAnnualResultForYear(CurrentYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        using (new AssertionScope())
        {
            chaseSave.MonthlyDiffs.Should().HaveCount(12);
            chaseSave.MonthlyDiffs[0].Should().BeNull();
            chaseSave.MonthlyDiffs[1].Should().Be(200m);
            chaseSave.MonthlyDiffs[2].Should().Be(-100m);
        }
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_MissingSnapshotForAMonth_ContributesZero()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, 1, 500m));

        var result = _sut.GetInvestmentAnnualResultForYear(CurrentYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyValues[1].Should().Be(0m);
        chaseSave.MonthlyDiffs[1].Should().Be(-500m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_WithPriorYearData_JanuaryDiffEqualsJanuaryMinusPriorDecember()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear - 1, 12, 800m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 1, 1000m));

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs[0].Should().Be(200m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NoPriorYearDataAtAll_JanuaryDiffIsNullForEveryAccountAndNetPosition()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 1, 1000m));

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        result.Accounts.Should().OnlyContain(a => a.MonthlyDiffs[0] == null);
        result.NetPosition.MonthlyDiffs[0].Should().BeNull();
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_AccountAbsentFromPriorYear_JanuaryDiffTreatsPriorDecemberAsZero()
    {
        // Some other account has prior-year data, so PastYear - 1 is not "no data at all" - but
        // ChaseSave itself has no December snapshot that prior year.
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "PlatinumVisa8003"), PastYear - 1, 12, 50m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 1, 300m));

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs[0].Should().Be(300m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NetPositionJanuaryDiffEqualsSumOfAccountJanuaryDiffs()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear - 1, 12, 800m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 1, 1000m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "PlatinumVisa8003"), PastYear - 1, 12, 100m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "PlatinumVisa8003"), PastYear, 1, 150m));

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        // ChaseSave (asset): 1000 - 800 = 200. PlatinumVisa8003 (liability): -(150 - 100) = -50.
        result.NetPosition.MonthlyDiffs[0].Should().Be(150m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NetPositionSubtractsLiabilitiesFromAssets()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, 1, 1000m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "PlatinumVisa8003"), CurrentYear, 1, 300m));

        var result = _sut.GetInvestmentAnnualResultForYear(CurrentYear);

        result.NetPosition.MonthlyValues[0].Should().Be(700m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NetPositionSumsOnlyScopedAccounts()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 1, 1000m));
        // PlatinumVisa8003 has no snapshot in PastYear, so it's out of scope and must not contribute.

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        result.NetPosition.MonthlyValues[0].Should().Be(1000m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_PastYear_FullYearNetChangeEqualsDecemberMinusJanuary()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 1, 1000m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, 12, 1800m));

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        result.NetPosition.FullYearNetChange.Should().Be(800m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_CurrentYear_FullYearNetChangeUsesCurrentMonthNotDecember()
    {
        var currentMonth = DateTime.Now.Month;
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, 1, 1000m));
        if (currentMonth != 1)
        {
            _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, currentMonth, 1300m));
        }
        if (currentMonth != 12)
        {
            // A December value exists but must NOT be used - it hasn't happened yet this year.
            _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, 12, 9999m));
        }

        var result = _sut.GetInvestmentAnnualResultForYear(CurrentYear);

        var expected = currentMonth == 1 ? 0m : 300m;
        result.NetPosition.FullYearNetChange.Should().Be(expected);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_PastYear_AverageAndSumIncludeAllTwelveMonthsIncludingJanuary()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear - 1, 12, 800m));
        var value = 900m;
        for (var month = 1; month <= 12; month++)
        {
            _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), PastYear, month, value));
            value += 50m;
        }

        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        // January diff = 900 - 800 = 100; the remaining 11 months each diff by 50. Routed through
        // F01's MonthlySeries.Average at full precision, so this stays exact, not just approximate.
        result.NetPosition.SumOfMonthResults.Should().Be(650m);
        result.NetPosition.AverageMonthResult.Should().Be(650m / 12m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_CurrentYear_AverageAndSumOnlyIncludeMonthsThroughTheCurrentMonth()
    {
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear - 1, 12, 500m));
        var currentMonth = DateTime.Now.Month;
        var value = 600m;
        for (var month = 1; month <= currentMonth; month++)
        {
            _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(Account(_repository, "ChaseSave"), CurrentYear, month, value));
            value += 50m;
        }
        // No snapshots for any month after the current one - they must not contribute a fake
        // "dropped to zero" diff to the totals.

        var result = _sut.GetInvestmentAnnualResultForYear(CurrentYear);

        var expectedSum = 100m + 50m * (currentMonth - 1);
        result.NetPosition.SumOfMonthResults.Should().Be(expectedSum);
        result.NetPosition.AverageMonthResult.Should().Be(expectedSum / currentMonth);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NoAccountsOrSnapshots_ReturnsEmptyAccountsAndAllZeroNetPosition()
    {
        var result = _sut.GetInvestmentAnnualResultForYear(PastYear);

        using (new AssertionScope())
        {
            result.Accounts.Should().BeEmpty();
            result.NetPosition.MonthlyValues.Should().OnlyContain(v => v == 0m);
            result.NetPosition.FullYearNetChange.Should().Be(0m);
            result.NetPosition.AverageMonthResult.Should().Be(0m);
            result.NetPosition.SumOfMonthResults.Should().Be(0m);
        }
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
    public void GetCategoryTotalsAnnualForYear_NestedCategoryTotalsAndIncomeSummaryMatchStandaloneMethods()
    {
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, CategoryByName(_repository, "Mercado"), Barclays, null));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), Source("Gleison"), 1000m, 800m, Barclays));

        var result = _sut.GetCategoryTotalsAnnualForYear(2026);

        result.CategoryTotals.Should().BeEquivalentTo(_sut.GetCategoryTotalsForYear(2026));
        result.IncomeSummary.Should().BeEquivalentTo(_sut.GetIncomeSummaryForYear(2026));
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
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(Math.Round(3500m / 12m, AnnualSummaryService.AverageDecimalPlaces));
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
        _repository.Incomes.Add(Income.Create(PinnedToday, Source("Gleison"), 9999m * PinnedMonthsElapsed, 9999m * PinnedMonthsElapsed, Barclays));

        var result = service.GetIncomeSummaryForYear(CurrentYear);

        result.SalaryAverage.Should().Be(1000m);
        result.SalaryAfterTaxesAverage.Should().Be(800m);
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

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new AnnualSummaryService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
