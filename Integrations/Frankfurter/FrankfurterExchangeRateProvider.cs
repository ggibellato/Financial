using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Microsoft.Extensions.Logging;

namespace Financial.Integrations.Frankfurter;

public sealed class FrankfurterExchangeRateProvider : IExchangeRateProvider, IUsdRateFetcher
{
    public const string BaseAddress = "https://api.frankfurter.dev/v1/";

    public static readonly TimeSpan DefaultCallBudget = TimeSpan.FromSeconds(10);

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
            var reply = await FetchAsync(date.AddDays(-offset), $"from={from}&to={to}", budget.Token).ConfigureAwait(false);
            if (reply.Failed)
            {
                return null;
            }

            if (reply.Rates is not null && reply.Rates.TryGetValue(to.ToString(), out var rate))
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
            var reply = await FetchAsync(date.AddDays(-offset), $"from={Currency.USD}&to={Currency.BRL},{Currency.GBP}", budget.Token)
                .ConfigureAwait(false);
            if (reply.Failed)
            {
                break;
            }

            var brlRate = RateOf(reply.Rates, Currency.BRL);
            var gbpRate = RateOf(reply.Rates, Currency.GBP);
            if (brlRate is not null || gbpRate is not null)
            {
                return new UsdRateFetchResult(brlRate, gbpRate);
            }
        }

        return new UsdRateFetchResult(null, null);
    }

    private static decimal? RateOf(Dictionary<string, decimal>? rates, Currency currency) =>
        rates is not null && rates.TryGetValue(currency.ToString(), out var rate) ? rate : null;

    private async Task<Reply> FetchAsync(DateOnly date, string query, CancellationToken cancellationToken)
    {
        try
        {
            var path = $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}?{query}";
            using var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new Reply(Failed: false, Rates: null);
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<FrankfurterResponse>(cancellationToken).ConfigureAwait(false);
            return new Reply(Failed: false, body?.Rates);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Failed to fetch exchange rates ({Query}) for {Date} with {ErrorType}",
                query, date, ex.GetType().Name);
            return new Reply(Failed: true, Rates: null);
        }
    }

    private sealed record Reply(bool Failed, Dictionary<string, decimal>? Rates);

    private sealed class FrankfurterResponse
    {
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
