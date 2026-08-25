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

    /// <summary>
    /// A price read from Price History used to arrive with no timestamp at all, so "As of"
    /// rendered an em dash and there was no way to tell how stale a manual value was.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_PriceReadFromHistory_ShowsTheStoredEntryDate()
    {
        var asOfDate = new DateOnly(2026, 8, 16);
        var applied = new List<TodayInfoSnapshot>();
        var tracker = new TodayInfoTracker(applied.Add, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Acoes|TASA4");

        await tracker.RefreshAsync(
            forceRefresh: true, hasAssetContext: true, new StoredPriceService(asOfDate),
            GlobalAssetClass.Equity, "XPI", "BVMF", "TASA4", "TASA4", "Acoes", "TASA4", _ => { });

        applied.Should().ContainSingle();
        applied[0].AsOf.Should().Be(asOfDate.ToString("d"));
        applied[0].IsManual.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_PriceReadFromHistory_ShowsNoTimeOfDay()
    {
        var applied = new List<TodayInfoSnapshot>();
        var tracker = new TodayInfoTracker(applied.Add, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Acoes|TASA4");

        await tracker.RefreshAsync(
            forceRefresh: true, hasAssetContext: true, new StoredPriceService(new DateOnly(2026, 8, 16)),
            GlobalAssetClass.Equity, "XPI", "BVMF", "TASA4", "TASA4", "Acoes", "TASA4", _ => { });

        applied[0].AsOf.Should().NotContain(":", "a stored entry carries no time of day");
    }

    /// <summary>
    /// A price with neither a live timestamp nor a stored entry date used to render "As of" as a blank
    /// string in WPF while the web showed an em dash for the equivalent case (AssetSummaryTab.tsx).
    /// </summary>
    [Fact]
    public async Task RefreshAsync_PriceWithNoDate_ShowsAnEmDash()
    {
        var applied = new List<TodayInfoSnapshot>();
        var tracker = new TodayInfoTracker(applied.Add, () => { }, () => { });
        tracker.UpdateAssetKey("XPI|Acoes|TASA4");

        await tracker.RefreshAsync(
            forceRefresh: true, hasAssetContext: true, new NoDatePriceService(),
            GlobalAssetClass.Equity, "XPI", "BVMF", "TASA4", "TASA4", "Acoes", "TASA4", _ => { });

        applied.Should().ContainSingle();
        applied[0].AsOf.Should().Be("—");
    }

    private sealed class NoDatePriceService : IAssetPriceLookupService
    {
        public Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request) =>
            Task.FromResult(new AssetPriceDTO
            {
                Exchange = request.Exchange,
                Ticker = request.Ticker,
                Price = 1.23m,
                AsOf = null,
                AsOfDate = null,
                IsManual = true
            });
    }

    private sealed class StoredPriceService : IAssetPriceLookupService
    {
        private readonly DateOnly _asOfDate;

        public StoredPriceService(DateOnly asOfDate)
        {
            _asOfDate = asOfDate;
        }

        public Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request) =>
            Task.FromResult(new AssetPriceDTO
            {
                Exchange = request.Exchange,
                Ticker = request.Ticker,
                Price = 4.91m,
                AsOf = null,
                AsOfDate = _asOfDate,
                IsManual = true
            });
    }

    private sealed class StubPriceService : IAssetPriceLookupService
    {
        private readonly bool _isManual;

        public StubPriceService(bool isManual = false)
        {
            _isManual = isManual;
        }

        public AssetPriceRequestDTO? LastRequest { get; private set; }

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
