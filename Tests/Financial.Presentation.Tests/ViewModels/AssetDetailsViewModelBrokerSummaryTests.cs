using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelBrokerSummaryTests
{
    private static readonly TimeSpan AsyncWaitTimeout = TimeSpan.FromSeconds(2);

    private static AssetDetailsViewModel BuildViewModel(IBrokerBreakdownQueryService? brokerBreakdownQueryService = null)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            new StubAssetPriceService(),
            brokerBreakdownQueryService ?? new StubBrokerBreakdownQueryService());
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
    public void LoadBrokerSummary_SetsNegativeTotalInvested()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 1000m, TotalSold = 5000m, TotalInvested = -4000m };
        vm.LoadBrokerSummary("XPI", summary, []);
        vm.TotalInvested.Should().Be(-4000m);
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
        var stub = new StubBrokerBreakdownQueryService();
        var vm = BuildViewModel(stub);
        vm.LoadBrokerBreakdown("XPI");
        vm.IsBreakdownLoading.Should().BeTrue();
    }

    [Fact]
    public void LoadBrokerBreakdown_PopulatesOverallBreakdownPlotModel_OnSuccess()
    {
        var stub = new StubBrokerBreakdownQueryService
        {
            Breakdown =
            [
                new PortfolioBreakdownItemDTO { PortfolioName = "Acoes", TotalInvested = 1000m, Assets = [] },
                new PortfolioBreakdownItemDTO { PortfolioName = "FII", TotalInvested = 2000m, Assets = [] },
            ],
        };
        var vm = BuildViewModel(stub);
        vm.LoadBrokerBreakdown("XPI");
        SpinWait.SpinUntil(() => !vm.IsBreakdownLoading, AsyncWaitTimeout);

        vm.OverallBreakdownPlotModel.Should().NotBeNull();
        vm.OverallBreakdownPlotModel!.Series.Should().HaveCount(1);
    }

    [Fact]
    public void LoadBrokerBreakdown_PopulatesPortfolioBreakdownPieItems_OnSuccess()
    {
        var stub = new StubBrokerBreakdownQueryService
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
        vm.LoadBrokerBreakdown("XPI");
        SpinWait.SpinUntil(() => !vm.IsBreakdownLoading, AsyncWaitTimeout);

        vm.PortfolioBreakdownPieItems.Should().HaveCount(2);
        vm.PortfolioBreakdownPieItems[0].PortfolioName.Should().Be("Acoes");
        vm.PortfolioBreakdownPieItems[0].PlotModel.Should().NotBeNull();
        vm.PortfolioBreakdownPieItems[1].PortfolioName.Should().Be("FII");
    }

    [Fact]
    public void LoadBrokerBreakdown_SetsIsBreakdownLoadingFalse_OnSuccess()
    {
        var stub = new StubBrokerBreakdownQueryService { Breakdown = [] };
        var vm = BuildViewModel(stub);
        vm.LoadBrokerBreakdown("XPI");
        SpinWait.SpinUntil(() => !vm.IsBreakdownLoading, AsyncWaitTimeout);
        vm.IsBreakdownLoading.Should().BeFalse();
    }

    [Fact]
    public void LoadBrokerBreakdown_SetsBreakdownError_OnFailure()
    {
        var stub = new StubBrokerBreakdownQueryService { ExceptionToThrow = new InvalidOperationException("boom") };
        var vm = BuildViewModel(stub);
        vm.LoadBrokerBreakdown("XPI");
        SpinWait.SpinUntil(() => !vm.IsBreakdownLoading, AsyncWaitTimeout);

        vm.BreakdownError.Should().NotBeNull();
        vm.IsBreakdownLoading.Should().BeFalse();
    }

    [Fact]
    public void Clear_AfterLoadBrokerBreakdown_ResetsBreakdownState()
    {
        var stub = new StubBrokerBreakdownQueryService
        {
            Breakdown = [new PortfolioBreakdownItemDTO { PortfolioName = "Acoes", TotalInvested = 1000m, Assets = [] }],
        };
        var vm = BuildViewModel(stub);
        vm.LoadBrokerBreakdown("XPI");
        SpinWait.SpinUntil(() => !vm.IsBreakdownLoading, AsyncWaitTimeout);

        vm.Clear();

        vm.OverallBreakdownPlotModel.Should().BeNull();
        vm.PortfolioBreakdownPieItems.Should().BeEmpty();
        vm.IsBreakdownLoading.Should().BeFalse();
        vm.BreakdownError.Should().BeNull();
    }

    [Fact]
    public void LoadAssetDetails_AfterLoadBrokerBreakdown_ResetsBreakdownState()
    {
        var stub = new StubBrokerBreakdownQueryService
        {
            Breakdown = [new PortfolioBreakdownItemDTO { PortfolioName = "Acoes", TotalInvested = 1000m, Assets = [] }],
        };
        var vm = BuildViewModel(stub);
        vm.LoadBrokerBreakdown("XPI");
        SpinWait.SpinUntil(() => !vm.IsBreakdownLoading, AsyncWaitTimeout);

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

    private sealed class StubBrokerBreakdownQueryService : IBrokerBreakdownQueryService
    {
        public IReadOnlyList<PortfolioBreakdownItemDTO> Breakdown { get; set; } = [];
        public Exception? ExceptionToThrow { get; set; }
        public string? LastBrokerName { get; private set; }

        public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName)
        {
            LastBrokerName = brokerName;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Breakdown;
        }
    }

    private sealed class StubAssetPriceService : IAssetPriceService
    {
        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request) =>
            new() { Exchange = request.Exchange, Ticker = request.Ticker, Price = 0m };
    }

    private sealed class StubTransactionService : ITransactionService
    {
        public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    }

    private sealed class StubCreditService : ICreditService
    {
        public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    }
}
