using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

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

    [Fact]
    public void ResolvePortfolioName_Trading212WithIsaInSheetName_ReturnsEtfIsa()
    {
        var resolver = CreateResolver();
        var sheet = new SheetDTO { Id = 0, Name = "VUSA ISA", Color = "" };

        var portfolioName = resolver.ResolvePortfolioName("Trading 212", sheet);

        portfolioName.Should().Be("ETF ISA");
    }

    [Fact]
    public void ResolvePortfolioName_Trading212WithSippInSheetName_ReturnsEtfSipp()
    {
        var resolver = CreateResolver();
        var sheet = new SheetDTO { Id = 0, Name = "VUSA SIPP", Color = "" };

        var portfolioName = resolver.ResolvePortfolioName("Trading 212", sheet);

        portfolioName.Should().Be("ETF SIPP");
    }

    [Fact]
    public void ResolvePortfolioName_Trading212WithoutIsaOrSippInSheetName_ReturnsEtf()
    {
        var resolver = CreateResolver();
        var sheet = new SheetDTO { Id = 0, Name = "VUSA", Color = "" };

        var portfolioName = resolver.ResolvePortfolioName("Trading 212", sheet);

        portfolioName.Should().Be("ETF");
    }

    [Fact]
    public void IsClosedPortfolio_Encerradas_ReturnsTrue()
    {
        var resolver = CreateResolver();

        resolver.IsClosedPortfolio("Encerradas").Should().BeTrue();
    }

    [Fact]
    public void IsClosedPortfolio_OtherPortfolioName_ReturnsFalse()
    {
        var resolver = CreateResolver();

        resolver.IsClosedPortfolio("Acoes").Should().BeFalse();
    }

    [Fact]
    public void ResolveHistoricPortfolioName_DelegatesToAssetClassificationLookup()
    {
        var resolver = CreateResolver();

        var portfolioName = resolver.ResolveHistoricPortfolioName("Bitcoin");

        portfolioName.Should().Be(AssetClassificationLookup.UncategorizedHistoricPortfolioName);
    }
}
