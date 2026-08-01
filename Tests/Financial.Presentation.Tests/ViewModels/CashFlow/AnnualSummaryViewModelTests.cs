using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class AnnualSummaryViewModelTests
{
    private static (AnnualSummaryViewModel ViewModel, StubAnnualSummaryService Service) CreateViewModel()
    {
        var service = new StubAnnualSummaryService();
        var viewModel = new AnnualSummaryViewModel(service);
        return (viewModel, service);
    }

    private static decimal[] MonthlyArray(decimal value) => Enumerable.Repeat(value, 12).ToArray();

    [Fact]
    public async Task RefreshAsync_BuildsCategoryTotalsRowsInCorrectOrderWithSpacersAndEmphasis()
    {
        var (viewModel, service) = CreateViewModel();
        service.CategoryTotalsAnnual = new CategoryTotalsAnnualDTO
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
        var (viewModel, service) = CreateViewModel();
        var monthlyDiffs = new decimal?[12];
        monthlyDiffs[0] = null;
        for (var i = 1; i < 12; i++)
        {
            monthlyDiffs[i] = 5m;
        }

        service.InvestmentAnnualResult = new InvestmentAnnualResultDTO
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
        var (viewModel, service) = CreateViewModel();
        service.InvestmentAnnualResult = new InvestmentAnnualResultDTO
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
        var (viewModel, service) = CreateViewModel();
        service.HistoricSummaryAverage =
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
    public async Task RefreshAsync_ExposesAvailableYearsFromHistoricSummaryResponse()
    {
        var (viewModel, service) = CreateViewModel();
        service.HistoricSummaryAverage =
        [
            new CategoryAnnualGroupValueDTO { Year = 2023, AnnualAverages = [] },
            new CategoryAnnualGroupValueDTO { Year = 2024, AnnualAverages = [] },
            new CategoryAnnualGroupValueDTO { Year = 2025, AnnualAverages = [] },
        ];

        await viewModel.RefreshAsync();

        viewModel.AvailableYears.Should().Equal(2023, 2024, 2025);
    }

    [Fact]
    public async Task SettingYear_RefetchesAllThreeSubTabs()
    {
        var (viewModel, service) = CreateViewModel();
        await viewModel.RefreshAsync();
        var categoryCallsBefore = service.GetCategoryTotalsAnnualForYearCallCount;
        var investmentCallsBefore = service.GetInvestmentAnnualResultForYearCallCount;
        var historicCallsBefore = service.GetHistoricSummaryAverageFromYearCallCount;

        viewModel.Year = viewModel.Year - 1;
        await viewModel.RefreshAsync();

        service.GetCategoryTotalsAnnualForYearCallCount.Should().BeGreaterThan(categoryCallsBefore);
        service.GetInvestmentAnnualResultForYearCallCount.Should().BeGreaterThan(investmentCallsBefore);
        service.GetHistoricSummaryAverageFromYearCallCount.Should().BeGreaterThan(historicCallsBefore);
    }
}
