using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Microsoft.Extensions.Logging;

namespace Financial.Integrations.Frankfurter;

public sealed class FrankfurterExchangeRateProvider : IExchangeRateProvider, IUsdRateFetcher
{
    public const string BaseAddress = "https://api.frankfurter.dev/v2/";

    private static readonly TimeSpan DefaultCallBudget = TimeSpan.FromSeconds(10);

    private const int MaxFallbackDays = 10;

    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterExchangeRateProvider> _logger;
    private readonly TimeSpan _callBudget;

    public FrankfurterExchangeRateProvider(
        HttpClient httpClient,
        ILogger<FrankfurterExchangeRateProvider> logger,
        TimeSpan? callBudget = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _callBudget = callBudget ?? DefaultCallBudget;
        _httpClient.BaseAddress ??= new Uri(BaseAddress);
    }

    public async Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
    {
        using var budget = new CancellationTokenSource(_callBudget);

        for (var offset = 0; offset <= MaxFallbackDays; offset++)
        {
            var rates = await QueryRatesAsync(date.AddDays(-offset), $"base={from}&quotes={to}", budget.Token).ConfigureAwait(false);
            if (rates is null)
            {
                return null;
            }

            if (rates.TryGetValue(to.ToString(), out var rate))
            {
                return rate;
            }
        }

        return null;
    }

    public async Task<UsdRateFetchResult> FetchAsync(DateOnly date)
    {
        using var budget = new CancellationTokenSource(_callBudget);

        for (var offset = 0; offset <= MaxFallbackDays; offset++)
        {
            var rates = await QueryRatesAsync(date.AddDays(-offset), $"base={Currency.USD}&quotes={Currency.BRL},{Currency.GBP}", budget.Token)
                .ConfigureAwait(false);
            if (rates is null)
            {
                break;
            }

            var brlRate = RateOf(rates, Currency.BRL);
            var gbpRate = RateOf(rates, Currency.GBP);
            if (brlRate is not null || gbpRate is not null)
            {
                return new UsdRateFetchResult(brlRate, gbpRate);
            }
        }

        return new UsdRateFetchResult(null, null);
    }

    private static decimal? RateOf(Dictionary<string, decimal> rates, Currency currency) =>
        rates.TryGetValue(currency.ToString(), out var rate) ? rate : null;

    private async Task<Dictionary<string, decimal>?> QueryRatesAsync(DateOnly date, string query, CancellationToken cancellationToken)
    {
        try
        {
            var path = $"rates?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&{query}";
            using var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }

            response.EnsureSuccessStatusCode();
            var quotes = await response.Content.ReadFromJsonAsync<List<FrankfurterQuote>>(cancellationToken).ConfigureAwait(false);
            return quotes?.ToDictionary(quote => quote.Quote, quote => quote.Rate) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Failed to fetch exchange rates ({Query}) for {Date} with {ErrorType}",
                query, date, ex.GetType().Name);
            return null;
        }
    }

    private sealed class FrankfurterQuote
    {
        public string Quote { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }
}
