using Financial.Infrastructure.Integrations.WebPageParser;
using Xunit.Abstractions;

namespace Financial.Infrastructure.Tests.Integrations;

/// <summary>
/// Manual verification tests for the Tesouro Direto redemption-table scraper.
/// These tests make real HTTP requests and should be run manually, on a machine
/// with normal internet access, to confirm the live table structure before this
/// feature is relied on in production.
/// Mark as [Fact] to run, or keep as [Fact(Skip = "Manual")] to skip in CI.
/// </summary>
public class TesouroDiretoVerificationTests
{
    private readonly ITestOutputHelper _output;

    public TesouroDiretoVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_WithKnownBonds()
    {
        _output.WriteLine("Testing Tesouro Direto redemption-table scraping with live data...");
        _output.WriteLine("");

        var bondTitles = new[]
        {
            "TESOURO SELIC 2029",
            "TESOURO IPCA+ 2029",
            "TESOURO PREFIXADO 2027",
        };

        foreach (var bondTitle in bondTitles)
        {
            _output.WriteLine($"Testing '{bondTitle}'...");

            var snapshot = TesouroDireto.GetRedemptionValue(bondTitle);

            if (snapshot is null)
            {
                _output.WriteLine($"  ✗ Not found — either the bond title doesn't match the table, or column resolution failed.");
                continue;
            }

            Assert.True(snapshot.Price > 0, $"Price should be positive, got {snapshot.Price}");

            _output.WriteLine($"  ✓ Name: {snapshot.Name}");
            _output.WriteLine($"  ✓ Price: {snapshot.Price:C}");
            _output.WriteLine($"  ✓ AsOf: {snapshot.AsOf}");
            _output.WriteLine("");
        }
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_UnknownBond_ReturnsNull()
    {
        _output.WriteLine("Testing a bond title that should not exist in the table...");

        var snapshot = TesouroDireto.GetRedemptionValue("NOT A REAL BOND TITLE");

        Assert.Null(snapshot);
        _output.WriteLine("  ✓ Correctly returned null for an unmatched title");
    }
}
