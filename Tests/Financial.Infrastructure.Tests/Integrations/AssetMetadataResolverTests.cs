using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Integrations;

public class AssetMetadataResolverTests
{
    private static AssetMetadataResolver CreateResolver()
    {
        var options = new GoogleGeneratorOptions(
            IgnoreSheetNames: [],
            PortfolioNameMap: new Dictionary<string, string>(),
            BrokerCurrencyMap: new Dictionary<string, string>
            {
                ["Coinbase"] = "GBP"
            });

        return new AssetMetadataResolver(options, sheetsReader: null!);
    }

    [Fact]
    public void ResolveBrokerCurrency_Coinbase_ReturnsGBP()
    {
        var resolver = CreateResolver();

        var currency = resolver.ResolveBrokerCurrency("Coinbase");

        currency.Should().Be("GBP");
    }

    [Fact]
    public void ResolveBrokerCurrency_UnmappedBroker_ThrowsWithBrokerName()
    {
        var resolver = CreateResolver();

        Action act = () => resolver.ResolveBrokerCurrency("NotABroker");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NotABroker*");
    }

    [Fact]
    public void ResolvePortfolioName_CoinbaseWithBlankColor_ReturnsCryptocurrency()
    {
        var resolver = CreateResolver();
        var sheet = new SheetDTO { Id = 0, Name = "Bitcoin", Color = "" };

        var portfolioName = resolver.ResolvePortfolioName("Coinbase", sheet);

        portfolioName.Should().Be("Cryptocurrency");
    }
}
