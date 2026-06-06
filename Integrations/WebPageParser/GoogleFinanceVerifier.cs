using Financial.Infrastructure.Integrations.WebPageParser;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Financial.Infrastructure.Integrations;

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

            // Test Main Container
            result.MainContainerStrategy = TryGetMainData(htmlDoc, out var mainData);
            if (mainData == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to find main data container";
                return result;
            }

            // Test Asset Name
            result.AssetNameStrategy = TryReadAssetName(mainData, out var assetName);
            result.AssetName = assetName;

            // Test Price
            result.PriceStrategy = TryReadPriceText(mainData, out var priceText);
            result.Price = priceText;

            // Test Timestamp
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
        // Strategy 1: PrimaryClass
        mainData = document.DocumentNode.SelectSingleNode($"//div[@class='{GoogleFinanceSelectors.MainContainer.PrimaryClass}']");
        if (mainData != null)
            return "Strategy 1: PrimaryClass (KycIzb)";

        // Strategy 2: Via PriceJsName
        var priceNode = document.DocumentNode.SelectSingleNode($"//span[@jsname='{GoogleFinanceSelectors.MainContainer.PriceJsName}']");
        if (priceNode != null)
        {
            var container = priceNode.ParentNode;
            for (int i = 0; i < 8 && container != null; i++)
            {
                var xpath = $".//div[contains(@class, '{GoogleFinanceSelectors.AssetName.PrimaryClass}')]";
                var nameNode = container.SelectSingleNode(xpath);
                if (nameNode != null)
                {
                    mainData = container;
                    return "Strategy 2: Via PriceJsName traversal";
                }
                container = container.ParentNode;
            }
        }

        // Strategy 3: Main tag
        mainData = document.DocumentNode.SelectSingleNode("//main");
        if (mainData != null)
            return "Strategy 3: Fallback to <main> tag";

        return "FAILED: No strategy worked";
    }

    private static string TryReadAssetName(HtmlNode mainData, out string assetName)
    {
        // Strategy 1: PrimaryClass
        var nameNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.AssetName.PrimaryClass}']");
        if (nameNode != null)
        {
            assetName = nameNode.InnerText.Trim();
            return "Strategy 1: PrimaryClass (gO24Ff)";
        }

        // Strategy 2: ContainerClass
        var containerNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.AssetName.ContainerClass}']");
        if (containerNode != null)
        {
            var firstDiv = containerNode.SelectSingleNode(".//div");
            if (firstDiv != null)
            {
                assetName = firstDiv.InnerText.Trim();
                return "Strategy 2: ContainerClass (YTGvuc)";
            }
        }

        // Strategy 3: Pattern matching
        var divNodes = mainData.SelectNodes(".//div");
        if (divNodes != null)
        {
            foreach (var div in divNodes.Take(10))
            {
                var text = div.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text.Length > 5 && text.Length < 100)
                {
                    assetName = text;
                    return "Strategy 3: Pattern matching (first substantial text)";
                }
            }
        }

        assetName = string.Empty;
        return "FAILED: No strategy worked";
    }

    private static string TryReadPriceText(HtmlNode mainData, out string priceText)
    {
        // Strategy 1: PrimaryJsName
        var priceNode = mainData.SelectSingleNode($".//span[@jsname='{GoogleFinanceSelectors.Price.PrimaryJsName}']");
        if (priceNode != null)
        {
            priceText = priceNode.InnerText.Trim();
            return "Strategy 1: PrimaryJsName (Pdsbrc)";
        }

        // Strategy 2: ContainerClass
        var priceContainer = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.Price.ContainerClass}']");
        if (priceContainer != null)
        {
            var priceSpan = priceContainer.SelectSingleNode(".//span");
            if (priceSpan != null)
            {
                priceText = priceSpan.InnerText.Trim();
                return "Strategy 2: ContainerClass (N6SYTe)";
            }
        }

        // Strategy 3: Regex pattern
        var allSpans = mainData.SelectNodes(".//span");
        if (allSpans != null)
        {
            foreach (var span in allSpans)
            {
                var text = span.InnerText.Trim();
                if (Regex.IsMatch(text, GoogleFinanceSelectors.Price.PricePattern))
                {
                    priceText = text;
                    return "Strategy 3: Regex pattern matching";
                }
            }
        }

        priceText = string.Empty;
        return "FAILED: No strategy worked";
    }

    private static string TryReadAsOfText(HtmlNode mainData, out string timestamp)
    {
        // Strategy 1: PrimaryClass
        var timeNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.Timestamp.PrimaryClass}']");
        if (timeNode != null)
        {
            timestamp = timeNode.InnerText.Trim();
            return "Strategy 1: PrimaryClass (jZZ2de)";
        }

        // Strategy 2: Date pattern
        var allDivs = mainData.SelectNodes(".//div");
        if (allDivs != null)
        {
            foreach (var div in allDivs)
            {
                var text = div.InnerText.Trim();
                if (Regex.IsMatch(text, GoogleFinanceSelectors.Timestamp.DatePattern, RegexOptions.IgnoreCase))
                {
                    timestamp = text;
                    return "Strategy 2: Date pattern regex";
                }
            }
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
