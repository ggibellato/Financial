using Financial.Domain.Entities;
using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Integrations;

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
}
