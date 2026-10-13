using Financial.Integrations.WebPageParser;
using Xunit.Abstractions;

namespace Financial.WebPageParser.Tests;

[Trait("Category", "Live")]
public class TesouroTransparenteVerificationTests
{
    private readonly ITestOutputHelper _output;

    public TesouroTransparenteVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("TESOURO IPCA+ 2040")]
    [InlineData("TESOURO SELIC 2031")]
    [InlineData("TESOURO IPCA+ 2026")]
    public void GetSellValue_BondInThePublishedList_ReturnsAPositivePrice(string bondTitle)
    {
        var quote = TesouroTransparente.GetSellValue(bondTitle);

        _output.WriteLine($"{bondTitle}: {quote.Price} as of {quote.AsOf}");
        Assert.True(quote.Price > 0, $"Price should be positive, got {quote.Price}");
    }

    [Fact]
    public void GetSellValue_BondThatDoesNotExist_ThrowsInvalidOperationException()
    {
        var ex = Record.Exception(() => TesouroTransparente.GetSellValue("TESOURO IPCA+ 1999"));

        Assert.IsType<InvalidOperationException>(ex);
    }
}
