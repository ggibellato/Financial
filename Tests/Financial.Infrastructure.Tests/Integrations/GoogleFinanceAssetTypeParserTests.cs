using Financial.Infrastructure.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Integrations;

public class GoogleFinanceAssetTypeParserTests
{
    [Theory]
    [InlineData("{\"quoteType\":\"REIT\"}", "REIT")]
    [InlineData("{\"instrumentType\":\"ETF\"}", "ETF")]
    [InlineData("{\"assetClass\":\"MUTUAL_FUND\"}", "Fund")]
    [InlineData("{\"quoteType\":\"BOND\"}", "Bond")]
    [InlineData("{\"quoteType\":\"EQUITY\"}", "Stock")]
    public void TryParseLocalTypeCode_WithToken_ReturnsMappedCode(string html, string expected)
    {
        var result = GoogleFinanceAssetTypeParser.TryParseLocalTypeCode(html);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Real Estate Investment Trust", "REIT")]
    [InlineData("Mutual Fund", "Fund")]
    [InlineData("Government Bond", "Bond")]
    [InlineData("Conventional Gilt", "ConventionalGilt")]
    public void TryParseLocalTypeCode_WithKeywords_ReturnsMappedCode(string html, string expected)
    {
        var result = GoogleFinanceAssetTypeParser.TryParseLocalTypeCode(html);

        result.Should().Be(expected);
    }

    [Fact]
    public void TryParseLocalTypeCode_WithNoMatch_ReturnsEmpty()
    {
        var result = GoogleFinanceAssetTypeParser.TryParseLocalTypeCode("No asset type info here");

        result.Should().BeEmpty();
    }
}
