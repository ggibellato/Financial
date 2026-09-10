using Financial.Integrations.WebPageParser;
using Xunit.Abstractions;

namespace Financial.WebPageParser.Tests;

/// <summary>
/// Manual verification tests for the redentia.com.br bond-page scraper.
/// These tests make real HTTP requests and should be run manually, on a
/// machine with normal internet access, to confirm the live page structure
/// before this feature is relied on in production.
/// Mark as [Fact] to run, or keep as [Fact(Skip = "Manual")] to skip in CI.
/// </summary>
public class RedentiaVerificationTests
{
    private readonly ITestOutputHelper _output;

    public RedentiaVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_WithKnownBonds()
    {
        _output.WriteLine("Testing redentia.com.br bond-page scraping with live data...");
        _output.WriteLine("");

        var bondTitles = new[]
        {
            "TESOURO SELIC 2031",
            "TESOURO IPCA+ 2029",
            "TESOURO PREFIXADO 2027",
        };

        foreach (var bondTitle in bondTitles)
        {
            _output.WriteLine($"Testing '{bondTitle}'...");

            var snapshot = Redentia.GetSellValue(bondTitle);

            Assert.True(snapshot.Price > 0, $"Price should be positive, got {snapshot.Price}");

            _output.WriteLine($"  ✓ Price (Compra): {snapshot.Price:C}");
            _output.WriteLine($"  ✓ AsOf: {snapshot.AsOf}");
            _output.WriteLine("");
        }
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_UnknownBond_ThrowsInvalidOperationException()
    {
        _output.WriteLine("Testing a bond title that should not resolve to a valid page...");

        Action act = () => Redentia.GetSellValue("TESOURO NAOEXISTE 2099");

        var ex = Record.Exception(act);
        Assert.IsType<InvalidOperationException>(ex);
        _output.WriteLine($"  ✓ Correctly threw InvalidOperationException: {ex!.Message}");
    }
}
