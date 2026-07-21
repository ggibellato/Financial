using Financial.Infrastructure.Integrations.WebPageParser;
using Xunit.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

/// <summary>
/// Manual verification tests for the Status Invest bond-page scraper.
/// These tests make real HTTP requests and should be run manually, on a
/// machine with normal internet access, to confirm the live page structure
/// before this feature is relied on in production.
/// Mark as [Fact] to run, or keep as [Fact(Skip = "Manual")] to skip in CI.
/// </summary>
public class StatusInvestVerificationTests
{
    private readonly ITestOutputHelper _output;

    public StatusInvestVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_WithKnownBonds()
    {
        _output.WriteLine("Testing Status Invest sell-price scraping with live data...");
        _output.WriteLine("");

        var bondTitles = new[]
        {
            "TESOURO SELIC 2029",
            "TESOURO IPCA+ 2029",
            "TESOURO IPCA+ COM JUROS SEMESTRAIS 2035",
        };

        foreach (var bondTitle in bondTitles)
        {
            _output.WriteLine($"Testing '{bondTitle}'...");

            var snapshot = StatusInvest.GetSellValue(bondTitle);

            Assert.True(snapshot.Price > 0, $"Price should be positive, got {snapshot.Price}");

            _output.WriteLine($"  ✓ Slug: {StatusInvest.DeriveSlug(bondTitle)}");
            _output.WriteLine($"  ✓ Price (Valor de Venda): {snapshot.Price:C}");
            _output.WriteLine($"  ✓ AsOf: {snapshot.AsOf}");
            _output.WriteLine("");
        }
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_UnknownBond_ThrowsInvalidOperationException()
    {
        _output.WriteLine("Testing a bond title that should not resolve to a valid page...");

        Action act = () => StatusInvest.GetSellValue("TESOURO NAOEXISTE 2099");

        var ex = Record.Exception(act);
        Assert.IsType<InvalidOperationException>(ex);
        _output.WriteLine($"  ✓ Correctly threw InvalidOperationException: {ex!.Message}");
    }
}
