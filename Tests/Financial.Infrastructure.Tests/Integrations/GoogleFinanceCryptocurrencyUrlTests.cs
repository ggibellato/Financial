using Financial.Infrastructure.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Integrations;

public class GoogleFinanceCryptocurrencyUrlTests
{
    [Fact]
    public void BuildCryptocurrencyQuoteUrl_BitcoinGBP_ReturnsBetaQuoteUrl()
    {
        var url = GoogleFinance.BuildCryptocurrencyQuoteUrl("GBP", "BTC");

        url.Should().Be("https://www.google.com/finance/beta/quote/BTC-GBP");
    }

    [Fact]
    public void BuildStockQuoteUrl_UnchangedFormat_ReturnsStandardQuoteUrl()
    {
        var url = GoogleFinance.BuildStockQuoteUrl("BVMF", "BCIA11");

        url.Should().Be("https://www.google.com/finance/quote/BCIA11:BVMF");
    }
}
