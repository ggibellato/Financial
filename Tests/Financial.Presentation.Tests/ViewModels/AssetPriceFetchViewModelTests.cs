using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Collections.Specialized;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetPriceFetchViewModelTests
{
    private readonly StubNavigationService _navigationService;
    private readonly StubPriceService _priceService;

    public AssetPriceFetchViewModelTests()
    {
        _navigationService = new StubNavigationService();
        _priceService = new StubPriceService();
    }

    /// <summary>Builds the view model over the shared stubs for one configured broker/portfolio pair,
    /// which is the only thing that varies between these tests.</summary>
    private AssetPriceFetchViewModel CreateViewModel(string brokerName, string portfolioName) =>
        new(_navigationService, _priceService,
            Options.Create(new AssetPriceFetchOptions
            {
                Portfolios = [new AssetPriceFetch { BrokerName = brokerName, PortfolioName = portfolioName }]
            }),
            _ => { });

    [Fact]
    public async Task FetchAsync_CoinbaseCryptocurrencyAsset_PassesAssetClassAndBrokerName()
    {
        _navigationService.AssetsByBrokerPortfolio[("Coinbase", "Cryptocurrency")] =
        [
            new AssetNodeDTO
            {
                Name = "Bitcoin",
                Ticker = "BTC",
                Exchange = "",
                Class = GlobalAssetClass.Cryptocurrency,
            }
        ];
        var vm = CreateViewModel("Coinbase", "Cryptocurrency");

        vm.FetchCommand.Execute(null);
        var request = await _priceService.RequestReceived.WaitAsync(TimeSpan.FromSeconds(5));

        request.Ticker.Should().Be("BTC");
        request.Exchange.Should().Be("");
        request.AssetClass.Should().Be(GlobalAssetClass.Cryptocurrency);
        request.BrokerName.Should().Be("Coinbase");
    }

    [Fact]
    public async Task FetchAsync_NonCryptocurrencyAsset_RequestUnaffected()
    {
        _navigationService.AssetsByBrokerPortfolio[("XPI", "Acoes")] =
        [
            new AssetNodeDTO
            {
                Name = "KLBN4",
                Ticker = "KLBN4",
                Exchange = "BVMF",
                Class = GlobalAssetClass.Equity,
            }
        ];
        var vm = CreateViewModel("XPI", "Acoes");

        vm.FetchCommand.Execute(null);
        var request = await _priceService.RequestReceived.WaitAsync(TimeSpan.FromSeconds(5));

        request.Ticker.Should().Be("KLBN4");
        request.Exchange.Should().Be("BVMF");
        request.AssetClass.Should().Be(GlobalAssetClass.Equity);
        request.BrokerName.Should().Be("XPI");
    }

    [Fact]
    public async Task FetchAsync_BondAsset_PassesName()
    {
        _navigationService.AssetsByBrokerPortfolio[("XPI", "Reserva")] =
        [
            new AssetNodeDTO
            {
                Name = "TESOURO IPCA+ 2029",
                Ticker = "TESOURO IPCA+ 2029",
                Exchange = "BVMF",
                Class = GlobalAssetClass.Bond,
            }
        ];
        var vm = CreateViewModel("XPI", "Reserva");

        vm.FetchCommand.Execute(null);
        var request = await _priceService.RequestReceived.WaitAsync(TimeSpan.FromSeconds(5));

        request.AssetClass.Should().Be(GlobalAssetClass.Bond);
        request.Name.Should().Be("TESOURO IPCA+ 2029");
    }

    /// <summary>
    /// Broker, portfolio and asset name together are what make the orchestration record the
    /// fetched price into Price History. Without them this screen took the lookup-only path and
    /// built no history at all.
    /// </summary>
    [Fact]
    public async Task FetchAsync_SuppliesTheAssetIdentityThatEnablesRecording()
    {
        _navigationService.AssetsByBrokerPortfolio[("XPI", "Acoes")] =
        [
            new AssetNodeDTO { Name = "TASA4", Ticker = "TASA4", Exchange = "BVMF", Class = GlobalAssetClass.Equity }
        ];
        var vm = CreateViewModel("XPI", "Acoes");

        vm.FetchCommand.Execute(null);
        var request = await _priceService.RequestReceived.WaitAsync(TimeSpan.FromSeconds(5));

        request.BrokerName.Should().Be("XPI");
        request.PortfolioName.Should().Be("Acoes");
        request.AssetName.Should().Be("TASA4");
    }

    [Fact]
    public async Task FetchAsync_OneAssetFails_StillFetchesEveryRemainingAsset()
    {
        _navigationService.AssetsByBrokerPortfolio[("XPI", "Acoes")] =
        [
            new AssetNodeDTO { Name = "Asset 1", Ticker = "AAA1", Exchange = "BVMF", Class = GlobalAssetClass.Equity },
            new AssetNodeDTO { Name = "Asset 2", Ticker = "AAA2", Exchange = "BVMF", Class = GlobalAssetClass.Equity },
            new AssetNodeDTO { Name = "Asset 3", Ticker = "AAA3", Exchange = "BVMF", Class = GlobalAssetClass.Equity },
        ];
        _priceService.TickersToFail.Add("AAA2");
        var vm = CreateViewModel("XPI", "Acoes");

        vm.FetchCommand.Execute(null);
        await WaitForResultsAsync(vm, expectedCount: 3, TimeSpan.FromSeconds(5));

        _priceService.AllRequests.Select(r => r.Ticker).Should().BeEquivalentTo(["AAA1", "AAA2", "AAA3"]);
    }

    [Fact]
    public async Task FetchAsync_OneAssetFails_AddsAnErrorRowInsteadOfAbortingTheRun()
    {
        _navigationService.AssetsByBrokerPortfolio[("XPI", "Acoes")] =
        [
            new AssetNodeDTO { Name = "Asset 1", Ticker = "AAA1", Exchange = "BVMF", Class = GlobalAssetClass.Equity },
            new AssetNodeDTO { Name = "Asset 2", Ticker = "AAA2", Exchange = "BVMF", Class = GlobalAssetClass.Equity },
        ];
        _priceService.TickersToFail.Add("AAA2");
        _priceService.FailureMessage = "Unable to fetch price.";
        var vm = CreateViewModel("XPI", "Acoes");

        vm.FetchCommand.Execute(null);
        await WaitForResultsAsync(vm, expectedCount: 2, TimeSpan.FromSeconds(5));

        var successRow = vm.Results.Single(r => r.Ticker == "AAA1");
        successRow.HasError.Should().BeFalse();
        successRow.Price.Should().Be(1m);

        var failedRow = vm.Results.Single(r => r.Ticker == "AAA2");
        failedRow.HasError.Should().BeTrue();
        failedRow.Error.Should().Be("Unable to fetch price.");
        failedRow.Price.Should().BeNull();
    }

    /// <summary>Waits on the collection's own change notifications rather than sampling <c>Results.Count</c>
    /// on a timer, so the test cannot flake on how fast the stub happens to complete.</summary>
    private static async Task WaitForResultsAsync(AssetPriceFetchViewModel vm, int expectedCount, TimeSpan timeout)
    {
        if (vm.Results.Count >= expectedCount)
        {
            return;
        }

        var tcs = new TaskCompletionSource();
        void Handler(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (vm.Results.Count >= expectedCount)
            {
                tcs.TrySetResult();
            }
        }

        vm.Results.CollectionChanged += Handler;
        try
        {
            if (vm.Results.Count >= expectedCount)
            {
                return;
            }

            await tcs.Task.WaitAsync(timeout);
        }
        finally
        {
            vm.Results.CollectionChanged -= Handler;
        }
    }

    private sealed class StubNavigationService : INavigationService
    {
        public Dictionary<(string BrokerName, string PortfolioName), List<AssetNodeDTO>> AssetsByBrokerPortfolio { get; } = new();

        public TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IEnumerable<BrokerNodeDTO> GetBrokers(InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();

        public IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName) =>
            AssetsByBrokerPortfolio.TryGetValue((brokerName, portfolioName), out var assets) ? assets : [];
    }

    private sealed class StubPriceService : IPriceService
    {
        private readonly TaskCompletionSource<AssetPriceRequestDTO> _tcs = new();

        public Task<AssetPriceRequestDTO> RequestReceived => _tcs.Task;
        public List<AssetPriceRequestDTO> AllRequests { get; } = new();

        /// <summary>Tickers in this set fail with <see cref="FailureMessage"/> instead of returning a price.</summary>
        public HashSet<string> TickersToFail { get; } = new();
        public string FailureMessage { get; set; } = "boom";

        public Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request) => throw new NotImplementedException();

        public Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request) => throw new NotImplementedException();

        public Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request)
        {
            _tcs.TrySetResult(request);
            AllRequests.Add(request);

            if (TickersToFail.Contains(request.Ticker))
            {
                throw new InvalidOperationException(FailureMessage);
            }

            return Task.FromResult(new AssetPriceDTO
            {
                Exchange = request.Exchange,
                Ticker = request.Ticker,
                Name = "Test",
                Price = 1m,
                AsOf = DateTimeOffset.UtcNow
            });
        }
    }
}
