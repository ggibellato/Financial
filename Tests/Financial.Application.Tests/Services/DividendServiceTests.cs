using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using Financial.Domain.Rules;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Application.Tests.Services;

public class DividendServiceTests
{
    private readonly StubDividendDataSource _dataSource = new();
    private readonly StubSnapshotSource _snapshotSource = new();

    [Fact]
    public void Constructor_WithNullDividendDataSource_Throws()
    {
        Action act = () => new DividendService(null!, _snapshotSource);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dividendDataSource");
    }

    [Fact]
    public void Constructor_WithNullSnapshotSource_Throws()
    {
        Action act = () => new DividendService(_dataSource, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snapshotSource");
    }

    [Fact]
    public void GetDividendHistory_WithNullRequest_Throws()
    {
        var service = CreateService();

        Action act = () => service.GetDividendHistory(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("", "TICK")]
    [InlineData("   ", "TICK")]
    [InlineData("NYSE", "")]
    [InlineData("NYSE", "  ")]
    public void GetDividendHistory_WithMissingExchangeOrTicker_Throws(string exchange, string ticker)
    {
        var service = CreateService();

        Action act = () => service.GetDividendHistory(new DividendLookupRequestDTO
        {
            Exchange = exchange,
            Ticker = ticker
        });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetDividendHistory_WithNoDividends_ReturnsEmptyList()
    {
        _dataSource.Dividends = [];
        var service = CreateService();

        var result = service.GetDividendHistory(MakeRequest());

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDividendHistory_OrdersByDateDescending()
    {
        _dataSource.Dividends =
        [
            new DividendValue(DividendType.Dividend, new DateTime(2020, 1, 1), 2.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2022, 1, 1), 4.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2021, 1, 1), 3.0m),
        ];
        var service = CreateService();

        var result = service.GetDividendHistory(MakeRequest());

        result[0].Date.Should().Be(new DateTime(2022, 1, 1));
        result[1].Date.Should().Be(new DateTime(2021, 1, 1));
        result[2].Date.Should().Be(new DateTime(2020, 1, 1));
    }

    [Fact]
    public void GetDividendHistory_MapsTypeAndValue()
    {
        _dataSource.Dividends =
        [
            new DividendValue(DividendType.JCP, new DateTime(2021, 6, 1), 5.5m),
        ];
        var service = CreateService();

        var item = service.GetDividendHistory(MakeRequest()).Should().ContainSingle().Which;

        using (new AssertionScope())
        {
            item.Type.Should().Be("JCP");
            item.Value.Should().Be(5.5m);
            item.Date.Should().Be(new DateTime(2021, 6, 1));
        }
    }

    [Fact]
    public void GetDividendSummary_MapsSnapshotFields()
    {
        _dataSource.Dividends = [];
        _snapshotSource.Snapshot = new AssetValueSnapshot("TICK", "Asset Name", 50m, DateTimeOffset.UtcNow);
        var service = CreateService();

        var result = service.GetDividendSummary(MakeRequest("NYSE", "TICK"));

        using (new AssertionScope())
        {
            result.Exchange.Should().Be("NYSE");
            result.Ticker.Should().Be("TICK");
            result.Name.Should().Be("Asset Name");
            result.CurrentPrice.Should().Be(50m);
        }
    }

    [Fact]
    public void GetDividendSummary_GroupsDividendsByYear()
    {
        _dataSource.Dividends =
        [
            new DividendValue(DividendType.Dividend, new DateTime(2020, 1, 1), 2.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2020, 6, 1), 3.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2019, 1, 1), 4.0m),
        ];
        var service = CreateService();

        var result = service.GetDividendSummary(MakeRequest());

        result.YearTotals.Should().HaveCount(2);
        result.YearTotals.First(t => t.Year == 2020).Total.Should().Be(5.0m);
        result.YearTotals.First(t => t.Year == 2019).Total.Should().Be(4.0m);
    }

    [Fact]
    public void GetDividendSummary_CalculatesPriceMaxAndDiscount()
    {
        // One past year with total 6.0m → priceMax = 6.0 / 0.06 = 100m, price 80m → discount 20%
        _dataSource.Dividends =
        [
            new DividendValue(DividendType.Dividend, new DateTime(2020, 1, 1), 6.0m),
        ];
        _snapshotSource.Snapshot = new AssetValueSnapshot("TICK", "Name", 80m, DateTimeOffset.UtcNow);
        var service = CreateService();

        var result = service.GetDividendSummary(MakeRequest());

        using (new AssertionScope())
        {
            result.AverageDividendLastFiveYears.Should().Be(6.0m);
            result.PriceMaxBuy.Should().Be(6.0m / DividendValuationRules.RequiredYield);
            result.DiscountPercent.Should().Be(20.0m);
        }
    }

    [Fact]
    public void GetDividendSummary_ExcludesCurrentYearFromAverage()
    {
        // Only current-year dividend — should be excluded → average and priceMax stay zero
        _dataSource.Dividends =
        [
            new DividendValue(DividendType.Dividend, new DateTime(DateTime.Today.Year, 1, 1), 12.0m),
        ];
        var service = CreateService();

        var result = service.GetDividendSummary(MakeRequest());

        using (new AssertionScope())
        {
            result.AverageDividendLastFiveYears.Should().Be(0m);
            result.PriceMaxBuy.Should().Be(0m);
        }
    }

    [Fact]
    public void GetDividendSummary_LimitsAverageToLastFiveYears()
    {
        // 7 past years; latest 5 average to 5.0m, all 7 would average to ~3.86m
        _dataSource.Dividends =
        [
            new DividendValue(DividendType.Dividend, new DateTime(2014, 1, 1), 1.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2015, 1, 1), 1.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2016, 1, 1), 1.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2017, 1, 1), 6.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2018, 1, 1), 6.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2019, 1, 1), 6.0m),
            new DividendValue(DividendType.Dividend, new DateTime(2020, 1, 1), 6.0m),
        ];
        var service = CreateService();

        var result = service.GetDividendSummary(MakeRequest());

        // Latest 5 years (2020:6, 2019:6, 2018:6, 2017:6, 2016:1) → average = 25/5 = 5.0m
        result.AverageDividendLastFiveYears.Should().Be(5.0m);
    }

    [Fact]
    public void GetDividendSummary_WithNoDividends_ReturnsZeroCalculations()
    {
        _dataSource.Dividends = [];
        var service = CreateService();

        var result = service.GetDividendSummary(MakeRequest());

        using (new AssertionScope())
        {
            result.AverageDividendLastFiveYears.Should().Be(0m);
            result.PriceMaxBuy.Should().Be(0m);
            result.DiscountPercent.Should().Be(0m);
            result.YearTotals.Should().BeEmpty();
            result.History.Should().BeEmpty();
        }
    }

    private DividendService CreateService() => new(_dataSource, _snapshotSource);

    private static DividendLookupRequestDTO MakeRequest(string exchange = "NYSE", string ticker = "TICK") =>
        new() { Exchange = exchange, Ticker = ticker };

    private sealed class StubDividendDataSource : IDividendDataSource
    {
        public IReadOnlyList<DividendValue> Dividends { get; set; } = [];

        public IReadOnlyList<DividendValue> GetDividends(string exchange, string ticker) => Dividends;
    }

    private sealed class StubSnapshotSource : IAssetSnapshotSource
    {
        public AssetValueSnapshot Snapshot { get; set; } =
            new AssetValueSnapshot("DEFAULT", "Default Asset", 0m, DateTimeOffset.UtcNow);

        public AssetValueSnapshot GetSnapshot(string exchange, string ticker) => Snapshot;
    }
}
