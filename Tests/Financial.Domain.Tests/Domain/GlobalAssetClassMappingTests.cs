using Financial.Domain.Entities;
using Financial.Domain.Rules;
using FluentAssertions;

namespace Financial.Domain.Tests;

public class GlobalAssetClassMappingTests
{
    [Theory]
    [InlineData(CountryCode.BR, "FII", GlobalAssetClass.RealEstate)]
    [InlineData(CountryCode.BR, "Acoes", GlobalAssetClass.Equity)]
    [InlineData(CountryCode.BR, "ETF", GlobalAssetClass.ETF)]
    [InlineData(CountryCode.BR, "Fund", GlobalAssetClass.Fund)]
    [InlineData(CountryCode.BR, "Fundo", GlobalAssetClass.Fund)]
    [InlineData(CountryCode.BR, "Bond", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.BR, "TesouroDireto", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.BR, "CreditoImobiliario", GlobalAssetClass.PrivateCredit)]
    [InlineData(CountryCode.BR, "Pensao", GlobalAssetClass.Pension)]
    [InlineData(CountryCode.US, "REIT", GlobalAssetClass.RealEstate)]
    [InlineData(CountryCode.US, "Stock", GlobalAssetClass.Equity)]
    [InlineData(CountryCode.US, "ETF", GlobalAssetClass.ETF)]
    [InlineData(CountryCode.US, "Fund", GlobalAssetClass.Fund)]
    [InlineData(CountryCode.US, "Bond", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.US, "T-Bill", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.US, "Cash", GlobalAssetClass.Cash)]
    [InlineData(CountryCode.US, "Pension", GlobalAssetClass.Pension)]
    [InlineData(CountryCode.UK, "REIT", GlobalAssetClass.RealEstate)]
    [InlineData(CountryCode.UK, "Stock", GlobalAssetClass.Equity)]
    [InlineData(CountryCode.UK, "ETF", GlobalAssetClass.ETF)]
    [InlineData(CountryCode.UK, "Fund", GlobalAssetClass.Fund)]
    [InlineData(CountryCode.UK, "Bond", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.UK, "ConventionalGilt", GlobalAssetClass.Bond)]
    [InlineData(CountryCode.UK, "Cash", GlobalAssetClass.Cash)]
    [InlineData(CountryCode.UK, "Pension", GlobalAssetClass.Pension)]
    public void Resolve_KnownMappings_ReturnExpected(CountryCode country, string localTypeCode, GlobalAssetClass expected)
    {
        var result = GlobalAssetClassMapping.Resolve(country, localTypeCode);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(CountryCode.BR, "fii", GlobalAssetClass.RealEstate)]
    [InlineData(CountryCode.BR, "ACOES", GlobalAssetClass.Equity)]
    [InlineData(CountryCode.UK, "conventionalgilt", GlobalAssetClass.Bond)]
    public void Resolve_CaseInsensitive_ReturnsMappedClass(CountryCode country, string localTypeCode, GlobalAssetClass expected)
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
    public void Resolve_LocalTypeCodeMappedForDifferentCountry_ReturnsUnknown()
    {
        // "Stock" is a valid key for US/UK but not BR - exercises the comparer's
        // country-mismatch short-circuit against otherwise-matching local type codes.
        var result = GlobalAssetClassMapping.Resolve(CountryCode.BR, "Stock");

        result.Should().Be(GlobalAssetClass.Unknown);
    }

    [Fact]
    public void Resolve_EmptyLocalType_ReturnsUnknown()
    {
        var result = GlobalAssetClassMapping.Resolve(CountryCode.BR, "  ");

        result.Should().Be(GlobalAssetClass.Unknown);
    }
}
