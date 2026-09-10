using Financial.Integrations.WebPageParser;
using Xunit.Abstractions;

namespace Financial.WebPageParser.Tests;

/// <summary>
/// Manual verification tests for the dicionariodoinvestidor.com bond-list scraper.
/// These tests make real HTTP requests and should be run manually, on a
/// machine with normal internet access, to confirm the live page structure
/// before this feature is relied on in production.
/// Mark as [Fact] to run, or keep as [Fact(Skip = "Manual")] to skip in CI.
/// </summary>
public class DicionarioDoInvestidorVerificationTests
{
    private readonly ITestOutputHelper _output;

    public DicionarioDoInvestidorVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_WithCurrentlyOfferedBonds()
    {
        _output.WriteLine("Testing dicionariodoinvestidor.com bond-list scraping with live data...");
        _output.WriteLine("");

        var bondTitles = new[]
        {
            "TESOURO IPCA+ 2040",
            "TESOURO SELIC 2031",
        };

        foreach (var bondTitle in bondTitles)
        {
            _output.WriteLine($"Testing '{bondTitle}'...");

            var snapshot = DicionarioDoInvestidor.GetSellValue(bondTitle);

            Assert.True(snapshot.Price > 0, $"Price should be positive, got {snapshot.Price}");

            _output.WriteLine($"  ✓ Price (Valor Atual): {snapshot.Price:C}");
            _output.WriteLine($"  ✓ AsOf: {snapshot.AsOf}");
            _output.WriteLine("");
        }
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_BondNotCurrentlyOffered_ThrowsInvalidOperationException()
    {
        _output.WriteLine("Testing a matured/no-longer-offered bond title, which this site does not list...");

        Action act = () => DicionarioDoInvestidor.GetSellValue("TESOURO IPCA+ 2029");

        var ex = Record.Exception(act);
        Assert.IsType<InvalidOperationException>(ex);
        _output.WriteLine($"  ✓ Correctly threw InvalidOperationException: {ex!.Message}");
    }
}
