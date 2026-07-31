using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class AnnualSummaryServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int PastYear = CurrentYear - 5;

    // Pinned to a fixed mid-year date so "current year" averaging tests don't have to branch on
    // (or silently no-op during) the real wall-clock month - June guarantees 5 completed months.
    private static readonly DateTimeOffset PinnedNow = new(CurrentYear, 6, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PinnedToday = DateOnly.FromDateTime(PinnedNow.UtcDateTime);
    private const int PinnedMonthsElapsed = 5;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new AnnualSummaryService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetCategoryTotalsForYear_ReturnsAllFourteenCategories()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

        result.Should().HaveCount(Enum.GetValues<Category>().Length);
    }

    [Fact]
    public void GetCategoryTotalsForYear_AnnualTotalEqualsSumOfMonthlyTotals()
    {
        var repository = CreateRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Mar", 50m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 25m, Category.Mercado, "Barclays", null));
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

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
        var repository = CreateRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Last year", 999m, Category.Mercado, "Barclays", null));
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

        result.Single(c => c.Category == "Mercado").AnnualTotal.Should().Be(0m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_CategoryWithNoExpenses_ReturnsAllZeroMonthsAndZeroAnnualTotal()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsForYear(2026);

        var estudo = result.Single(c => c.Category == "Estudo");
        estudo.MonthlyTotals.Should().OnlyContain(v => v == 0m);
        estudo.AnnualTotal.Should().Be(0m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_CurrentYear_ReturnsAllElevenActiveAccounts()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(CurrentYear);

        result.Accounts.Should().HaveCount(repository.InvestmentAccounts.Count);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_CurrentYear_ExcludesDisabledAccounts()
    {
        var repository = CreateRepository();
        repository.InvestmentAccounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(CurrentYear);

        result.Accounts.Should().NotContain(a => a.Account == "EverydaySaver");
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_PastYear_ReturnsOnlyAccountsPresentThatYear()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 100m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        result.Accounts.Should().ContainSingle().Which.Account.Should().Be("ChaseSave");
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_2023_ReturnsExactlyTheNineAccountsConfirmedPresentThatYear()
    {
        // Mirrors PRD P18 F04's named acceptance criterion: Resumo2023 is confirmed (from the
        // source spreadsheet inspection during PRD authoring) to contain exactly these 9 accounts.
        var repository = CreateRepository();
        repository.InvestmentAccounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));
        repository.InvestmentAccounts.Add(InvestmentAccount.Create("HelpToBuyIsaGgs", isActive: false, isLiability: false));
        repository.InvestmentAccounts.Add(InvestmentAccount.Create("HelpToBuyIsaAacs", isActive: false, isLiability: false));
        repository.InvestmentAccounts.Add(InvestmentAccount.Create("ChipEasyAccess", isActive: false, isLiability: false));
        string[] presentIn2023 =
        [
            "EverydaySaver", "BlueRewardsSaver", "PlatinumVisa8003", "PlatinumVisa6007",
            "PaypalCredit", "HelpToBuyIsaGgs", "HelpToBuyIsaAacs", "ChipEasyAccess", "ChaseSave"
        ];
        foreach (var name in presentIn2023)
        {
            repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(name, 2023, 1, 100m));
        }
        // Accounts NOT present in 2023 (e.g. opened later, like Trading212Invested) get no snapshot that year.
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(2023);

        result.Accounts.Select(a => a.Account).Should().BeEquivalentTo(presentIn2023);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_MonthlyDiffsEqualThisMonthMinusPrevMonth()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 1000m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 2, 1200m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 3, 1100m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(CurrentYear);

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
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 500m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(CurrentYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyValues[1].Should().Be(0m);
        chaseSave.MonthlyDiffs[1].Should().Be(-500m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_WithPriorYearData_JanuaryDiffEqualsJanuaryMinusPriorDecember()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear - 1, 12, 800m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs[0].Should().Be(200m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NoPriorYearDataAtAll_JanuaryDiffIsNullForEveryAccountAndNetPosition()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        result.Accounts.Should().OnlyContain(a => a.MonthlyDiffs[0] == null);
        result.NetPosition.MonthlyDiffs[0].Should().BeNull();
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_AccountAbsentFromPriorYear_JanuaryDiffTreatsPriorDecemberAsZero()
    {
        var repository = CreateRepository();
        // Some other account has prior-year data, so PastYear - 1 is not "no data at all" - but
        // ChaseSave itself has no December snapshot that prior year.
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", PastYear - 1, 12, 50m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 300m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs[0].Should().Be(300m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NetPositionJanuaryDiffEqualsSumOfAccountJanuaryDiffs()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear - 1, 12, 800m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", PastYear - 1, 12, 100m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", PastYear, 1, 150m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        // ChaseSave (asset): 1000 - 800 = 200. PlatinumVisa8003 (liability): -(150 - 100) = -50.
        result.NetPosition.MonthlyDiffs[0].Should().Be(150m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NetPositionSubtractsLiabilitiesFromAssets()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 1000m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", CurrentYear, 1, 300m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(CurrentYear);

        result.NetPosition.MonthlyValues[0].Should().Be(700m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NetPositionSumsOnlyScopedAccounts()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        // PlatinumVisa8003 has no snapshot in PastYear, so it's out of scope and must not contribute.
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        result.NetPosition.MonthlyValues[0].Should().Be(1000m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_PastYear_FullYearNetChangeEqualsDecemberMinusJanuary()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 12, 1800m));
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        result.NetPosition.FullYearNetChange.Should().Be(800m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_CurrentYear_FullYearNetChangeUsesCurrentMonthNotDecember()
    {
        var repository = CreateRepository();
        var currentMonth = DateTime.Now.Month;
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 1000m));
        if (currentMonth != 1)
        {
            repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, currentMonth, 1300m));
        }
        if (currentMonth != 12)
        {
            // A December value exists but must NOT be used - it hasn't happened yet this year.
            repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 12, 9999m));
        }
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(CurrentYear);

        var expected = currentMonth == 1 ? 0m : 300m;
        result.NetPosition.FullYearNetChange.Should().Be(expected);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_PastYear_AverageAndSumIncludeAllTwelveMonthsIncludingJanuary()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear - 1, 12, 800m));
        var value = 900m;
        for (var month = 1; month <= 12; month++)
        {
            repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, month, value));
            value += 50m;
        }
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

        // January diff = 900 - 800 = 100; the remaining 11 months each diff by 50. Routed through
        // F01's MonthlySeries.Average at full precision, so this stays exact, not just approximate.
        result.NetPosition.SumOfMonthResults.Should().Be(650m);
        result.NetPosition.AverageMonthResult.Should().Be(650m / 12m);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_CurrentYear_AverageAndSumOnlyIncludeMonthsThroughTheCurrentMonth()
    {
        var repository = CreateRepository();
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear - 1, 12, 500m));
        var currentMonth = DateTime.Now.Month;
        var value = 600m;
        for (var month = 1; month <= currentMonth; month++)
        {
            repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, month, value));
            value += 50m;
        }
        // No snapshots for any month after the current one - they must not contribute a fake
        // "dropped to zero" diff to the totals.
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(CurrentYear);

        var expectedSum = 100m + 50m * (currentMonth - 1);
        result.NetPosition.SumOfMonthResults.Should().Be(expectedSum);
        result.NetPosition.AverageMonthResult.Should().Be(expectedSum / currentMonth);
    }

    [Fact]
    public void GetInvestmentAnnualResultForYear_NoAccountsOrSnapshots_ReturnsEmptyAccountsAndAllZeroNetPosition()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);

        var result = service.GetInvestmentAnnualResultForYear(PastYear);

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
        var repository = CreateRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 8), IncomeSource.Ariana, 400m, 350m, "Chase"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 2, 1), IncomeSource.Gleison, 3300m, 2500m, "Barclays"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly[0].Should().Be(3600m);
        result.SalaryMonthly[1].Should().Be(3300m);
        result.SalaryAnnualTotal.Should().Be(result.SalaryMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_SalaryAfterTaxesRowSumsGleisonAndArianaNetValuesPerMonth()
    {
        var repository = CreateRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 8), IncomeSource.Ariana, 400m, 350m, "Chase"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryAfterTaxesMonthly[0].Should().Be(2800m);
        result.SalaryAfterTaxesAnnualTotal.Should().Be(result.SalaryAfterTaxesMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_TaxDifferenceRowEqualsSalaryMinusSalaryAfterTaxes()
    {
        var repository = CreateRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.TaxDifferenceMonthly[0].Should().Be(750m);
        result.TaxDifferenceAnnualTotal.Should().Be(result.TaxDifferenceMonthly.Sum());
    }

    [Fact]
    public void GetIncomeSummaryForYear_DividendoJurosRowSumsOnlyThatSourcesNetValues()
    {
        var repository = CreateRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 1), IncomeSource.DividendoJuros, null, 15.50m, "Trading212"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 5), IncomeSource.DividendoJuros, null, 4.50m, "Trading212"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 10), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.DividendoJurosMonthly[2].Should().Be(20m);
        result.DividendoJurosAnnualTotal.Should().Be(20m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_LotteryEntriesContributeToNoRow()
    {
        var repository = CreateRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 4, 1), IncomeSource.Lottery, null, 500m, "Chase"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

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
        var repository = CreateRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 5, 1), IncomeSource.Ariana, null, 350m, "Chase"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryMonthly[4].Should().Be(0m);
        result.SalaryAfterTaxesMonthly[4].Should().Be(350m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_ExcludesIncomeFromOtherYears()
    {
        var repository = CreateRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 1), IncomeSource.Gleison, 3200m, 2450m, "Barclays"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        result.SalaryAnnualTotal.Should().Be(0m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_WithNoIncome_ReturnsAllZeros()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);

        var result = service.GetIncomeSummaryForYear(2026);

        using (new AssertionScope())
        {
            result.SalaryMonthly.Should().OnlyContain(v => v == 0m);
            result.SalaryAfterTaxesMonthly.Should().OnlyContain(v => v == 0m);
            result.TaxDifferenceMonthly.Should().OnlyContain(v => v == 0m);
            result.DividendoJurosMonthly.Should().OnlyContain(v => v == 0m);
        }
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasMonthlyEqualsSumOfAllCategoriesPerMonth()
    {
        var repository = CreateRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Investing", 30m, Category.Investimento, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 2, 5), "Groceries", 50m, Category.Mercado, "Barclays", null));
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsAnnualForYear(2026);

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
        var repository = CreateRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Investing", 30m, Category.Investimento, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        // DividendoJuros is seeded deliberately: the corrected formula must exclude it entirely.
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.DividendoJuros, null, 20m, "Barclays"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsAnnualForYear(2026);

        // Resultado = SalaryAfterTaxes(800) - TotalDespesas(130) + Investimento(30) = 700, not 720.
        result.ResultadoMonthly[0].Should().Be(700m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_AnnualTotalsEqualSumOfMonthlyValues()
    {
        var repository = CreateRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 6, 5), "Investing", 30m, Category.Investimento, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsAnnualForYear(2026);

        result.TotalDespesasAnnualTotal.Should().Be(result.TotalDespesasMonthly.Sum());
        result.ResultadoAnnualTotal.Should().Be(result.ResultadoMonthly.Sum());
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_NoRecordedData_ReturnsAllZeroSeries()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsAnnualForYear(2026);

        using (new AssertionScope())
        {
            result.CategoryTotals.Should().HaveCount(Enum.GetValues<Category>().Length)
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
        var repository = CreateRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        var service = new AnnualSummaryService(repository);

        var result = service.GetCategoryTotalsAnnualForYear(2026);

        result.CategoryTotals.Should().BeEquivalentTo(service.GetCategoryTotalsForYear(2026));
        result.IncomeSummary.Should().BeEquivalentTo(service.GetIncomeSummaryForYear(2026));
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ReturnsEmptyList_WhenNoExpensesForSpecifiedYear()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2027, 4, 5), "Should not be there", 1000m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricSummaryAverageFromYear(2020);

        result.Count.Should().Be(0);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ReturnsYearsUpToAndIncludingSpecifiedYear()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2027, 4, 5), "Should not be there", 1000m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricSummaryAverageFromYear(2026);

        result.Count.Should().Be(1);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ReturnsTheYearsInOrderDescending()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Mar", 50m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 25m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 55m, Category.Gleison, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 6, 5), "Jun", 120m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2023, 3, 5), "Mar", 52m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricSummaryAverageFromYear(2026);

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
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Jan first", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 20), "Jan second", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 2, 10), "Feb", 400m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricSummaryAverageFromYear(2026);

        var mercadoAverage = result[0].AnnualAverages.Single(a => a.Category == nameof(Category.Mercado)).Value;

        mercadoAverage.Should().Be(50m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_AveragesCategoryValuesFor2017Year()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Jan first", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 20), "Jan second", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 2, 10), "Feb", 400m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricSummaryAverageFromYear(2026);

        var mercadoAverage = result[0].AnnualAverages.Single(a => a.Category == nameof(Category.Mercado)).Value;

        mercadoAverage.Should().Be(54.55m);
    }


    [Fact]
    public void GetHistoricSummaryAverageFromYear_MergesIncomeAveragesIntoMatchingYearsInDescendingOrder()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(DateTime.UtcNow.Year + 1, 4, 5), "Should not be there", 10m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 4, 5), "2025", 10m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2023, 4, 5), "2023", 10m, Category.Mercado, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(DateTime.UtcNow.Year+1, 4, 5), IncomeSource.Gleison, 9999m, 9999m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 4, 5), IncomeSource.Gleison, 1200m, 1200m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2023, 4, 5), IncomeSource.Gleison, 900m, 900m, "Barclays"));

        var result = service.GetHistoricSummaryAverageFromYear(2026);

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
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository, new FakeTimeProvider(PinnedNow));

        repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, PinnedNow.Month, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, PinnedNow.Month - 1, 5), IncomeSource.Gleison, 2400m, 900m, "Barclays"));

        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 20), IncomeSource.Gleison, 500m, 400m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 2, 5), IncomeSource.Gleison, 3000m, 2400m, "Barclays"));

        repository.Incomes.Add(Income.Create(new DateOnly(2017, 7, 5), IncomeSource.Gleison, 1100m, 110m, "Barclays"));

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
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 2, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 3, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Ariana, 500m, 500m, "Barclays"));

        var result = service.GetHistoricSummaryAverageFromYear(2025);

        // Combined per-month salary: Jan 1500, Feb 1000, Mar 1000 → total 3500, averaged over all 12
        // months of a completed past year (NumberOfMonthsForAverage returns 12 for any non-current year).
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(Math.Round(3500m / 12m, AnnualSummaryService.AverageDecimalPlaces));
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_IncludesYearsWithIncomeButNoExpenses()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1200m, 600m, "Barclays"));

        var result = service.GetHistoricSummaryAverageFromYear(2026);

        result.Count.Should().Be(1);
        result[0].Year.Should().Be(2025);
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(100m);
        result[0].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(50m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ComputesTotalDespesasAsSumOfCategoryRowsOnly()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Investing", 30m, Category.Investimento, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.DividendoJuros, null, 20m, "Barclays"));

        var result = service.GetHistoricSummaryAverageFromYear(2025);

        // Total despesas must be the sum of the 14 expense category rows only (Mercado 100 + Investimento 30 = 130),
        // never the income rows (Salary/Salary after taxes/Tax difference/Dividendo/Juros) merged in ahead of them.
        result[0].AnnualAverages.Single(a => a.Category == "Total despesas").Value.Should().Be(10.83m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ComputesResultadoFromSalaryAfterTaxesTotalDespesasAndInvestimentoExcludingDividendoJuros()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Investing", 30m, Category.Investimento, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        // DividendoJuros is seeded deliberately: unlike Category Totals' own Resultado, this sub-tab's
        // Resultado excludes Dividendo/Juros entirely, so this income must NOT affect the expected value.
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.DividendoJuros, null, 20m, "Barclays"));

        var result = service.GetHistoricSummaryAverageFromYear(2025);

        // Resultado (R-D-Inv) = SalaryAfterTaxes(800/12) - TotalDespesas(130/12) + Investimento(30/12) = 700
        result[0].AnnualAverages.Single(a => a.Category == "Resultado (R-D-Inv)").Value.Should().Be(58.34m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_LotteryIncomeContributesToNoRow()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 120m, Category.Mercado, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.DividendoJuros, null, 20m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Lottery, null, 500m, "Barclays"));

        var result = service.GetHistoricSummaryAverageFromYear(2025);

        // Lottery must not leak into Dividendo/Juros (or any other row) - it must have the same
        // effect as if it were never recorded at all.
        result[0].AnnualAverages.Single(a => a.Category == "Salary").Value.Should().Be(83.33m);
        result[0].AnnualAverages.Single(a => a.Category == "Salary after taxes").Value.Should().Be(66.67m);
        result[0].AnnualAverages.Single(a => a.Category == "Dividendo/Juros").Value.Should().Be(1.67m);
        result[0].AnnualAverages.Single(a => a.Category == "Resultado (R-D-Inv)").Value.Should().Be(56.67m);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ZeroFillsCategoriesWithNoExpensesThatYear()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 1200m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricSummaryAverageFromYear(2025);

        // Every one of the 14 Category enum values must appear, even with zero recorded expenses that year.
        foreach (var category in Enum.GetValues<Category>())
        {
            var entry = result[0].AnnualAverages.Single(a => a.Category == category.ToString());
            entry.Value.Should().Be(category == Category.Mercado ? 100m : 0m);
        }
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_RowOrderMatchesCategoryTotalsFixedOrder()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.DividendoJuros, null, 20m, "Barclays"));

        var result = service.GetHistoricSummaryAverageFromYear(2026);

        var expectedOrder = new List<string> { "Salary", "Salary after taxes", "Tax difference", "Dividendo/Juros" }
            .Concat(Enum.GetValues<Category>().Select(c => c.ToString()))
            .Concat(["Resultado (R-D-Inv)", "Total despesas"]);

        result[0].AnnualAverages.Select(a => a.Category).Should().Equal(expectedOrder);
    }

    [Fact]
    public void GetHistoricSummaryAverageFromYear_ExcludesInProgressCurrentMonthFromCurrentYearAverage()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository, new FakeTimeProvider(PinnedNow));
        repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear, 1, 5), "Completed months", 100m * PinnedMonthsElapsed, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, Category.Mercado, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, 1, 5), IncomeSource.Gleison, 1000m * PinnedMonthsElapsed, 800m * PinnedMonthsElapsed, "Barclays"));
        repository.Incomes.Add(Income.Create(PinnedToday, IncomeSource.Gleison, 9999m * PinnedMonthsElapsed, 9999m * PinnedMonthsElapsed, "Barclays"));

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
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository, new FakeTimeProvider(PinnedNow));
        repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear - 1, 12, 5), "Prior year", 50m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricSummaryAverageFromYear(CurrentYear);

        // The current year has no fully completed month yet, so it must not appear at all;
        // the range starts at the previous year instead.
        result.Should().NotContain(r => r.Year == CurrentYear);
        result.Should().ContainSingle(r => r.Year == CurrentYear - 1);
    }

    [Fact]
    public void GetCategoryTotalsForYear_AverageDividesByTwelveForAnOrdinaryYear()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Jan first", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 20), "Jan second", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 2, 10), "Feb", 400m, Category.Mercado, "Barclays", null));

        var result = service.GetCategoryTotalsForYear(2025);

        result.Single(c => c.Category == "Mercado").Average.Should().Be(50m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_AverageDividesByElevenFor2017()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Jan first", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 20), "Jan second", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 2, 10), "Feb", 400m, Category.Mercado, "Barclays", null));

        var result = service.GetCategoryTotalsForYear(2017);

        result.Single(c => c.Category == "Mercado").Average.Should().Be(54.55m);
    }

    [Fact]
    public void GetCategoryTotalsForYear_AverageForCurrentYearExcludesInProgressMonth()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository, new FakeTimeProvider(PinnedNow));
        repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear, 1, 5), "Completed months", 100m * PinnedMonthsElapsed, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, Category.Mercado, "Barclays", null));

        var result = service.GetCategoryTotalsForYear(CurrentYear);

        // The in-progress current-month entry (9999) must be excluded entirely from the average,
        // not treated as a completed month with a low value.
        result.Single(c => c.Category == "Mercado").Average.Should().Be(100m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_AveragesDivideByTwelveForAnOrdinaryYear()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1200m, 600m, "Barclays"));

        var result = service.GetIncomeSummaryForYear(2025);

        result.SalaryAverage.Should().Be(100m);
        result.SalaryAfterTaxesAverage.Should().Be(50m);
        result.TaxDifferenceAverage.Should().Be(50m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_AveragesDivideByElevenFor2017()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2017, 1, 5), IncomeSource.Gleison, 1200m, 600m, "Barclays"));

        var result = service.GetIncomeSummaryForYear(2017);

        result.SalaryAverage.Should().Be(109.09m);
        result.SalaryAfterTaxesAverage.Should().Be(54.55m);
        result.TaxDifferenceAverage.Should().Be(54.55m);
    }

    [Fact]
    public void GetIncomeSummaryForYear_AveragesForCurrentYearExcludeInProgressMonth()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository, new FakeTimeProvider(PinnedNow));
        repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, 1, 5), IncomeSource.Gleison, 1000m * PinnedMonthsElapsed, 800m * PinnedMonthsElapsed, "Barclays"));
        repository.Incomes.Add(Income.Create(PinnedToday, IncomeSource.Gleison, 9999m * PinnedMonthsElapsed, 9999m * PinnedMonthsElapsed, "Barclays"));

        var result = service.GetIncomeSummaryForYear(CurrentYear);

        result.SalaryAverage.Should().Be(1000m);
        result.SalaryAfterTaxesAverage.Should().Be(800m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasAndResultadoAveragesDivideByTwelveForAnOrdinaryYear()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 1, 5), "Investing", 30m, Category.Investimento, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 1, 5), IncomeSource.DividendoJuros, null, 20m, "Barclays"));

        var result = service.GetCategoryTotalsAnnualForYear(2025);

        // Total despesas: (Mercado 100 + Investimento 30) / 12 = 10.83.
        result.TotalDespesasAverage.Should().Be(10.83m);
        // Resultado: the per-month series (SalaryAfterTaxes 800 - TotalDespesas 130 + Investimento 30 = 700
        // in January, zero elsewhere) is averaged as a whole, i.e. 700 / 12 = 58.33.
        result.ResultadoAverage.Should().Be(58.33m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasAndResultadoAveragesDivideByElevenFor2017()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Groceries", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2017, 1, 5), "Investing", 30m, Category.Investimento, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(2017, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2017, 1, 5), IncomeSource.DividendoJuros, null, 20m, "Barclays"));

        var result = service.GetCategoryTotalsAnnualForYear(2017);

        result.TotalDespesasAverage.Should().Be(11.82m);
        result.ResultadoAverage.Should().Be(63.64m);
    }

    [Fact]
    public void GetCategoryTotalsAnnualForYear_TotalDespesasAndResultadoAveragesForCurrentYearExcludeInProgressMonth()
    {
        var repository = CreateRepository();
        var service = new AnnualSummaryService(repository, new FakeTimeProvider(PinnedNow));
        repository.Expenses.Add(Expense.Create(new DateOnly(CurrentYear, 1, 5), "Completed months", 100m * PinnedMonthsElapsed, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(PinnedToday, "In-progress month", 9999m, Category.Mercado, "Barclays", null));
        repository.Incomes.Add(Income.Create(new DateOnly(CurrentYear, 1, 5), IncomeSource.Gleison, 1000m * PinnedMonthsElapsed, 800m * PinnedMonthsElapsed, "Barclays"));
        repository.Incomes.Add(Income.Create(PinnedToday, IncomeSource.Gleison, 9999m * PinnedMonthsElapsed, 9999m * PinnedMonthsElapsed, "Barclays"));

        var result = service.GetCategoryTotalsAnnualForYear(CurrentYear);

        result.TotalDespesasAverage.Should().Be(100m);
        result.ResultadoAverage.Should().Be(700m);
    }

    private static StubCashFlowRepository CreateRepository()
    {
        var repository = new StubCashFlowRepository();
        SeededInvestmentAccounts.SeedInto(repository);
        return repository;
    }
}
