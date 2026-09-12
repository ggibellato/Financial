using System.Globalization;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class PriceSourceToLabelConverterTests
{
    private readonly PriceSourceToLabelConverter _converter = new();

    [Theory]
    [InlineData(PriceSource.Unknown, "Unknown")]
    [InlineData(PriceSource.Manual, "Manual")]
    [InlineData(PriceSource.ProviderValuation, "Provider Valuation")]
    [InlineData(PriceSource.Google, "Google")]
    [InlineData(PriceSource.Yahoo, "Yahoo")]
    [InlineData(PriceSource.StatusInvest, "StatusInvest")]
    [InlineData(PriceSource.DicionarioDoInvestidor, "Dicionario do Investidor")]
    [InlineData(PriceSource.Redentia, "Redentia")]
    public void Convert_EachPriceSource_ReturnsTheSameLabelFinancialWebUses(PriceSource source, string expectedLabel)
    {
        var result = _converter.Convert(source, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(expectedLabel);
    }

    [Fact]
    public void Convert_NonPriceSourceValue_ReturnsEmptyString()
    {
        var result = _converter.Convert("not a PriceSource", typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotSupportedException()
    {
        var act = () => _converter.ConvertBack("Manual", typeof(PriceSource), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
