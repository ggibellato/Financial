using Financial.Integrations.WebPageParser;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class WebPageParserMappersTests
{
    [Fact]
    public void ToAssetValueSnapshot_MapsAllFields()
    {
        var asOf = DateTimeOffset.UtcNow;
        var quote = new WebAssetQuote("BCIA11", "Some ETF", 10.5m, asOf);

        var snapshot = WebPageParserMappers.ToAssetValueSnapshot(quote);

        snapshot.Should().Be(new AssetValueSnapshot("BCIA11", "Some ETF", 10.5m, asOf));
    }

    [Fact]
    public void ToDividendValues_MapsDividendType()
    {
        var records = new[] { new WebDividendRecord(WebDividendType.Dividend, new DateTime(2024, 1, 1), 5m) };

        var result = WebPageParserMappers.ToDividendValues(records);

        result.Should().ContainSingle().Which.Should().Be(new DividendValue(DividendType.Dividend, new DateTime(2024, 1, 1), 5m));
    }

    [Fact]
    public void ToDividendValues_MapsJcpType()
    {
        var records = new[] { new WebDividendRecord(WebDividendType.JCP, new DateTime(2024, 2, 1), 3m) };

        var result = WebPageParserMappers.ToDividendValues(records);

        result.Should().ContainSingle().Which.Type.Should().Be(DividendType.JCP);
    }

    [Fact]
    public void ToDividendValues_PreservesOrder()
    {
        var records = new[]
        {
            new WebDividendRecord(WebDividendType.Dividend, new DateTime(2024, 1, 1), 5m),
            new WebDividendRecord(WebDividendType.JCP, new DateTime(2024, 2, 1), 3m),
        };

        var result = WebPageParserMappers.ToDividendValues(records);

        result.Select(r => r.Value).Should().ContainInOrder(5m, 3m);
    }
}
