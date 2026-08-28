using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class AnnualSummaryViewModelTests
{
    private static (
        AnnualSummaryViewModel ViewModel,
        StubCategorySummaryService CategoryService,
        StubInvestmentAnnualResultService InvestmentService,
        StubHistoricAverageService HistoricService) CreateViewModel()
    {
        var categoryService = new StubCategorySummaryService();
        var investmentService = new StubInvestmentAnnualResultService();
        var historicService = new StubHistoricAverageService();
        var viewModel = new AnnualSummaryViewModel(categoryService, investmentService, historicService);
        return (viewModel, categoryService, investmentService, historicService);
    }

    private static decimal[] MonthlyArray(decimal value) => Enumerable.Repeat(value, 12).ToArray();

    [Fact]
    public async Task RefreshAsync_BuildsCategoryTotalsRowsInCorrectOrderWithSpacersAndEmphasis()
    {
        var (viewModel, categoryService, _, _) = CreateViewModel();
        categoryService.CategoryTotalsAnnual = new CategoryTotalsAnnualDTO
        {
            CategoryTotals =
            [
                new CategoryAnnualTotalDTO { Category = "Mercado", MonthlyTotals = MonthlyArray(100m), AnnualTotal = 1200m, Average = 100m },
                new CategoryAnnualTotalDTO { Category = "Transporte", MonthlyTotals = MonthlyArray(50m), AnnualTotal = 600m, Average = 50m },
            ],
            IncomeSummary = new IncomeAnnualSummaryDTO
            {
                SalaryMonthly = MonthlyArray(1000m), SalaryAnnualTotal = 12000m, SalaryAverage = 1000m,
                SalaryAfterTaxesMonthly = MonthlyArray(800m), SalaryAfterTaxesAnnualTotal = 9600m, SalaryAfterTaxesAverage = 800m,
                TaxDifferenceMonthly = MonthlyArray(200m), TaxDifferenceAnnualTotal = 2400m, TaxDifferenceAverage = 200m,
                DividendoJurosMonthly = MonthlyArray(10m), DividendoJurosAnnualTotal = 120m, DividendoJurosAverage = 10m,
            },
            TotalDespesasMonthly = MonthlyArray(150m), TotalDespesasAnnualTotal = 1800m, TotalDespesasAverage = 150m,
            ResultadoMonthly = MonthlyArray(660m), ResultadoAnnualTotal = 7920m, ResultadoAverage = 660m,
        };

        await viewModel.RefreshAsync();

        var labels = viewModel.CategoryTotalRows.Select(r => r.Label).ToList();
        labels.Should().Equal(
            "Salary", "Salary after taxes", "Tax difference", "",
            "Dividendo/Juros", "",
            "Mercado", "Transporte", "",
            "Resultado (R-D-Inv)", "Total despesas");

        viewModel.CategoryTotalRows[3].IsSpacer.Should().BeTrue();
        viewModel.CategoryTotalRows[5].IsSpacer.Should().BeTrue();
        viewModel.CategoryTotalRows[8].IsSpacer.Should().BeTrue();
        viewModel.CategoryTotalRows[9].IsEmphasized.Should().BeTrue();
        viewModel.CategoryTotalRows[10].IsEmphasized.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_BuildsInvestmentRowsWithLiabilitySuffixAndNullableCells()
    {
        var (viewModel, _, investmentService, _) = CreateViewModel();
        var monthlyDiffs = new decimal?[12];
        monthlyDiffs[0] = null;
        for (var i = 1; i < 12; i++)
        {
            monthlyDiffs[i] = 5m;
        }

        investmentService.InvestmentAnnualResult = new InvestmentAnnualResultDTO
        {
            Accounts =
            [
                new InvestmentAccountAnnualDiffDTO { Account = "ISA", IsLiability = false, MonthlyValues = MonthlyArray(1000m), MonthlyDiffs = new decimal?[12] },
                new InvestmentAccountAnnualDiffDTO { Account = "Credit Card", IsLiability = true, MonthlyValues = MonthlyArray(200m), MonthlyDiffs = new decimal?[12] },
            ],
            NetPosition = new NetPositionAnnualDiffDTO
            {
                MonthlyValues = MonthlyArray(800m), MonthlyDiffs = monthlyDiffs,
                FullYearNetChange = 100m, AverageMonthResult = 5m, SumOfMonthResults = 55m,
            },
        };

        await viewModel.RefreshAsync();

        viewModel.InvestmentRows.Should().Contain(r => r.Label == "ISA");
        viewModel.InvestmentRows.Should().Contain(r => r.Label == "Credit Card (-)");
        var totalRow = viewModel.InvestmentRows.Single(r => r.Label == "Total");
        totalRow.IsEmphasized.Should().BeTrue();
        var monthResultRow = viewModel.InvestmentRows.Single(r => r.Label == "Month Result");
        monthResultRow.IsEmphasized.Should().BeTrue();
        monthResultRow.MonthlyValues[0].Should().BeNull();
        monthResultRow.MonthlyValues[1].Should().Be(5m);
    }

    [Fact]
    public async Task RefreshAsync_ExposesNetPositionSummaryFigures()
    {
        var (viewModel, _, investmentService, _) = CreateViewModel();
        investmentService.InvestmentAnnualResult = new InvestmentAnnualResultDTO
        {
            Accounts = [],
            NetPosition = new NetPositionAnnualDiffDTO
            {
                MonthlyValues = new decimal[12], MonthlyDiffs = new decimal?[12],
                FullYearNetChange = 1234.56m, AverageMonthResult = 102.88m, SumOfMonthResults = 1234.56m,
            },
        };

        await viewModel.RefreshAsync();

        viewModel.YearProgress.Should().Be(1234.56m);
        viewModel.AverageMonthResult.Should().Be(102.88m);
        viewModel.SumOfMonthResults.Should().Be(1234.56m);
    }

    [Fact]
    public async Task RefreshAsync_BuildsHistoricSummaryRowsWithSpacersAndEmphasis()
    {
        var (viewModel, _, _, historicService) = CreateViewModel();
        historicService.HistoricSummaryAverage =
        [
            new CategoryAnnualGroupValueDTO
            {
                Year = 2024,
                AnnualAverages =
                [
                    new CategoryGroupValueDTO { Category = "Tax difference", Value = 100m },
                    new CategoryGroupValueDTO { Category = "Reserva", Value = 200m },
                    new CategoryGroupValueDTO { Category = "Resultado (R-D-Inv)", Value = 300m },
                ],
            },
            new CategoryAnnualGroupValueDTO
            {
                Year = 2025,
                AnnualAverages =
                [
                    new CategoryGroupValueDTO { Category = "Tax difference", Value = 110m },
                    new CategoryGroupValueDTO { Category = "Reserva", Value = 210m },
                    new CategoryGroupValueDTO { Category = "Resultado (R-D-Inv)", Value = 310m },
                ],
            },
        ];

        await viewModel.RefreshAsync();

        var categories = viewModel.HistoricSummaryRows.Select(r => r.Category).ToList();
        categories.Should().Equal("Tax difference", "", "Reserva", "", "Resultado (R-D-Inv)");

        viewModel.HistoricSummaryRows[1].IsSpacer.Should().BeTrue();
        viewModel.HistoricSummaryRows[3].IsSpacer.Should().BeTrue();
        viewModel.HistoricSummaryRows[4].IsEmphasized.Should().BeTrue();

        var taxDifferenceRow = viewModel.HistoricSummaryRows.Single(r => r.Category == "Tax difference");
        taxDifferenceRow.ValuesByYear[2024].Should().Be(100m);
        taxDifferenceRow.ValuesByYear[2025].Should().Be(110m);
    }

    [Fact]
    public async Task RefreshAsync_EveryHistoricSummaryRow_HasAValueForEveryAvailableYear()
    {
        var (viewModel, _, _, historicService) = CreateViewModel();
        historicService.HistoricSummaryAverage =
        [
            new CategoryAnnualGroupValueDTO
            {
                Year = 2017,
                AnnualAverages = [new CategoryGroupValueDTO { Category = "Tax difference", Value = 50m }],
            },
            new CategoryAnnualGroupValueDTO
            {
                Year = 2025,
                AnnualAverages = [new CategoryGroupValueDTO { Category = "Tax difference", Value = 90m }],
            },
        ];

        await viewModel.RefreshAsync();

        foreach (var year in viewModel.AvailableYears)
        {
            foreach (var row in viewModel.FilteredHistoricSummaryRows.Where(r => !r.IsSpacer))
            {
                row.ValuesByYear.Should().ContainKey(year,
                    $"the {row.Category} row must have a value for every year the grid's dynamic columns expose, or binding throws a KeyNotFoundException");
            }
        }
    }

    [Fact]
    public async Task RefreshAsync_ExposesAvailableYearsFromHistoricSummaryResponse()
    {
        var (viewModel, _, _, historicService) = CreateViewModel();
        historicService.HistoricSummaryAverage =
        [
            new CategoryAnnualGroupValueDTO { Year = 2023, AnnualAverages = [] },
            new CategoryAnnualGroupValueDTO { Year = 2024, AnnualAverages = [] },
            new CategoryAnnualGroupValueDTO { Year = 2025, AnnualAverages = [] },
        ];

        await viewModel.RefreshAsync();

        viewModel.AvailableYears.Should().Equal(2023, 2024, 2025);
    }

    [Fact]
    public async Task CategoryTotalsFilter_UncheckingCategory_ExcludesItButKeepsSpacersAndEmphasizedRows()
    {
        var (viewModel, categoryService, _, _) = CreateViewModel();
        categoryService.CategoryTotalsAnnual = new CategoryTotalsAnnualDTO
        {
            CategoryTotals =
            [
                new CategoryAnnualTotalDTO { Category = "Mercado", MonthlyTotals = MonthlyArray(100m), AnnualTotal = 1200m, Average = 100m },
                new CategoryAnnualTotalDTO { Category = "Transporte", MonthlyTotals = MonthlyArray(50m), AnnualTotal = 600m, Average = 50m },
            ],
            IncomeSummary = new IncomeAnnualSummaryDTO
            {
                SalaryMonthly = MonthlyArray(1000m), SalaryAnnualTotal = 12000m, SalaryAverage = 1000m,
                SalaryAfterTaxesMonthly = MonthlyArray(800m), SalaryAfterTaxesAnnualTotal = 9600m, SalaryAfterTaxesAverage = 800m,
                TaxDifferenceMonthly = MonthlyArray(200m), TaxDifferenceAnnualTotal = 2400m, TaxDifferenceAverage = 200m,
                DividendoJurosMonthly = MonthlyArray(10m), DividendoJurosAnnualTotal = 120m, DividendoJurosAverage = 10m,
            },
            TotalDespesasMonthly = MonthlyArray(150m), TotalDespesasAnnualTotal = 1800m, TotalDespesasAverage = 150m,
            ResultadoMonthly = MonthlyArray(660m), ResultadoAnnualTotal = 7920m, ResultadoAverage = 660m,
        };

        await viewModel.RefreshAsync();

        viewModel.FilteredCategoryTotalRows.Should().HaveCount(viewModel.CategoryTotalRows.Count);

        var mercadoOption = viewModel.CategoryTotalsFilter.Options.Single(o => o.Value == "Mercado");
        mercadoOption.IsChecked = false;

        viewModel.FilteredCategoryTotalRows.Select(r => r.Label).Should().NotContain("Mercado");
        viewModel.FilteredCategoryTotalRows.Should().Contain(r => r.Label == "Transporte");
        viewModel.FilteredCategoryTotalRows.Count(r => r.IsSpacer).Should().Be(viewModel.CategoryTotalRows.Count(r => r.IsSpacer));
        viewModel.FilteredCategoryTotalRows.Should().Contain(r => r.Label == "Resultado (R-D-Inv)" && r.IsEmphasized);
        viewModel.FilteredCategoryTotalRows.Should().Contain(r => r.Label == "Total despesas" && r.IsEmphasized);
    }

    [Fact]
    public async Task HistoricSummaryFilter_UncheckingCategory_ExcludesItButKeepsSpacersAndEmphasizedRows()
    {
        var (viewModel, _, _, historicService) = CreateViewModel();
        historicService.HistoricSummaryAverage =
        [
            new CategoryAnnualGroupValueDTO
            {
                Year = 2024,
                AnnualAverages =
                [
                    new CategoryGroupValueDTO { Category = "Tax difference", Value = 100m },
                    new CategoryGroupValueDTO { Category = "Reserva", Value = 200m },
                    new CategoryGroupValueDTO { Category = "Resultado (R-D-Inv)", Value = 300m },
                ],
            },
        ];

        await viewModel.RefreshAsync();

        var reservaOption = viewModel.HistoricSummaryFilter.Options.Single(o => o.Value == "Reserva");
        reservaOption.IsChecked = false;

        viewModel.FilteredHistoricSummaryRows.Select(r => r.Category).Should().NotContain("Reserva");
        viewModel.FilteredHistoricSummaryRows.Should().Contain(r => r.Category == "Tax difference");
        viewModel.FilteredHistoricSummaryRows.Count(r => r.IsSpacer).Should().Be(viewModel.HistoricSummaryRows.Count(r => r.IsSpacer));
        viewModel.FilteredHistoricSummaryRows.Should().Contain(r => r.Category == "Resultado (R-D-Inv)" && r.IsEmphasized);
    }

    [Fact]
    public async Task SettingYear_RefetchesAllThreeSubTabs()
    {
        var (viewModel, categoryService, investmentService, historicService) = CreateViewModel();
        await viewModel.RefreshAsync();
        var categoryCallsBefore = categoryService.GetCategoryTotalsAnnualForYearCallCount;
        var investmentCallsBefore = investmentService.GetInvestmentAnnualResultForYearCallCount;
        var historicCallsBefore = historicService.GetHistoricSummaryAverageFromYearCallCount;

        viewModel.Year = viewModel.Year - 1;
        await viewModel.RefreshAsync();

        categoryService.GetCategoryTotalsAnnualForYearCallCount.Should().BeGreaterThan(categoryCallsBefore);
        investmentService.GetInvestmentAnnualResultForYearCallCount.Should().BeGreaterThan(investmentCallsBefore);
        historicService.GetHistoricSummaryAverageFromYearCallCount.Should().BeGreaterThan(historicCallsBefore);
    }
}
