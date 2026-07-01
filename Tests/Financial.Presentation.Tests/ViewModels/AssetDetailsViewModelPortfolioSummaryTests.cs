using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelPortfolioSummaryTests
{
    private static AssetDetailsViewModel BuildViewModel(IAssetPriceService? priceService = null)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            priceService ?? new NeverResolvingPriceService());
    }

    private static IReadOnlyList<PortfolioAssetSummaryItemDTO> BuildItems(int count = 2)
    {
        return Enumerable.Range(1, count).Select(i => new PortfolioAssetSummaryItemDTO
        {
            AssetName = $"Asset {i}",
            Ticker = $"T{i}",
            Exchange = "LSE",
            CurrentQuantity = 10m * i,
            TotalBought = 1000m * i,
            TotalSold = 0m,
            TotalInvested = 1000m * i,
            PortfolioWeight = 50m
        }).ToList();
    }

    [Fact]
    public void LoadPortfolioSummary_SetsIsPortfolioViewTrue()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems());
        vm.IsPortfolioView.Should().BeTrue();
    }

    [Fact]
    public void LoadPortfolioSummary_PopulatesPortfolioAssetSummaryRows()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(2));
        vm.PortfolioAssetSummaryRows.Count.Should().Be(2);
    }

    [Fact]
    public void LoadPortfolioSummary_RowsAreInCorrectOrder()
    {
        var vm = BuildViewModel();
        var items = BuildItems(2);
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);
        vm.PortfolioAssetSummaryRows[0].AssetName.Should().Be(items[0].AssetName);
        vm.PortfolioAssetSummaryRows[1].AssetName.Should().Be(items[1].AssetName);
    }

    [Fact]
    public void LoadPortfolioSummary_SetsAggregatedTotals()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 10000m, TotalSold = 2000m };
        vm.LoadPortfolioSummary("Broker", "Portfolio", summary, [], BuildItems());
        vm.TotalBought.Should().Be(10000m);
    }

    [Fact]
    public void LoadPortfolioSummary_LoadsCreditsForCreditsTab()
    {
        var vm = BuildViewModel();
        var credits = new List<CreditDTO>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 100m },
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Interest", Value = 50m }
        };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), credits, BuildItems());
        vm.Credits.Count.Should().Be(2);
    }

    [Fact]
    public void LoadPortfolioSummary_RowsInitiallyShowLoadingPrice()
    {
        var vm = BuildViewModel(new NeverResolvingPriceService());
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(3));
        vm.PortfolioAssetSummaryRows.All(r => r.IsLoadingPrice).Should().BeTrue();
    }

    [Fact]
    public void Clear_AfterLoadPortfolioSummary_ClearsRows()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(2));
        vm.Clear();
        vm.PortfolioAssetSummaryRows.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_AfterLoadPortfolioSummary_SetsIsPortfolioViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems());
        vm.Clear();
        vm.IsPortfolioView.Should().BeFalse();
    }

    [Fact]
    public void LoadAssetDetails_AfterPortfolioSummary_SetsIsPortfolioViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems());
        vm.LoadAssetDetails(new AssetDetailsDTO
        {
            Name = "Asset A", BrokerName = "Broker", PortfolioName = "Portfolio",
            Ticker = "T", ISIN = "", Exchange = "LSE",
            Country = Financial.Domain.Entities.CountryCode.Unknown,
            LocalTypeCode = "", Class = Financial.Domain.Entities.GlobalAssetClass.Unknown,
            Quantity = 0m, AveragePrice = 0m, IsActive = true,
            TotalBought = 0m, TotalSold = 0m, TotalCredits = 0m,
            Transactions = [], Credits = []
        });
        vm.IsPortfolioView.Should().BeFalse();
    }

    private sealed class NeverResolvingPriceService : IAssetPriceService
    {
        private readonly SemaphoreSlim _blocker = new SemaphoreSlim(0);

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
        {
            _blocker.Wait();
            return new AssetPriceDTO { Exchange = request.Exchange, Ticker = request.Ticker, Price = 0m };
        }
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
