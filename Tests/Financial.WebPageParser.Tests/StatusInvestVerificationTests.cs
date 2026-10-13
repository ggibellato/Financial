using System.Net;
using Financial.Integrations.WebPageParser;
using Xunit.Abstractions;

namespace Financial.WebPageParser.Tests;

/// <summary>
/// Manual verification tests for the Status Invest bond-page scraper.
/// These tests make real HTTP requests and should be run manually, on a
/// machine with normal internet access, to confirm the live page structure
/// before this feature is relied on in production.
/// Category=Live: excluded from PR CI, run with `dotnet test --filter Category=Live`.
/// </summary>
[Trait("Category", "Live")]
public class StatusInvestVerificationTests
{
    private const string ForbiddenReason = "Status Invest answers 403 to this network (cloud runner IPs are blocked); run the Live tests from a normal connection.";

    private readonly ITestOutputHelper _output;

    public StatusInvestVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
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

            var snapshot = SkipWhenForbidden(() => StatusInvest.GetSellValue(bondTitle));

            Assert.True(snapshot.Price > 0, $"Price should be positive, got {snapshot.Price}");

            _output.WriteLine($"  ✓ Slug: {StatusInvest.DeriveSlug(bondTitle)}");
            _output.WriteLine($"  ✓ Price (Valor de Venda): {snapshot.Price:C}");
            _output.WriteLine($"  ✓ AsOf: {snapshot.AsOf}");
            _output.WriteLine("");
        }
    }

    [SkippableFact]
    public void VerifySelectors_UnknownBond_ThrowsInvalidOperationException()
    {
        _output.WriteLine("Testing a bond title that should not resolve to a valid page...");

        Action act = () => StatusInvest.GetSellValue("TESOURO NAOEXISTE 2099");

        var ex = Record.Exception(act);
        Skip.If(ex is StatusInvestBlockedException { StatusCode: HttpStatusCode.Forbidden }, ForbiddenReason);
        Assert.IsType<InvalidOperationException>(ex);
        _output.WriteLine($"  ✓ Correctly threw InvalidOperationException: {ex!.Message}");
    }

    private static T SkipWhenForbidden<T>(Func<T> lookup)
    {
        try
        {
            return lookup();
        }
        catch (StatusInvestBlockedException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new SkipException(ForbiddenReason);
        }
    }
}
