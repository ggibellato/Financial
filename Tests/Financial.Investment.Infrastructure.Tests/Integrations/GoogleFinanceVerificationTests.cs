using Financial.Infrastructure.Integrations;
using Financial.Infrastructure.Integrations.WebPageParser;
using Xunit.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

/// <summary>
/// Manual verification tests for Google Finance selectors.
/// These tests make real HTTP requests and should be run manually when verifying selector changes.
/// Mark as [Fact] to run, or keep as [Fact(Skip = "Manual")] to skip in CI.
/// </summary>
public class GoogleFinanceVerificationTests
{
    private readonly ITestOutputHelper _output;

    public GoogleFinanceVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_WithMultipleBrazilianStocks()
    {
        _output.WriteLine("Testing Google Finance selectors with live data...");
        _output.WriteLine("");

        // Test multiple Brazilian stocks
        var stocks = new[]
        {
            ("BBAS3", "BVMF", "Banco do Brasil"),
            ("KLBN4", "BVMF", "Klabin"),
            ("KLBN11", "BVMF", "Klabin Unit"),
        };

        foreach (var (ticker, exchange, expectedNamePart) in stocks)
        {
            _output.WriteLine($"Testing {ticker}:{exchange}...");

            var snapshot = GoogleFinance.GetFinancialInfoSnapshot(exchange, ticker);

            Assert.NotNull(snapshot);
            Assert.Equal(ticker, snapshot.Ticker);
            Assert.Contains(expectedNamePart, snapshot.Name, StringComparison.OrdinalIgnoreCase);
            Assert.True(snapshot.Price > 0, $"Price should be positive, got {snapshot.Price}");
            Assert.True(snapshot.AsOf != default, "AsOf should be set");

            _output.WriteLine($"  ✓ Name: {snapshot.Name}");
            _output.WriteLine($"  ✓ Price: {snapshot.Price:C}");
            _output.WriteLine($"  ✓ AsOf: {snapshot.AsOf}");
            _output.WriteLine("");
        }
    }

    [Fact(Skip = "Manual verification test - requires internet connection")]
    public void VerifySelectors_WithInternationalStocks()
    {
        _output.WriteLine("Testing Google Finance selectors with international stocks...");
        _output.WriteLine("");

        var stocks = new[]
        {
            ("AAPL", "NASDAQ", "Apple"),
            ("MSFT", "NASDAQ", "Microsoft"),
        };

        foreach (var (ticker, exchange, expectedNamePart) in stocks)
        {
            _output.WriteLine($"Testing {ticker}:{exchange}...");

            var snapshot = GoogleFinance.GetFinancialInfoSnapshot(exchange, ticker);

            Assert.NotNull(snapshot);
            Assert.Equal(ticker, snapshot.Ticker);
            Assert.Contains(expectedNamePart, snapshot.Name, StringComparison.OrdinalIgnoreCase);
            Assert.True(snapshot.Price > 0, $"Price should be positive, got {snapshot.Price}");

            _output.WriteLine($"  ✓ Name: {snapshot.Name}");
            _output.WriteLine($"  ✓ Price: ${snapshot.Price}");
            _output.WriteLine($"  ✓ AsOf: {snapshot.AsOf}");
            _output.WriteLine("");
        }
    }

    [Fact(Skip = "Manual - use this to run the detailed verifier utility")]
    public void RunDetailedVerification()
    {
        // This will show which strategies were used for each element
        GoogleFinanceVerifier.VerifyMultipleUrls();
    }
}
