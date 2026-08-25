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
}
