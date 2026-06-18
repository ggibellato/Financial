using System.Text.RegularExpressions;

namespace Financial.Infrastructure.Integrations.WebPageParser;

public static class GoogleFinanceAssetTypeParser
{
    private static readonly Regex[] TypePatterns =
    {
        new Regex("\"quoteType\"\\s*:\\s*\"(?<type>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex("\"instrumentType\"\\s*:\\s*\"(?<type>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex("\"assetClass\"\\s*:\\s*\"(?<type>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    private static readonly Dictionary<string, string> TokenMap = new(StringComparer.Ordinal)
    {
        ["REIT"] = "REIT",
        ["REALESTATEINVESTMENTTRUST"] = "REIT",
        ["ETF"] = "ETF",
        ["MUTUALFUND"] = "Fund",
        ["FUND"] = "Fund",
        ["BOND"] = "Bond",
        ["GOVERNMENTBOND"] = "Bond",
        ["GILT"] = "ConventionalGilt",
        ["STOCK"] = "Stock",
        ["EQUITY"] = "Stock",
        ["CASH"] = "Cash",
        ["PENSION"] = "Pension",
    };

    private static readonly (string Keyword, string LocalTypeCode)[] KeywordTable =
    {
        ("Real Estate Investment Trust", "REIT"),
        ("REIT", "REIT"),
        ("ETF", "ETF"),
        ("Mutual Fund", "Fund"),
        ("Bond", "Bond"),
        ("Gilt", "ConventionalGilt"),
        ("Stock", "Stock"),
        ("Equity", "Stock"),
        ("Cash", "Cash"),
        ("Pension", "Pension"),
    };

    public static string TryParseLocalTypeCode(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        foreach (var pattern in TypePatterns)
        {
            var match = pattern.Match(html);
            if (match.Success)
            {
                var localType = MapTokenToLocalTypeCode(match.Groups["type"].Value);
                if (!string.IsNullOrWhiteSpace(localType))
                {
                    return localType;
                }
            }
        }

        return TryParseKeyword(html);
    }

    private static string TryParseKeyword(string html)
    {
        foreach (var (keyword, localTypeCode) in KeywordTable)
        {
            if (ContainsKeyword(html, keyword))
            {
                return localTypeCode;
            }
        }

        return string.Empty;
    }

    private static bool ContainsKeyword(string html, string keyword) =>
        html.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string MapTokenToLocalTypeCode(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var normalized = NormalizeToken(token);
        return TokenMap.TryGetValue(normalized, out var localType) ? localType : string.Empty;
    }

    private static string NormalizeToken(string token) =>
        token
            .Trim()
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
}
