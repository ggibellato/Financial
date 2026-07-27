using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class YearlySummaryServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int PastYear = CurrentYear - 5;

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
    public void GetInvestmentDiffsForYear_CurrentYear_ReturnsAllElevenActiveAccounts()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(CurrentYear);

        result.Accounts.Should().HaveCount(repository.Accounts.Count);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_CurrentYear_ExcludesDisabledAccounts()
    {
        var repository = new StubCashFlowRepository();
        repository.Accounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(CurrentYear);

        result.Accounts.Should().NotContain(a => a.Account == "EverydaySaver");
    }

    [Fact]
    public void GetInvestmentDiffsForYear_PastYear_ReturnsOnlyAccountsPresentThatYear()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 100m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        result.Accounts.Should().ContainSingle().Which.Account.Should().Be("ChaseSave");
    }

    [Fact]
    public void GetInvestmentDiffsForYear_2023_ReturnsExactlyTheNineAccountsConfirmedPresentThatYear()
    {
        // Mirrors PRD P18 F04's named acceptance criterion: Resumo2023 is confirmed (from the
        // source spreadsheet inspection during PRD authoring) to contain exactly these 9 accounts.
        var repository = new StubCashFlowRepository();
        repository.Accounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));
        repository.Accounts.Add(InvestmentAccount.Create("HelpToBuyIsaGgs", isActive: false, isLiability: false));
        repository.Accounts.Add(InvestmentAccount.Create("HelpToBuyIsaAacs", isActive: false, isLiability: false));
        repository.Accounts.Add(InvestmentAccount.Create("ChipEasyAccess", isActive: false, isLiability: false));
        string[] presentIn2023 =
        [
            "EverydaySaver", "BlueRewardsSaver", "PlatinumVisa8003", "PlatinumVisa6007",
            "PaypalCredit", "HelpToBuyIsaGgs", "HelpToBuyIsaAacs", "ChipEasyAccess", "ChaseSave"
        ];
        foreach (var name in presentIn2023)
        {
            repository.Snapshots.Add(InvestmentSnapshot.Create(name, 2023, 1, 100m));
        }
        // Accounts NOT present in 2023 (e.g. opened later, like Trading212Invested) get no snapshot that year.
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(2023);

        result.Accounts.Select(a => a.Account).Should().BeEquivalentTo(presentIn2023);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_PastYearWithNoData_ReturnsNoAccounts()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        result.Accounts.Should().BeEmpty();
    }

    [Fact]
    public void GetInvestmentDiffsForYear_MonthlyDiffsEqualThisMonthMinusPrevMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 2, 1200m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 3, 1100m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(CurrentYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs.Should().HaveCount(12);
        chaseSave.MonthlyDiffs[0].Should().BeNull();
        chaseSave.MonthlyDiffs[1].Should().Be(200m);
        chaseSave.MonthlyDiffs[2].Should().Be(-100m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_MissingSnapshotForAMonth_ContributesZero()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 500m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(CurrentYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyValues[1].Should().Be(0m);
        chaseSave.MonthlyDiffs[1].Should().Be(-500m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_WithPriorYearData_JanuaryDiffEqualsJanuaryMinusPriorDecember()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear - 1, 12, 800m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs[0].Should().Be(200m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_NoPriorYearDataAtAll_JanuaryDiffIsNullForEveryAccountAndNetPosition()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        result.Accounts.Should().OnlyContain(a => a.MonthlyDiffs[0] == null);
        result.NetPosition.MonthlyDiffs[0].Should().BeNull();
    }

    [Fact]
    public void GetInvestmentDiffsForYear_AccountAbsentFromPriorYear_JanuaryDiffTreatsPriorDecemberAsZero()
    {
        var repository = new StubCashFlowRepository();
        // Some other account has prior-year data, so PastYear - 1 is not "no data at all" - but
        // ChaseSave itself has no December snapshot that prior year.
        repository.Snapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", PastYear - 1, 12, 50m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 300m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyDiffs[0].Should().Be(300m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_NetPositionJanuaryDiffEqualsSumOfAccountJanuaryDiffs()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear - 1, 12, 800m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", PastYear - 1, 12, 100m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", PastYear, 1, 150m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        // ChaseSave (asset): 1000 - 800 = 200. PlatinumVisa8003 (liability): -(150 - 100) = -50.
        result.NetPosition.MonthlyDiffs[0].Should().Be(150m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_NetPositionSubtractsLiabilitiesFromAssets()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("PlatinumVisa8003", CurrentYear, 1, 300m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(CurrentYear);

        result.NetPosition.MonthlyValues[0].Should().Be(700m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_NetPositionSumsOnlyScopedAccounts()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        // PlatinumVisa8003 has no snapshot in PastYear, so it's out of scope and must not contribute.
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        result.NetPosition.MonthlyValues[0].Should().Be(1000m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_PastYear_FullYearNetChangeEqualsDecemberMinusJanuary()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 1, 1000m));
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 12, 1800m));
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        result.NetPosition.FullYearNetChange.Should().Be(800m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_CurrentYear_FullYearNetChangeUsesCurrentMonthNotDecember()
    {
        var repository = new StubCashFlowRepository();
        var currentMonth = DateTime.Now.Month;
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 1, 1000m));
        if (currentMonth != 1)
        {
            repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, currentMonth, 1300m));
        }
        if (currentMonth != 12)
        {
            // A December value exists but must NOT be used - it hasn't happened yet this year.
            repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, 12, 9999m));
        }
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(CurrentYear);

        var expected = currentMonth == 1 ? 0m : 300m;
        result.NetPosition.FullYearNetChange.Should().Be(expected);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_PastYear_AverageAndSumIncludeAllTwelveMonthsIncludingJanuary()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear - 1, 12, 800m));
        var value = 900m;
        for (var month = 1; month <= 12; month++)
        {
            repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, month, value));
            value += 50m;
        }
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(PastYear);

        // January diff = 900 - 800 = 100; the remaining 11 months each diff by 50.
        result.NetPosition.SumOfMonthResults.Should().Be(650m);
        result.NetPosition.AverageMonthResult.Should().BeApproximately(650m / 12m, 0.0001m);
    }

    [Fact]
    public void GetInvestmentDiffsForYear_CurrentYear_AverageAndSumOnlyIncludeMonthsThroughTheCurrentMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear - 1, 12, 500m));
        var currentMonth = DateTime.Now.Month;
        var value = 600m;
        for (var month = 1; month <= currentMonth; month++)
        {
            repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", CurrentYear, month, value));
            value += 50m;
        }
        // No snapshots for any month after the current one - they must not contribute a fake
        // "dropped to zero" diff to the totals.
        var service = new YearlySummaryService(repository);

        var result = service.GetInvestmentDiffsForYear(CurrentYear);

        var expectedSum = 100m + 50m * (currentMonth - 1);
        result.NetPosition.SumOfMonthResults.Should().Be(expectedSum);
        result.NetPosition.AverageMonthResult.Should().BeApproximately(expectedSum / currentMonth, 0.0001m);
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


    [Fact]
    public void GetHistoricCategoriesAverageFromYear_ReturnsEmptyList_WhenNoExpensesForSpecifiedYear()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2027, 4, 5), "Should not be there", 1000m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricCategoriesAverageFromYear(2020);

        result.Count.Should().Be(0);
    }


    [Fact]
    public void GetHistoricCategoriesAverageFromYear_ReturnsYearsUpToAndIncludingSpecifiedYear()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2027, 4, 5), "Should not be there", 1000m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricCategoriesAverageFromYear(2026);
        
        result.Count.Should().Be(1);
    }

    [Fact]
    public void GetHistoricCategoriesAverageFromYear_ReturnsTheYearsInOrderDescending()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 3, 5), "Mar", 50m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 25m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 12, 5), "Dec", 55m, Category.Gleison, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2025, 6, 5), "Jun", 120m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2023, 3, 5), "Mar", 52m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricCategoriesAverageFromYear(2026);

        result[0].Year.Should().Be(2026);
        result[1].Year.Should().Be(2025);
        result[2].Year.Should().Be(2023);
    }

    [Fact]
    public void GetHistoricCategoriesAverageFromYear_AveragesPerMonthNotPerTransaction()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 5), "Jan first", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 1, 20), "Jan second", 100m, Category.Mercado, "Barclays", null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 2, 10), "Feb", 400m, Category.Mercado, "Barclays", null));

        var result = service.GetHistoricCategoriesAverageFromYear(2026);

        var mercadoAverage = result[0].AnnualAverages.Single(a => a.Category == nameof(Category.Mercado)).Average;

        // Per-month average (spec): Jan total 200 + Feb total 400 → avg over 2 months = 300
        mercadoAverage.Should().Be(300m);
    }

    [Fact]
    public void GetHistoricIncomeAverageFromYear_ReturnsEmptyList_WhenNoIncomesForSpecifiedYear()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2027, 4, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 100m, 100m, "Barclays"));

        var result = service.GetHistoricIncomeAverageFromYear(2020);

        result.Count.Should().Be(0);
    }

    [Fact]
    public void GetHistoricIncomeAverageFromYear_ReturnsYearsUpToAndIncludingSpecifiedYear()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2027, 4, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 100m, 100m, "Barclays"));

        var result = service.GetHistoricIncomeAverageFromYear(2026);

        result.Count.Should().Be(1);
    }

    [Fact]
    public void GetHistoricIncomeAverageFromYear_ReturnsTheYearsInOrderDescending()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 100m, 100m, "Barclays" ));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 5), IncomeSource.Gleison, 50m, 50m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 12, 5), IncomeSource.Gleison, 25m, 25m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 12, 5), IncomeSource.Gleison, 55m, 55m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2025, 6, 5), IncomeSource.Gleison, 120m, 120m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2023, 3, 5), IncomeSource.Gleison, 52m, 52m, "Barclays"));

        var result = service.GetHistoricIncomeAverageFromYear(2026);

        result[0].Year.Should().Be(2026);
        result[1].Year.Should().Be(2025);
        result[2].Year.Should().Be(2023);
    }

    [Fact]
    public void GetHistoricIncomeAverageFromYear_AveragesPerMonthNotPerTransaction()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 1000m, 800m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 20), IncomeSource.Gleison, 500m, 400m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 2, 5), IncomeSource.Gleison, 3000m, 2400m, "Barclays"));

        var result = service.GetHistoricIncomeAverageFromYear(2026);

        // Per-month gross: Jan total 1500 + Feb total 3000 → avg over 2 months = 2250
        result[0].SalaryAverage.Should().Be(2250m);

        // Per-month net: Jan total 1200 + Feb total 2400 → avg over 2 months = 1800
        result[0].SalaryAfterTaxesAverage.Should().Be(1800m);
    }

    [Fact]
    public void GetHistoricIncomeAverageFromYear_SumsSourcesPerMonthBeforeAveragingWhenActiveMonthsDiffer()
    {
        var repository = new StubCashFlowRepository();
        var service = new YearlySummaryService(repository);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 2, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 3, 5), IncomeSource.Gleison, 1000m, 1000m, "Barclays"));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 1, 5), IncomeSource.Ariana, 500m, 500m, "Barclays"));

        var result = service.GetHistoricIncomeAverageFromYear(2026);

        // Combined per-month salary: Jan 1500, Feb 1000, Mar 1000 → avg over 3 months = 1166.67
        result[0].SalaryAverage.Should().Be(1166.67m);
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
