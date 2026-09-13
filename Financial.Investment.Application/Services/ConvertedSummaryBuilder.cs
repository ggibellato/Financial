using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Services;

internal readonly record struct ConvertedSummaryResult(
    Currency ReportingCurrency,
    decimal? ConvertedMarketValue,
    decimal? ConvertedInvested,
    decimal? ConvertedUnrealisedGainLoss,
    decimal? ConvertedTotalReturn,
    decimal? ConvertedTotalReturnNetOfTax,
    bool IsPartial,
    bool IsReportingCurrencyUnavailable);

/// <summary>
/// <see cref="ConvertedSummaryResult.ConvertedInvested"/> is deliberately the sum of each converted
/// Buy-effect transaction minus each converted Sell-effect one - not a converted version of
/// <see cref="AggregatedSummaryDTO.TotalInvested"/>, which for Active scope is a weighted-average
/// cost basis, a derived figure rather than a sum of dated amounts. The two can differ numerically
/// even when the two currencies are identical; that is expected, not a bug.
/// </summary>
internal static class ConvertedSummaryBuilder
{
    public static async Task<ConvertedSummaryResult> BuildAsync(
        IReadOnlyList<Asset> assets,
        Currency brokerCurrency,
        Currency reportingCurrency,
        decimal marketValueSum,
        decimal unrealisedGainSum,
        IExchangeRateProvider exchangeRateProvider,
        IXirrCalculationService xirrCalculationService,
        DateTime asOf)
    {
        var tracker = new ConversionTracker();
        var rateCache = new Dictionary<DateOnly, decimal?>();
        var today = DateOnly.FromDateTime(asOf);

        var convertedMarketValue = await ConvertAsync(marketValueSum, brokerCurrency, reportingCurrency, today, exchangeRateProvider, rateCache, tracker).ConfigureAwait(false);
        var convertedUnrealisedGainLoss = await ConvertAsync(unrealisedGainSum, brokerCurrency, reportingCurrency, today, exchangeRateProvider, rateCache, tracker).ConfigureAwait(false);

        decimal convertedTotalBought = 0m, convertedTotalSold = 0m;
        var grossFlows = new List<AssetCashFlowDTO>();
        var netOfTaxFlows = new List<AssetCashFlowDTO>();

        foreach (var asset in assets)
        {
            foreach (var transaction in asset.Transactions)
            {
                var date = DateOnly.FromDateTime(transaction.Date);
                var convertedNetCash = await ConvertAsync(transaction.NetCash, brokerCurrency, reportingCurrency, date, exchangeRateProvider, rateCache, tracker).ConfigureAwait(false);
                if (convertedNetCash is not decimal amount)
                {
                    continue;
                }

                grossFlows.Add(new AssetCashFlowDTO { Date = transaction.Date, Amount = amount });
                netOfTaxFlows.Add(new AssetCashFlowDTO { Date = transaction.Date, Amount = amount });

                switch (TransactionTypeEffects.For(transaction.Type).Cash)
                {
                    case CashEffect.Out:
                        convertedTotalBought += -amount;
                        break;
                    case CashEffect.In:
                        convertedTotalSold += amount;
                        break;
                }
            }

            foreach (var credit in asset.Credits)
            {
                var date = DateOnly.FromDateTime(credit.Date);
                var convertedValue = await ConvertAsync(credit.Value, brokerCurrency, reportingCurrency, date, exchangeRateProvider, rateCache, tracker).ConfigureAwait(false);
                var convertedNetAmount = await ConvertAsync(credit.NetAmount, brokerCurrency, reportingCurrency, date, exchangeRateProvider, rateCache, tracker).ConfigureAwait(false);

                if (convertedValue is decimal grossAmount)
                {
                    grossFlows.Add(new AssetCashFlowDTO { Date = credit.Date, Amount = grossAmount });
                }

                if (convertedNetAmount is decimal netAmount)
                {
                    netOfTaxFlows.Add(new AssetCashFlowDTO { Date = credit.Date, Amount = netAmount });
                }
            }
        }

        var convertedInvested = convertedTotalBought - convertedTotalSold;

        decimal? convertedTotalReturn = null;
        decimal? convertedTotalReturnNetOfTax = null;
        if (convertedMarketValue.HasValue)
        {
            convertedTotalReturn = xirrCalculationService.Calculate(grossFlows, convertedMarketValue.Value, asOf);
            convertedTotalReturnNetOfTax = xirrCalculationService.Calculate(netOfTaxFlows, convertedMarketValue.Value, asOf);
        }

        var isUnavailable = tracker.AttemptCount > 0 && tracker.FailureCount == tracker.AttemptCount;
        if (isUnavailable)
        {
            return new ConvertedSummaryResult(
                reportingCurrency, null, null, null, null, null,
                IsPartial: false, IsReportingCurrencyUnavailable: true);
        }

        return new ConvertedSummaryResult(
            reportingCurrency,
            convertedMarketValue,
            convertedInvested,
            convertedUnrealisedGainLoss,
            convertedTotalReturn,
            convertedTotalReturnNetOfTax,
            IsPartial: tracker.FailureCount > 0,
            IsReportingCurrencyUnavailable: false);
    }

    private static async Task<decimal?> ConvertAsync(
        decimal amount,
        Currency from,
        Currency to,
        DateOnly date,
        IExchangeRateProvider exchangeRateProvider,
        Dictionary<DateOnly, decimal?> rateCache,
        ConversionTracker tracker)
    {
        if (from == to)
        {
            return amount;
        }

        if (!rateCache.TryGetValue(date, out var rate))
        {
            tracker.CountAttempt();
            rate = await exchangeRateProvider.GetHistoricalRateAsync(date, from, to).ConfigureAwait(false);
            rateCache[date] = rate;
            if (rate is null)
            {
                tracker.CountFailure();
            }
        }

        return rate.HasValue ? amount * rate.Value : null;
    }

    private sealed class ConversionTracker
    {
        public int AttemptCount { get; private set; }
        public int FailureCount { get; private set; }

        public void CountAttempt() => AttemptCount++;
        public void CountFailure() => FailureCount++;
    }
}
