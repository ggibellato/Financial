using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

public class AssetClassificationLookupTests
{
    [Fact]
    public void TryGet_Bitcoin_ReturnsCryptocurrencyClass()
    {
        var found = AssetClassificationLookup.TryGet("Bitcoin", out var entry);

        found.Should().BeTrue();
        entry.Class.Should().Be(GlobalAssetClass.Cryptocurrency);
        entry.Country.Should().Be(CountryCode.UK);
    }

    [Fact]
    public void TryGet_Bitcoin_HistoricPortfolioIsNull()
    {
        AssetClassificationLookup.TryGet("Bitcoin", out var entry);

        entry.HistoricPortfolio.Should().BeNull();
    }

    [Fact]
    public void TryGet_BitcoinCaseInsensitiveAndTrimmed_ReturnsCryptocurrencyClass()
    {
        var found = AssetClassificationLookup.TryGet(" bitcoin ", out var entry);

        found.Should().BeTrue();
        entry.Class.Should().Be(GlobalAssetClass.Cryptocurrency);
    }

    [Fact]
    public void TryGet_UnknownAssetName_ReturnsFalse()
    {
        var found = AssetClassificationLookup.TryGet("NotARealAsset", out var entry);

        found.Should().BeFalse();
        entry.Should().Be(default(AssetClassificationEntry));
    }

    [Fact]
    public void ResolveHistoricPortfolio_EntryWithHistoricPortfolio_ReturnsClassifiedValue()
    {
        var entry = new AssetClassificationEntry(CountryCode.UK, "REIT", GlobalAssetClass.RealEstate, "Dividend Portfolio");

        var result = AssetClassificationLookup.ResolveHistoricPortfolio(entry);

        result.Should().Be("Dividend Portfolio");
    }

    [Fact]
    public void ResolveHistoricPortfolio_EntryWithoutHistoricPortfolio_ReturnsUncategorized()
    {
        var entry = new AssetClassificationEntry(CountryCode.UK, "REIT", GlobalAssetClass.RealEstate);

        var result = AssetClassificationLookup.ResolveHistoricPortfolio(entry);

        result.Should().Be(AssetClassificationLookup.UncategorizedHistoricPortfolioName);
    }

    [Fact]
    public void ResolveHistoricPortfolio_KnownAssetWithoutHistoricPortfolio_ReturnsUncategorized()
    {
        var result = AssetClassificationLookup.ResolveHistoricPortfolio("Bitcoin");

        result.Should().Be(AssetClassificationLookup.UncategorizedHistoricPortfolioName);
    }

    [Fact]
    public void ResolveHistoricPortfolio_UnknownAssetName_ReturnsUncategorized()
    {
        var result = AssetClassificationLookup.ResolveHistoricPortfolio("NotARealAsset");

        result.Should().Be(AssetClassificationLookup.UncategorizedHistoricPortfolioName);
    }
}
