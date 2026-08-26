using System.Globalization;
using System.Net.Http.Json;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Infrastructure.Services;

public sealed class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterExchangeRateProvider> _logger;

    public FrankfurterExchangeRateProvider(HttpClient httpClient, ILogger<FrankfurterExchangeRateProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
