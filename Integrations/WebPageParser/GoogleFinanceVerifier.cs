using Financial.Investment.Infrastructure.Integrations.WebPageParser;
using HtmlAgilityPack;

namespace Financial.Investment.Infrastructure.Integrations;

/// <summary>
/// Manual verification utility to test Google Finance parsing against live URLs.
/// Run this to verify selectors work after Google changes their HTML structure.
/// </summary>
public static class GoogleFinanceVerifier
{
    public static void VerifyMultipleUrls()
    {
        var testUrls = new[]
        {
            ("BBAS3", "BVMF", "https://www.google.com/finance/quote/BBAS3:BVMF"),
            ("KLBN4", "BVMF", "https://www.google.com/finance/quote/KLBN4:BVMF"),
            ("KLBN11", "BVMF", "https://www.google.com/finance/quote/KLBN11:BVMF"),
            ("AAPL", "NASDAQ", "https://www.google.com/finance/quote/AAPL:NASDAQ"),
        };

        Console.WriteLine("=== Google Finance Selector Verification ===\n");

        foreach (var (ticker, exchange, url) in testUrls)
        {
            Console.WriteLine($"Testing: {ticker}:{exchange}");
            Console.WriteLine($"URL: {url}");

            try
            {
                var result = VerifySingleUrl(url);

                if (result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ SUCCESS");
                    Console.ResetColor();
                    Console.WriteLine($"  Asset Name: {result.AssetName}");
                    Console.WriteLine($"  Price: {result.Price}");
                    Console.WriteLine($"  Timestamp: {result.Timestamp}");
                    Console.WriteLine($"  Strategies Used:");
                    Console.WriteLine($"    - Main Container: {result.MainContainerStrategy}");
                    Console.WriteLine($"    - Asset Name: {result.AssetNameStrategy}");
                    Console.WriteLine($"    - Price: {result.PriceStrategy}");
                    Console.WriteLine($"    - Timestamp: {result.TimestampStrategy}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ FAILED");
                    Console.ResetColor();
                    Console.WriteLine($"  Error: {result.ErrorMessage}");
                    Console.WriteLine($"  Strategies Used:");
                    Console.WriteLine($"    - Main Container: {result.MainContainerStrategy}");
                    Console.WriteLine($"    - Asset Name: {result.AssetNameStrategy}");
                    Console.WriteLine($"    - Price: {result.PriceStrategy}");
                    Console.WriteLine($"    - Timestamp: {result.TimestampStrategy}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ EXCEPTION");
                Console.ResetColor();
                Console.WriteLine($"  {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    private static VerificationResult VerifySingleUrl(string url)
    {
        var result = new VerificationResult();

        try
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc = htmlWeb.Load(url);

            result.MainContainerStrategy = TryGetMainData(htmlDoc, out var mainData);
            if (mainData == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to find main data container";
                return result;
            }

            result.AssetNameStrategy = TryReadAssetName(mainData, out var assetName);
            result.AssetName = assetName;

            result.PriceStrategy = TryReadPriceText(mainData, out var priceText);
            result.Price = priceText;

            result.TimestampStrategy = TryReadAsOfText(mainData, out var timestamp);
            result.Timestamp = timestamp;

            result.Success = !string.IsNullOrWhiteSpace(assetName) &&
                             !string.IsNullOrWhiteSpace(priceText);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static string TryGetMainData(HtmlDocument document, out HtmlNode? mainData)
    {
        if ((mainData = GoogleFinance.TryGetMainContainerByClass(document)) != null)
            return "Strategy 1: PrimaryClass";
        if ((mainData = GoogleFinance.TryGetMainContainerByPriceNode(document)) != null)
            return "Strategy 2: Via PriceJsName traversal";
        if ((mainData = GoogleFinance.TryGetMainFallbackTag(document)) != null)
            return "Strategy 3: Fallback to <main> tag";
        return "FAILED: No strategy worked";
    }

    private static string TryReadAssetName(HtmlNode mainData, out string assetName)
    {
        string? result;
        if ((result = GoogleFinance.TryReadAssetNameByClass(mainData)) != null)
        {
            assetName = result;
            return "Strategy 1: PrimaryClass";
        }
        if ((result = GoogleFinance.TryReadAssetNameByContainer(mainData)) != null)
        {
            assetName = result;
            return "Strategy 2: ContainerClass";
        }
        if ((result = GoogleFinance.TryReadAssetNameByText(mainData)) != null)
        {
            assetName = result;
            return "Strategy 3: Pattern matching (first substantial text)";
        }
        assetName = string.Empty;
        return "FAILED: No strategy worked";
    }

    private static string TryReadPriceText(HtmlNode mainData, out string priceText)
    {
        string? result;
        if ((result = GoogleFinance.TryReadPriceByJsName(mainData)) != null)
        {
            priceText = result;
            return "Strategy 1: PrimaryJsName";
        }
        if ((result = GoogleFinance.TryReadPriceByContainer(mainData)) != null)
        {
            priceText = result;
            return "Strategy 2: ContainerClass";
        }
        if ((result = GoogleFinance.TryReadPriceByPattern(mainData)) != null)
        {
            priceText = result;
            return "Strategy 3: Regex pattern matching";
        }
        priceText = string.Empty;
        return "FAILED: No strategy worked";
    }

    private static string TryReadAsOfText(HtmlNode mainData, out string timestamp)
    {
        string? result;
        if ((result = GoogleFinance.TryReadAsOfByClass(mainData)) != null)
        {
            timestamp = result;
            return "Strategy 1: PrimaryClass";
        }
        if ((result = GoogleFinance.TryReadAsOfByPattern(mainData)) != null)
        {
            timestamp = result;
            return "Strategy 2: Date pattern regex";
        }
        timestamp = string.Empty;
        return "Strategy 3: Empty (will use DateTimeOffset.Now)";
    }

    private class VerificationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string MainContainerStrategy { get; set; } = string.Empty;
        public string AssetNameStrategy { get; set; } = string.Empty;
        public string PriceStrategy { get; set; } = string.Empty;
        public string TimestampStrategy { get; set; } = string.Empty;
    }
}
