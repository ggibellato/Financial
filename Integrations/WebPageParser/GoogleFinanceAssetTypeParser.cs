using System;
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
        if (ContainsKeyword(html, "Real Estate Investment Trust"))
        {
            return "REIT";
        }

        if (ContainsKeyword(html, "REIT"))
        {
            return "REIT";
        }

        if (ContainsKeyword(html, "ETF"))
        {
            return "ETF";
        }

        if (ContainsKeyword(html, "Mutual Fund"))
        {
            return "Fund";
        }

        if (ContainsKeyword(html, "Bond"))
        {
            return "Bond";
        }

        if (ContainsKeyword(html, "Gilt"))
        {
            return "ConventionalGilt";
        }

        if (ContainsKeyword(html, "Stock") || ContainsKeyword(html, "Equity"))
        {
            return "Stock";
        }

        if (ContainsKeyword(html, "Cash"))
        {
            return "Cash";
        }

        if (ContainsKeyword(html, "Pension"))
        {
            return "Pension";
        }

        return string.Empty;
    }

    private static bool ContainsKeyword(string html, string keyword)
    {
        return html.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string MapTokenToLocalTypeCode(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var normalized = NormalizeToken(token);
        return normalized switch
        {
            "REIT" or "REALESTATEINVESTMENTTRUST" => "REIT",
            "ETF" => "ETF",
            "MUTUALFUND" or "FUND" => "Fund",
            "BOND" or "GOVERNMENTBOND" => "Bond",
            "GILT" => "ConventionalGilt",
            "STOCK" or "EQUITY" => "Stock",
            "CASH" => "Cash",
            "PENSION" => "Pension",
            _ => string.Empty
        };
    }

    private static string NormalizeToken(string token)
    {
        return token
            .Trim()
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
    }
}
