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

    private static AssetDetailsDTO BuildAssetDetails(
        string brokerName,
        string assetName,
        IReadOnlyList<AssetPriceSnapshotDTO> priceHistory,
        IReadOnlyList<TransactionDTO>? transactions = null) => new()
    {
        Name = assetName,
        BrokerName = brokerName,
        PortfolioName = "Default",
        Ticker = assetName,
        Exchange = "BVMF",
        PriceHistory = priceHistory.ToList(),
        Transactions = (transactions ?? []).ToList()
    };

    [Fact]
    public void LoadAssetDetails_DefaultsToLast12MonthsFilter()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", []));

        vm.PriceHistory.PriceHistoryFilters.First(f => f.Value == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();
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

        vm.PriceHistory.PriceHistory.Should().ContainSingle(e => e.Price == 100m);
        vm.PriceHistory.PriceHistoryPlotModel.Should().NotBeNull();
    }

    [Fact]
    public void SelectPriceHistoryFilter_PersistsSelectionPerAsset()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails("XPI", "AssetA", []));

        vm.PriceHistory.SelectPriceHistoryFilterCommand.Execute(PeriodFilter.AllTime);
        vm.PriceHistory.PriceHistoryFilters.First(f => f.Value == PeriodFilter.AllTime).IsSelected.Should().BeTrue();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "AssetB", []));
        vm.PriceHistory.PriceHistoryFilters.First(f => f.Value == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "AssetA", []));
        vm.PriceHistory.PriceHistoryFilters.First(f => f.Value == PeriodFilter.AllTime).IsSelected.Should().BeTrue();
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

        vm.PriceHistory.SelectPriceHistoryFilterCommand.Execute(PeriodFilter.ThisMonth);

        vm.PriceHistory.PriceHistoryPlotModel.Should().NotBeNull();
        var line = vm.PriceHistory.PriceHistoryPlotModel!.Series.OfType<OxyPlot.Series.LineSeries>().Single();
        line.Points.Should().ContainSingle();
    }

    [Fact]
    public void Clear_ResetsPriceHistoryState()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", [new() { Date = DateOnly.FromDateTime(DateTime.Today), Price = 100m, IsManual = true }]));

        vm.Clear();

        vm.PriceHistory.PriceHistory.Should().BeEmpty();
        vm.PriceHistory.PriceHistoryPlotModel.Should().BeNull();
        vm.PriceHistory.SelectedPriceEntry.Should().BeNull();
    }

    [Fact]
    public void LoadAssetDetails_PopulatesBuyAndSellSeriesFromTransactions()
    {
        var transactions = new List<TransactionDTO>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 90m, Fees = 1m, TotalPrice = 901m },
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Sell", Quantity = 5m, UnitPrice = 130m, Fees = 1m, TotalPrice = 649m },
        };
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", [], transactions));

        var scatterSeries = vm.PriceHistory.PriceHistoryPlotModel!.Series.OfType<OxyPlot.Series.ScatterSeries>().ToList();
        scatterSeries.Should().Contain(s => s.Title == "Buy" && s.Points.Count == 1);
        scatterSeries.Should().Contain(s => s.Title == "Sell" && s.Points.Count == 1);
    }

    [Fact]
    public void SelectPriceHistoryFilter_ExcludesTransactionsOutsideWindow()
    {
        var transactions = new List<TransactionDTO>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 10m, UnitPrice = 90m, Fees = 1m, TotalPrice = 901m },
            new() { Id = Guid.NewGuid(), Date = DateTime.Today.AddYears(-2), Type = "Buy", Quantity = 10m, UnitPrice = 80m, Fees = 1m, TotalPrice = 801m },
        };
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails("XPI", "TEST", [], transactions));

        vm.PriceHistory.SelectPriceHistoryFilterCommand.Execute(PeriodFilter.ThisMonth);

        var buy = vm.PriceHistory.PriceHistoryPlotModel!.Series.OfType<OxyPlot.Series.ScatterSeries>().Single(s => s.Title == "Buy");
        buy.Points.Should().ContainSingle();
    }
}
