using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TodayInfoTrackerTests
{
    [Fact]
    public async Task RefreshAsync_BondWithName_BuildsRequestWithNameAndAppliesSnapshot()
    {
        var priceService = new StubAssetPriceService();
        TodayInfoSnapshot? applied = null;
        var tracker = new TodayInfoTracker(snapshot => applied = snapshot, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Reserva|TESOURO IPCA+ 2029");
        var messages = new List<string>();

        await tracker.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            assetPriceService: priceService,
            assetClass: GlobalAssetClass.Bond,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "TESOURO IPCA+ 2029",
            name: "TESOURO IPCA+ 2029",
            setMessage: messages.Add);

        priceService.LastRequest.Should().NotBeNull();
        priceService.LastRequest!.Name.Should().Be("TESOURO IPCA+ 2029");
        applied.Should().NotBeNull();
        applied!.Price.Should().Be(3775.97m);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_BondWithoutName_SetsValidationMessageAndDoesNotFetch()
    {
        var priceService = new StubAssetPriceService();
        var tracker = new TodayInfoTracker(_ => { }, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Reserva|TESOURO IPCA+ 2029");
        var messages = new List<string>();

        await tracker.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            assetPriceService: priceService,
            assetClass: GlobalAssetClass.Bond,
            brokerName: "XPI",
            exchange: "BVMF",
            ticker: "TESOURO IPCA+ 2029",
            name: null,
            setMessage: messages.Add);

        priceService.LastRequest.Should().BeNull();
        messages.Should().ContainSingle().Which.Should().Be("Asset name is missing.");
    }

    [Fact]
    public async Task RefreshAsync_EquityWithoutExchange_SetsValidationMessageAndDoesNotFetch()
    {
        var priceService = new StubAssetPriceService();
        var tracker = new TodayInfoTracker(_ => { }, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Acoes|KLBN4");
        var messages = new List<string>();

        await tracker.RefreshAsync(
            forceRefresh: true,
            hasAssetContext: true,
            assetPriceService: priceService,
            assetClass: GlobalAssetClass.Equity,
            brokerName: "XPI",
            exchange: "",
            ticker: "KLBN4",
            name: null,
            setMessage: messages.Add);

        priceService.LastRequest.Should().BeNull();
        messages.Should().ContainSingle().Which.Should().Be("Asset exchange or ticker is missing.");
    }

    private sealed class StubAssetPriceService : IAssetPriceService
    {
        public AssetPriceRequestDTO? LastRequest { get; private set; }

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
        {
            LastRequest = request;
            return new AssetPriceDTO
            {
                Exchange = request.Exchange,
                Ticker = request.Ticker,
                Name = request.Name ?? request.Ticker,
                Price = 3775.97m,
                AsOf = DateTimeOffset.UtcNow
            };
        }
    }
}
