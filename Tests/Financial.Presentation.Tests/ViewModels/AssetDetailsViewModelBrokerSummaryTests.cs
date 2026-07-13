using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelBrokerSummaryTests
{
    private static AssetDetailsViewModel BuildViewModel(
        IBrokerBreakdownService? brokerBreakdownService = null,
        ITransactionQueryService? transactionQueryService = null)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            new StubAssetPriceService(),
            brokerBreakdownService ?? new StubBrokerBreakdownService(),
            transactionQueryService ?? new StubTransactionQueryService(),
            new XirrCalculationService());
    }

    [Fact]
    public void LoadBrokerSummary_SetsIsBrokerViewTrue()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.IsBrokerView.Should().BeTrue();
    }

    [Fact]
    public void LoadBrokerSummary_SetsAggregatedTotals()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 20000m, TotalSold = 9000m };
        vm.LoadBrokerSummary("XPI", summary, []);
        vm.TotalBought.Should().Be(20000m);
        vm.TotalSold.Should().Be(9000m);
    }

    [Fact]
    public void LoadBrokerSummary_SetsTotalInvested()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 20000m, TotalSold = 9000m, TotalInvested = 11000m };
        vm.LoadBrokerSummary("XPI", summary, []);
        vm.TotalInvested.Should().Be(11000m);
    }

    [Fact]
    public void LoadBrokerSummary_LoadsCreditsForCreditsTab()
    {
        var vm = BuildViewModel();
        var credits = new List<CreditDTO>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 100m },
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Interest", Value = 50m }
        };
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), credits);
        vm.Credits.Count.Should().Be(2);
    }

    [Fact]
    public void LoadBrokerSummary_ClearsAssetSpecificFields()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(new AssetDetailsDTO
        {
            Name = "Asset A", BrokerName = "XPI", PortfolioName = "Portfolio",
            Ticker = "T", ISIN = "US1234567890", Exchange = "LSE",
            Country = Financial.Domain.Entities.CountryCode.Unknown,
            LocalTypeCode = "", Class = Financial.Domain.Entities.GlobalAssetClass.Unknown,
            Quantity = 10m, AveragePrice = 50m, IsActive = true,
            TotalBought = 500m, TotalSold = 0m, TotalCredits = 0m,
            Transactions = [], Credits = []
        });

        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);

        vm.AssetName.Should().BeEmpty();
        vm.ISIN.Should().BeEmpty();
        vm.Quantity.Should().Be(0m);
        vm.AveragePrice.Should().Be(0m);
    }

    [Fact]
    public void Clear_AfterLoadBrokerSummary_SetsIsBrokerViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.Clear();
        vm.IsBrokerView.Should().BeFalse();
    }

    [Fact]
    public void Clear_AfterLoadBrokerSummary_ResetsTotalInvested()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 20000m, TotalSold = 9000m, TotalInvested = 11000m };
        vm.LoadBrokerSummary("XPI", summary, []);
        vm.Clear();
        vm.TotalInvested.Should().Be(0m);
    }

    [Fact]
    public void LoadAssetDetails_AfterBrokerSummary_SetsIsBrokerViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.LoadAssetDetails(new AssetDetailsDTO
        {
            Name = "Asset A", BrokerName = "XPI", PortfolioName = "Portfolio",
            Ticker = "T", ISIN = "", Exchange = "LSE",
            Country = Financial.Domain.Entities.CountryCode.Unknown,
            LocalTypeCode = "", Class = Financial.Domain.Entities.GlobalAssetClass.Unknown,
            Quantity = 0m, AveragePrice = 0m, IsActive = true,
            TotalBought = 0m, TotalSold = 0m, TotalCredits = 0m,
            Transactions = [], Credits = []
        });
        vm.IsBrokerView.Should().BeFalse();
    }

    [Fact]
    public void LoadPortfolioSummary_AfterBrokerSummary_SetsIsBrokerViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.LoadPortfolioSummary("XPI", "Portfolio", new AggregatedSummaryDTO(), [], []);
        vm.IsBrokerView.Should().BeFalse();
        vm.IsPortfolioView.Should().BeTrue();
    }

    [Fact]
    public void LoadBrokerSummary_AfterPortfolioSummary_SetsIsPortfolioViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("XPI", "Portfolio", new AggregatedSummaryDTO(), [], []);
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.IsPortfolioView.Should().BeFalse();
        vm.IsBrokerView.Should().BeTrue();
    }

    [Fact]
    public void LoadBrokerBreakdown_SetsIsBreakdownLoadingTrue_Synchronously()
    {
        var stub = new StubBrokerBreakdownService();
        var vm = BuildViewModel(stub);
        _ = vm.LoadBrokerBreakdown("XPI");
        vm.IsBreakdownLoading.Should().BeTrue();
    }

    [Fact]
    public async Task LoadBrokerBreakdown_PopulatesOverallBreakdownPlotModel_OnSuccess()
    {
        var stub = new StubBrokerBreakdownService
        {
            Breakdown =
            [
                new PortfolioBreakdownItemDTO { PortfolioName = "Acoes", TotalInvested = 1000m, Assets = [] },
                new PortfolioBreakdownItemDTO { PortfolioName = "FII", TotalInvested = 2000m, Assets = [] },
            ],
        };
        var vm = BuildViewModel(stub);
        await vm.LoadBrokerBreakdown("XPI");

        vm.OverallBreakdownPlotModel.Should().NotBeNull();
        vm.OverallBreakdownPlotModel!.Series.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadBrokerBreakdown_PopulatesPortfolioBreakdownPieItems_OnSuccess()
    {
        var stub = new StubBrokerBreakdownService
        {
            Breakdown =
            [
                new PortfolioBreakdownItemDTO
                {
                    PortfolioName = "Acoes",
                    TotalInvested = 1000m,
                    Assets = [new AssetBreakdownItemDTO { AssetName = "BBAS3", TotalInvested = 1000m }],
                },
                new PortfolioBreakdownItemDTO
                {
                    PortfolioName = "FII",
                    TotalInvested = 2000m,
                    Assets = [new AssetBreakdownItemDTO { AssetName = "MXRF11", TotalInvested = 2000m }],
                },
            ],
        };
        var vm = BuildViewModel(stub);
        await vm.LoadBrokerBreakdown("XPI");

        vm.PortfolioBreakdownPieItems.Should().HaveCount(2);
        vm.PortfolioBreakdownPieItems[0].PortfolioName.Should().Be("Acoes");
        vm.PortfolioBreakdownPieItems[0].PlotModel.Should().NotBeNull();
        vm.PortfolioBreakdownPieItems[1].PortfolioName.Should().Be("FII");
    }

    [Fact]
    public async Task LoadBrokerBreakdown_SetsIsBreakdownLoadingFalse_OnSuccess()
    {
        var stub = new StubBrokerBreakdownService { Breakdown = [] };
        var vm = BuildViewModel(stub);
        await vm.LoadBrokerBreakdown("XPI");
        vm.IsBreakdownLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadBrokerBreakdown_SetsBreakdownError_OnFailure()
    {
        var stub = new StubBrokerBreakdownService { ExceptionToThrow = new InvalidOperationException("boom") };
        var vm = BuildViewModel(stub);
        await vm.LoadBrokerBreakdown("XPI");

        vm.BreakdownError.Should().NotBeNull();
        vm.IsBreakdownLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Clear_AfterLoadBrokerBreakdown_ResetsBreakdownState()
    {
        var stub = new StubBrokerBreakdownService
        {
            Breakdown = [new PortfolioBreakdownItemDTO { PortfolioName = "Acoes", TotalInvested = 1000m, Assets = [] }],
        };
        var vm = BuildViewModel(stub);
        await vm.LoadBrokerBreakdown("XPI");

        vm.Clear();

        vm.OverallBreakdownPlotModel.Should().BeNull();
        vm.PortfolioBreakdownPieItems.Should().BeEmpty();
        vm.IsBreakdownLoading.Should().BeFalse();
        vm.BreakdownError.Should().BeNull();
    }

    [Fact]
    public async Task LoadAssetDetails_AfterLoadBrokerBreakdown_ResetsBreakdownState()
    {
        var stub = new StubBrokerBreakdownService
        {
            Breakdown = [new PortfolioBreakdownItemDTO { PortfolioName = "Acoes", TotalInvested = 1000m, Assets = [] }],
        };
        var vm = BuildViewModel(stub);
        await vm.LoadBrokerBreakdown("XPI");

        vm.LoadAssetDetails(new AssetDetailsDTO
        {
            Name = "Asset A", BrokerName = "XPI", PortfolioName = "Portfolio",
            Ticker = "T", ISIN = "", Exchange = "LSE",
            Country = Financial.Domain.Entities.CountryCode.Unknown,
            LocalTypeCode = "", Class = Financial.Domain.Entities.GlobalAssetClass.Unknown,
            Quantity = 0m, AveragePrice = 0m, IsActive = true,
            TotalBought = 0m, TotalSold = 0m, TotalCredits = 0m,
            Transactions = [], Credits = []
        });

        vm.OverallBreakdownPlotModel.Should().BeNull();
        vm.PortfolioBreakdownPieItems.Should().BeEmpty();
    }

}
