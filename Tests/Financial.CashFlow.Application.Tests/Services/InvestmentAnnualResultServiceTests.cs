using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class InvestmentAnnualResultServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int PastYear = CurrentYear - 5;
    private static readonly Microsoft.Extensions.Logging.ILogger<InvestmentAnnualResultService> Logger = NullLogger<InvestmentAnnualResultService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly InvestmentAnnualResultService _sut;

    public InvestmentAnnualResultServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = new InvestmentAnnualResultService(_repository, _tracer, Logger);
    }

    private static InvestmentAccount Account(StubCashFlowRepository repository, string name) =>
        repository.InvestmentAccounts.First(a => a.Name == name);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentAnnualResultService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new InvestmentAnnualResultService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new InvestmentAnnualResultService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
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

    private static StubCashFlowRepository CreateRepository()
    {
        var repository = new StubCashFlowRepository(seedDefaultIncomeSources: true, seedDefaultCategories: true);
        SeededInvestmentAccounts.SeedInto(repository);
        return repository;
    }
}
