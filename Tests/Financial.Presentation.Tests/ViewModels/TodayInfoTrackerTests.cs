using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TodayInfoTrackerTests
{
    [Fact]
    public async Task RefreshAsync_BondWithName_BuildsRequestWithNameAndAppliesSnapshot()
    {
        var priceService = new StubPriceService();
        TodayInfoSnapshot? applied = null;
        var tracker = new TodayInfoTracker(snapshot => applied = snapshot, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Reserva|TESOURO IPCA+ 2029");
        var messages = new List<string>();

        await tracker.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: priceService,
            assetClass: GlobalAssetClass.Bond,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "TESOURO IPCA+ 2029",
            name: "TESOURO IPCA+ 2029",
            portfolioName: "Reserva",
            assetName: "TESOURO IPCA+ 2029",
            setMessage: messages.Add);

        priceService.LastRequest.Should().NotBeNull();
        priceService.LastRequest!.Name.Should().Be("TESOURO IPCA+ 2029");
        priceService.LastRequest.PortfolioName.Should().Be("Reserva");
        priceService.LastRequest.AssetName.Should().Be("TESOURO IPCA+ 2029");
        applied.Should().NotBeNull();
        applied!.Price.Should().Be(3775.97m);
        applied.IsManual.Should().BeFalse();
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_LiveFetchFallsBackToManualEntry_AppliesManualSnapshot()
    {
        var priceService = new StubPriceService(isManual: true);
        TodayInfoSnapshot? applied = null;
        var tracker = new TodayInfoTracker(snapshot => applied = snapshot, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Reserva|GUEP11");
        var messages = new List<string>();

        await tracker.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: priceService,
            assetClass: GlobalAssetClass.RealEstate,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "GUEP11",
            name: "GUEP11",
            portfolioName: "Reserva",
            assetName: "GUEP11",
            setMessage: messages.Add);

        applied.Should().NotBeNull();
        applied!.IsManual.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_BondWithoutName_SetsValidationMessageAndDoesNotFetch()
    {
        var priceService = new StubPriceService();
        var tracker = new TodayInfoTracker(_ => { }, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Reserva|TESOURO IPCA+ 2029");
        var messages = new List<string>();

        await tracker.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: priceService,
            assetClass: GlobalAssetClass.Bond,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "TESOURO IPCA+ 2029",
            name: null,
            portfolioName: "Reserva",
            assetName: null,
            setMessage: messages.Add);

        priceService.LastRequest.Should().BeNull();
        messages.Should().ContainSingle().Which.Should().Be("Asset name is missing.");
    }

    [Fact]
    public async Task RefreshAsync_EquityWithoutExchange_SetsValidationMessageAndDoesNotFetch()
    {
        var priceService = new StubPriceService();
        var tracker = new TodayInfoTracker(_ => { }, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Acoes|KLBN4");
        var messages = new List<string>();

        await tracker.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            priceService: priceService,
            assetClass: GlobalAssetClass.Equity,
            brokerName: "XPI",
            exchange: "",
            ticker: "KLBN4",
            name: null,
            portfolioName: "Acoes",
            assetName: "KLBN4",
            setMessage: messages.Add);

        priceService.LastRequest.Should().BeNull();
        messages.Should().ContainSingle().Which.Should().Be("Asset exchange or ticker is missing.");
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
