using System.Globalization;
using System.Net.Http.Json;
using Financial.Shared.Abstractions.Currencies;
using Microsoft.Extensions.Logging;

namespace Financial.Integrations.Frankfurter;

public sealed class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    public const string BaseAddress = "https://api.frankfurter.app/";

    private const int MaxFallbackDays = 10;

    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterExchangeRateProvider> _logger;

    public FrankfurterExchangeRateProvider(HttpClient httpClient, ILogger<FrankfurterExchangeRateProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient.BaseAddress ??= new Uri(BaseAddress);
    }

    public async Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
    {
        for (var offset = 0; offset <= MaxFallbackDays; offset++)
        {
            var rate = await FetchRateAsync(date.AddDays(-offset), from, to).ConfigureAwait(false);
            if (rate is not null)
            {
                return rate;
            }
        }

        return null;
    }

    private async Task<decimal?> FetchRateAsync(DateOnly date, Currency from, Currency to)
    {
        try
        {
            var path = $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}?from={from}&to={to}";
            var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(path).ConfigureAwait(false);

            if (response?.Rates is null || !response.Rates.TryGetValue(to.ToString(), out var rate))
            {
                return null;
            }

            return rate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Failed to fetch exchange rate {From}->{To} for {Date} with {ErrorType}",
                from, to, date, ex.GetType().Name);
            return null;
        }
    }

    private sealed class FrankfurterResponse
    {
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
