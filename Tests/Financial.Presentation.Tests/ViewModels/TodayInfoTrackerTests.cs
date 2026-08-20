using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TodayInfoTrackerTests
{
    private readonly StubPriceService _priceService;
    private readonly List<string> _messages;
    private readonly TodayInfoTracker _sut;
    private TodayInfoSnapshot? _applied;

    public TodayInfoTrackerTests()
    {
        _priceService = new StubPriceService();
        _messages = [];
        _sut = new TodayInfoTracker(snapshot => _applied = snapshot, () => { }, () => { });
    }

    [Fact]
    public async Task RefreshAsync_BondWithName_BuildsRequestWithNameAndAppliesSnapshot()
    {
        _sut.UpdateAssetKey("XPI|Reserva|TESOURO IPCA+ 2029");

        await _sut.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: _priceService,
            assetClass: GlobalAssetClass.Bond,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "TESOURO IPCA+ 2029",
            name: "TESOURO IPCA+ 2029",
            portfolioName: "Reserva",
            assetName: "TESOURO IPCA+ 2029",
            setMessage: _messages.Add);

        _priceService.LastRequest.Should().NotBeNull();
        _priceService.LastRequest!.Name.Should().Be("TESOURO IPCA+ 2029");
        _priceService.LastRequest.PortfolioName.Should().Be("Reserva");
        _priceService.LastRequest.AssetName.Should().Be("TESOURO IPCA+ 2029");
        _applied.Should().NotBeNull();
        _applied!.Price.Should().Be(3775.97m);
        _applied.IsManual.Should().BeFalse();
        _messages.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_LiveFetchFallsBackToManualEntry_AppliesManualSnapshot()
    {
        var manualPriceService = new StubPriceService(isManual: true);
        _sut.UpdateAssetKey("XPI|Reserva|GUEP11");

        await _sut.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: manualPriceService,
            assetClass: GlobalAssetClass.RealEstate,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "GUEP11",
            name: "GUEP11",
            portfolioName: "Reserva",
            assetName: "GUEP11",
            setMessage: _messages.Add);

        _applied.Should().NotBeNull();
        _applied!.IsManual.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_BondWithoutName_SetsValidationMessageAndDoesNotFetch()
    {
        _sut.UpdateAssetKey("XPI|Reserva|TESOURO IPCA+ 2029");

        await _sut.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: _priceService,
            assetClass: GlobalAssetClass.Bond,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "TESOURO IPCA+ 2029",
            name: null,
            portfolioName: "Reserva",
            assetName: null,
            setMessage: _messages.Add);

        _priceService.LastRequest.Should().BeNull();
        _messages.Should().ContainSingle().Which.Should().Be("Asset name is missing.");
    }

    [Fact]
    public async Task RefreshAsync_EquityWithoutExchange_SetsValidationMessageAndDoesNotFetch()
    {
        _sut.UpdateAssetKey("XPI|Acoes|KLBN4");

        await _sut.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: _priceService,
            assetClass: GlobalAssetClass.Equity,
            brokerName: "XPI",
            exchange: "",
            ticker: "KLBN4",
            name: null,
            portfolioName: "Acoes",
            assetName: "KLBN4",
            setMessage: _messages.Add);

        _priceService.LastRequest.Should().BeNull();
        _messages.Should().ContainSingle().Which.Should().Be("Asset exchange or ticker is missing.");
    }

    private sealed class StubPriceService : IPriceService
    {
        private readonly bool _isManual;

        public StubPriceService(bool isManual = false)
        {
            _isManual = isManual;
        }

        public AssetPriceRequestDTO? LastRequest { get; private set; }

        public Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request) => throw new NotImplementedException();
        public Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request) => throw new NotImplementedException();

        public Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request)
        {
            LastRequest = request;
            return Task.FromResult(new AssetPriceDTO
            {
                Exchange = request.Exchange,
                Ticker = request.Ticker,
                Name = request.Name ?? request.Ticker,
                Price = 3775.97m,
                AsOf = DateTimeOffset.UtcNow,
                IsManual = _isManual
            });
        }
    }
}
