using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Infrastructure.Integrations.WebPageParser;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed class AssetTypeLookup : IAssetTypeLookup
{
    private static readonly HttpClient HttpClient = new HttpClient();
    private static readonly string[] BrazilFiiPaths =
    {
        "bolsa/fundos-imobiliarios",
        "bolsa/fiis",
        "fundos-imobiliarios",
        "fiis"
    };

    public async Task<AssetTypeLookupResultDTO> LookupAsync(AssetTypeLookupRequestDTO request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var ticker = NormalizeTicker(request.Ticker);
        if (request.FallbackCountry == CountryCode.BR)
        {
            var brazilType = await TryResolveBrazilTypeAsync(ticker);
            if (!string.IsNullOrWhiteSpace(brazilType))
            {
                return new AssetTypeLookupResultDTO
                {
                    Country = CountryCode.BR,
                    LocalTypeCode = brazilType
                };
            }
        }

        var googleType = await TryResolveGoogleFinanceTypeAsync(request, ticker);
        if (!string.IsNullOrWhiteSpace(googleType))
        {
            var inferredCountry = ResolveCountryFromExchange(request.Exchange);
            return new AssetTypeLookupResultDTO
            {
                Country = inferredCountry,
                LocalTypeCode = googleType
            };
        }

        return new AssetTypeLookupResultDTO();
    }

    private static async Task<string> TryResolveBrazilTypeAsync(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return string.Empty;
        }

        foreach (var path in BrazilFiiPaths)
        {
            var html = await TryLoadPageAsync($"https://www.dadosdemercado.com.br/{path}/{ticker}");
            if (!string.IsNullOrWhiteSpace(html))
            {
                return "FII";
            }
        }

        var equityHtml = await TryLoadPageAsync($"https://www.dadosdemercado.com.br/bolsa/acoes/{ticker}");
        return string.IsNullOrWhiteSpace(equityHtml) ? string.Empty : "Acoes";
    }

    private static async Task<string> TryResolveGoogleFinanceTypeAsync(AssetTypeLookupRequestDTO request, string ticker)
    {
        foreach (var url in BuildGoogleFinanceUrls(request, ticker))
        {
            var html = await TryLoadPageAsync(url);
            if (string.IsNullOrWhiteSpace(html))
            {
                continue;
            }

            var localType = GoogleFinanceAssetTypeParser.TryParseLocalTypeCode(html);
            if (!string.IsNullOrWhiteSpace(localType))
            {
                return localType;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> BuildGoogleFinanceUrls(AssetTypeLookupRequestDTO request, string ticker)
    {
        var urls = new List<string>();
        if (!string.IsNullOrWhiteSpace(ticker))
        {
            if (!string.IsNullOrWhiteSpace(request.Exchange))
            {
                urls.Add(BuildGoogleFinanceQuoteUrl($"{ticker}:{request.Exchange}"));
            }

            urls.Add(BuildGoogleFinanceQuoteUrl(ticker));
        }

        if (!string.IsNullOrWhiteSpace(request.ISIN))
        {
            urls.Add(BuildGoogleFinanceQuoteUrl(request.ISIN));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            urls.Add(BuildGoogleFinanceQuoteUrl(request.Name));
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildGoogleFinanceQuoteUrl(string value)
    {
        return $"https://www.google.com/finance/quote/{Uri.EscapeDataString(value)}";
    }

    private static async Task<string> TryLoadPageAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Url must be provided.", nameof(url));
        }

        using var response = await HttpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        return await response.Content.ReadAsStringAsync();
    }

    private static string NormalizeTicker(string ticker)
    {
        return string.IsNullOrWhiteSpace(ticker) ? string.Empty : ticker.Trim();
    }

    private static CountryCode ResolveCountryFromExchange(string exchange)
    {
        if (string.IsNullOrWhiteSpace(exchange))
        {
            return CountryCode.Unknown;
        }

        return exchange.Trim().ToUpperInvariant() switch
        {
            "BVMF" => CountryCode.BR,
            "LON" or "LSE" => CountryCode.UK,
            "NYSE" or "NASDAQ" or "AMEX" => CountryCode.US,
            _ => CountryCode.Unknown
        };
    }
}
