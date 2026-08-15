using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelPriceHistoryChartTests
{
    private static AssetDetailsViewModel BuildViewModel()
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            new StubAssetPriceService(),
            new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            new XirrCalculationService(),
            new ProfitCalculationService());
    }

    private static AssetDetailsDTO BuildAssetDetails(string brokerName, string assetName, IReadOnlyList<AssetPriceSnapshotDTO> priceHistory) => new()
    {
        Name = assetName,
        BrokerName = brokerName,
        PortfolioName = "Default",
        Ticker = assetName,
        Exchange = "BVMF",
        PriceHistory = priceHistory.ToList()
    };

    [Fact]
    public void LoadAssetDetails_DefaultsToLast12MonthsFilter()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", []));

        vm.PriceHistoryFilters.First(f => f.Filter == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void LoadAssetDetails_PopulatesPriceHistoryCollection()
    {
        var entries = new List<AssetPriceSnapshotDTO>
        {
            new() { Date = new DateOnly(2026, 8, 15), Price = 100m, IsManual = true },
        };
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", entries));

        vm.PriceHistory.Should().ContainSingle(e => e.Price == 100m);
        vm.PriceHistoryPlotModel.Should().NotBeNull();
    }

    [Fact]
    public void SelectPriceHistoryFilter_PersistsSelectionPerAsset()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails("XPI", "AssetA", []));

        vm.SelectPriceHistoryFilterCommand.Execute(PeriodFilter.AllTime);
        vm.PriceHistoryFilters.First(f => f.Filter == PeriodFilter.AllTime).IsSelected.Should().BeTrue();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "AssetB", []));
        vm.PriceHistoryFilters.First(f => f.Filter == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "AssetA", []));
        vm.PriceHistoryFilters.First(f => f.Filter == PeriodFilter.AllTime).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectPriceHistoryFilter_ExcludesEntriesOutsideWindow()
    {
        var entries = new List<AssetPriceSnapshotDTO>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Price = 100m, IsManual = true },
            new() { Date = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)), Price = 50m, IsManual = false },
        };
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", entries));

        vm.SelectPriceHistoryFilterCommand.Execute(PeriodFilter.ThisMonth);

        vm.PriceHistoryPlotModel.Should().NotBeNull();
        var line = vm.PriceHistoryPlotModel!.Series.OfType<OxyPlot.Series.LineSeries>().Single();
        line.Points.Should().ContainSingle();
    }

    [Fact]
    public void Clear_ResetsPriceHistoryState()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", [new() { Date = DateOnly.FromDateTime(DateTime.Today), Price = 100m, IsManual = true }]));

        vm.Clear();

        vm.PriceHistory.Should().BeEmpty();
        vm.PriceHistoryPlotModel.Should().BeNull();
        vm.SelectedPriceEntry.Should().BeNull();
    }
}
