using FluentAssertions;
using Financial.Domain.Entities;

namespace Financial.Domain.Tests;

public class GlobalAssetClassMappingTests
{
    [Theory]
    [InlineData(CountryCode.BR, "FII", GlobalAssetClass.RealEstate)]
    [InlineData(CountryCode.BR, "Acoes", GlobalAssetClass.Equity)]
    [InlineData(CountryCode.US, "REIT", GlobalAssetClass.RealEstate)]
    [InlineData(CountryCode.BR, "TesouroDireto", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.US, "T-Bill", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.UK, "ConventionalGilt", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.US, "ETF", GlobalAssetClass.ETF)]
    [InlineData(CountryCode.UK, "Fund", GlobalAssetClass.Fund)]
    [InlineData(CountryCode.US, "Stock", GlobalAssetClass.Equity)]
    public void Resolve_KnownMappings_ReturnExpected(CountryCode country, string localTypeCode, GlobalAssetClass expected)
    {
        var result = GlobalAssetClassMapping.Resolve(country, localTypeCode);

        result.Should().Be(expected);
    }

    [Fact]
    public void Resolve_UnknownMapping_ReturnsUnknown()
    {
        var result = GlobalAssetClassMapping.Resolve(CountryCode.UK, "UnknownType");

        result.Should().Be(GlobalAssetClass.Unknown);
    }

    [Fact]
    public void Resolve_EmptyLocalType_ReturnsUnknown()
    {
        var result = GlobalAssetClassMapping.Resolve(CountryCode.BR, "  ");

        result.Should().Be(GlobalAssetClass.Unknown);
    }
}
