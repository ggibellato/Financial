using System.Globalization;
using System.Net.Http.Json;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Infrastructure.Services;

public sealed class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;

    public FrankfurterExchangeRateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
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
        catch
        {
            return null;
        }
    }

    private sealed class FrankfurterResponse
    {
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
