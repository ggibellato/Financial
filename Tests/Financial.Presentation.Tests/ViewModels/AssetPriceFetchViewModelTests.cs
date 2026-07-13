using Financial.Application.Configuration;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetPriceFetchViewModelTests
{
    [Fact]
    public async Task FetchAsync_CoinbaseCryptocurrencyAsset_PassesAssetClassAndBrokerName()
    {
        var navigationService = new StubNavigationService();
        navigationService.AssetsByBrokerPortfolio[("Coinbase", "Cryptocurrency")] =
        [
            new AssetNodeDTO
            {
                Name = "Bitcoin",
                Ticker = "BTC",
                Exchange = "",
                Class = GlobalAssetClass.Cryptocurrency,
                IsActive = true,
            }
        ];
        var priceService = new StubAssetPriceService();
        var options = Options.Create(new AssetPriceFetchOptions
        {
            Portfolios = [new AssetPriceFetch { BrokerName = "Coinbase", PortfolioName = "Cryptocurrency" }]
        });
        var vm = new AssetPriceFetchViewModel(navigationService, priceService, options, _ => { });

        vm.FetchCommand.Execute(null);
        var request = await priceService.RequestReceived.WaitAsync(TimeSpan.FromSeconds(5));

        request.Ticker.Should().Be("BTC");
        request.Exchange.Should().Be("");
        request.AssetClass.Should().Be(GlobalAssetClass.Cryptocurrency);
        request.BrokerName.Should().Be("Coinbase");
    }

    [Fact]
    public async Task FetchAsync_NonCryptocurrencyAsset_RequestUnaffected()
    {
        var navigationService = new StubNavigationService();
        navigationService.AssetsByBrokerPortfolio[("XPI", "Acoes")] =
        [
            new AssetNodeDTO
            {
                Name = "KLBN4",
                Ticker = "KLBN4",
                Exchange = "BVMF",
                Class = GlobalAssetClass.Equity,
                IsActive = true,
            }
        ];
        var priceService = new StubAssetPriceService();
        var options = Options.Create(new AssetPriceFetchOptions
        {
            Portfolios = [new AssetPriceFetch { BrokerName = "XPI", PortfolioName = "Acoes" }]
        });
        var vm = new AssetPriceFetchViewModel(navigationService, priceService, options, _ => { });

        vm.FetchCommand.Execute(null);
        var request = await priceService.RequestReceived.WaitAsync(TimeSpan.FromSeconds(5));

        request.Ticker.Should().Be("KLBN4");
        request.Exchange.Should().Be("BVMF");
        request.AssetClass.Should().Be(GlobalAssetClass.Equity);
        request.BrokerName.Should().Be("XPI");
    }

    [Fact]
    public async Task FetchAsync_BondAsset_PassesName()
    {
        var navigationService = new StubNavigationService();
        navigationService.AssetsByBrokerPortfolio[("XPI", "Reserva")] =
        [
            new AssetNodeDTO
            {
                Name = "TESOURO IPCA+ 2029",
                Ticker = "TESOURO IPCA+ 2029",
                Exchange = "BVMF",
                Class = GlobalAssetClass.Bond,
                IsActive = true,
            }
        ];
        var priceService = new StubAssetPriceService();
        var options = Options.Create(new AssetPriceFetchOptions
        {
            Portfolios = [new AssetPriceFetch { BrokerName = "XPI", PortfolioName = "Reserva" }]
        });
        var vm = new AssetPriceFetchViewModel(navigationService, priceService, options, _ => { });

        vm.FetchCommand.Execute(null);
        var request = await priceService.RequestReceived.WaitAsync(TimeSpan.FromSeconds(5));

        request.AssetClass.Should().Be(GlobalAssetClass.Bond);
        request.Name.Should().Be("TESOURO IPCA+ 2029");
    }

    private sealed class StubNavigationService : INavigationService
    {
        public Dictionary<(string BrokerName, string PortfolioName), List<AssetNodeDTO>> AssetsByBrokerPortfolio { get; } = new();

        public TreeNodeDTO GetNavigationTree() => throw new NotImplementedException();
        public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName) => throw new NotImplementedException();
        public IEnumerable<BrokerNodeDTO> GetBrokers() => throw new NotImplementedException();

        public IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName) =>
            AssetsByBrokerPortfolio.TryGetValue((brokerName, portfolioName), out var assets) ? assets : [];
    }

    private sealed class StubAssetPriceService : IAssetPriceService
    {
        private readonly TaskCompletionSource<AssetPriceRequestDTO> _tcs = new();

        public Task<AssetPriceRequestDTO> RequestReceived => _tcs.Task;

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
        {
            _tcs.TrySetResult(request);
            return new AssetPriceDTO { Exchange = request.Exchange, Ticker = request.Ticker, Name = "Test", Price = 1m, AsOf = DateTimeOffset.UtcNow };
        }
    }
}
