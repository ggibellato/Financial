using System.Text.Json;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

/// <summary>
/// Fallback finance service for tickers Google Finance doesn't track (e.g. B3 fractional-lot
/// tickers like BBAS3F). Uses Yahoo Finance's unauthenticated chart JSON endpoint.
/// </summary>
public sealed class YahooFinanceService : IFinanceService
{
    public const string BaseAddress = "https://query1.finance.yahoo.com/";

    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";

    private static readonly IReadOnlyDictionary<string, string> ExchangeSuffixes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BVMF"] = "SA",
            ["NASDAQ"] = string.Empty,
            ["NYSE"] = string.Empty,
            ["LON"] = "L",
        };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;

    public YahooFinanceService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Ticker is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Exchange))
        {
            throw new ArgumentException("Yahoo Finance fallback requires an Exchange.", nameof(request));
        }

        if (!ExchangeSuffixes.TryGetValue(request.Exchange, out var suffix))
        {
            throw new InvalidOperationException(
                $"Exchange '{request.Exchange}' is not supported by the Yahoo Finance fallback.");
        }

        var symbol = suffix.Length == 0 ? request.Ticker : $"{request.Ticker}.{suffix}";

        return FetchSnapshot(symbol, request.Ticker);
    }

    private AssetValueSnapshot FetchSnapshot(string symbol, string ticker)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"v8/finance/chart/{symbol}");

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = _httpClient.Send(httpRequest);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Yahoo Finance request failed for ticker '{ticker}'.", ex);
        }

        using (httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Yahoo Finance returned {(int)httpResponse.StatusCode} for ticker '{ticker}'.");
            }

            YahooChartResponse? response;
            try
            {
                using var stream = httpResponse.Content.ReadAsStream();
                response = JsonSerializer.Deserialize<YahooChartResponse>(stream, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Yahoo Finance returned an unreadable response for ticker '{ticker}'.", ex);
            }

            var meta = response?.Chart?.Result?.FirstOrDefault()?.Meta;
            if (meta?.RegularMarketPrice is not decimal price)
            {
                throw new InvalidOperationException($"Yahoo Finance response did not include a price for ticker '{ticker}'.");
            }

            var name = string.IsNullOrWhiteSpace(meta.ShortName) ? ticker : meta.ShortName.Trim();
            var asOf = meta.RegularMarketTime is long unixSeconds
                ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
                : DateTimeOffset.UtcNow;

            return new AssetValueSnapshot(ticker, name, price, asOf);
        }
    }

    private sealed class YahooChartResponse
    {
        public YahooChart? Chart { get; set; }
    }

    private sealed class YahooChart
    {
        public List<YahooChartResult>? Result { get; set; }
    }

    private sealed class YahooChartResult
    {
        public YahooChartMeta? Meta { get; set; }
    }

    private sealed class YahooChartMeta
    {
        public decimal? RegularMarketPrice { get; set; }

        public long? RegularMarketTime { get; set; }

        public string? ShortName { get; set; }
    }
}
