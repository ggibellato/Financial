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
        decimal? marketValueSum,
        decimal? unrealisedGainSum,
        IExchangeRateProvider exchangeRateProvider,
        IXirrCalculationService xirrCalculationService,
        DateTime asOf)
    {
        var context = new ConversionContext(brokerCurrency, reportingCurrency, exchangeRateProvider);
        var today = DateOnly.FromDateTime(asOf);

        var convertedMarketValue = await ConvertPointInTimeAsync(marketValueSum, today, context).ConfigureAwait(false);
        var convertedUnrealisedGainLoss = await ConvertPointInTimeAsync(unrealisedGainSum, today, context).ConfigureAwait(false);
        var (convertedInvested, grossFlows, netOfTaxFlows) = await ConvertAssetFlowsAsync(assets, context).ConfigureAwait(false);
        var (convertedTotalReturn, convertedTotalReturnNetOfTax) = ComputeConvertedReturns(
            convertedMarketValue, grossFlows, netOfTaxFlows, xirrCalculationService, asOf);

        return BuildResult(
            reportingCurrency, convertedMarketValue, convertedInvested, convertedUnrealisedGainLoss,
            convertedTotalReturn, convertedTotalReturnNetOfTax, context);
    }

    private static Task<decimal?> ConvertPointInTimeAsync(decimal? nativeAmount, DateOnly date, ConversionContext context) =>
        nativeAmount is decimal amount ? context.ConvertAsync(amount, date) : Task.FromResult((decimal?)null);

    private static async Task<(decimal ConvertedInvested, List<AssetCashFlowDTO> GrossFlows, List<AssetCashFlowDTO> NetOfTaxFlows)> ConvertAssetFlowsAsync(
        IReadOnlyList<Asset> assets, ConversionContext context)
    {
        var grossFlows = new List<AssetCashFlowDTO>();
        var netOfTaxFlows = new List<AssetCashFlowDTO>();
        decimal convertedTotalBought = 0m, convertedTotalSold = 0m;

        foreach (var asset in assets)
        {
            var converted = await ConvertAssetAsync(asset, context).ConfigureAwait(false);
            grossFlows.AddRange(converted.GrossFlows);
            netOfTaxFlows.AddRange(converted.NetOfTaxFlows);
            convertedTotalBought += converted.Bought;
            convertedTotalSold += converted.Sold;
        }

        return (convertedTotalBought - convertedTotalSold, grossFlows, netOfTaxFlows);
    }

    private static async Task<AssetConversionResult> ConvertAssetAsync(Asset asset, ConversionContext context)
    {
        var grossFlows = new List<AssetCashFlowDTO>();
        var netOfTaxFlows = new List<AssetCashFlowDTO>();
        decimal bought = 0m, sold = 0m;

        foreach (var transaction in asset.Transactions)
        {
            var convertedNetCash = await context.ConvertAsync(transaction.NetCash, DateOnly.FromDateTime(transaction.Date)).ConfigureAwait(false);
            if (convertedNetCash is not decimal amount)
            {
                continue;
            }

            grossFlows.Add(new AssetCashFlowDTO { Date = transaction.Date, Amount = amount });
            netOfTaxFlows.Add(new AssetCashFlowDTO { Date = transaction.Date, Amount = amount });

            switch (TransactionTypeEffects.For(transaction.Type).Cash)
            {
                case CashEffect.Out:
                    bought += -amount;
                    break;
                case CashEffect.In:
                    sold += amount;
                    break;
            }
        }

        foreach (var credit in asset.Credits)
        {
            var date = DateOnly.FromDateTime(credit.Date);
            var convertedValue = await context.ConvertAsync(credit.Value, date).ConfigureAwait(false);
            var convertedNetAmount = await context.ConvertAsync(credit.NetAmount, date).ConfigureAwait(false);

            if (convertedValue is decimal grossAmount)
            {
                grossFlows.Add(new AssetCashFlowDTO { Date = credit.Date, Amount = grossAmount });
            }

            if (convertedNetAmount is decimal netAmount)
            {
                netOfTaxFlows.Add(new AssetCashFlowDTO { Date = credit.Date, Amount = netAmount });
            }
        }

        return new AssetConversionResult(grossFlows, netOfTaxFlows, bought, sold);
    }

    private static (decimal? TotalReturn, decimal? TotalReturnNetOfTax) ComputeConvertedReturns(
        decimal? convertedMarketValue,
        IReadOnlyList<AssetCashFlowDTO> grossFlows,
        IReadOnlyList<AssetCashFlowDTO> netOfTaxFlows,
        IXirrCalculationService xirrCalculationService,
        DateTime asOf)
    {
        if (convertedMarketValue is not decimal marketValue)
        {
            return (null, null);
        }

        return (
            xirrCalculationService.Calculate(grossFlows, marketValue, asOf),
            xirrCalculationService.Calculate(netOfTaxFlows, marketValue, asOf));
    }

    private static ConvertedSummaryResult BuildResult(
        Currency reportingCurrency,
        decimal? convertedMarketValue,
        decimal? convertedInvested,
        decimal? convertedUnrealisedGainLoss,
        decimal? convertedTotalReturn,
        decimal? convertedTotalReturnNetOfTax,
        ConversionContext context)
    {
        if (context.IsUnavailable)
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
            IsPartial: context.IsPartial,
            IsReportingCurrencyUnavailable: false);
    }

    private readonly record struct AssetConversionResult(
        List<AssetCashFlowDTO> GrossFlows,
        List<AssetCashFlowDTO> NetOfTaxFlows,
        decimal Bought,
        decimal Sold);

    /// <summary>Caches one rate lookup per distinct date and tracks attempt/failure counts across
    /// the whole build, since every conversion within one call shares the same currency pair.</summary>
    private sealed class ConversionContext(Currency from, Currency to, IExchangeRateProvider exchangeRateProvider)
    {
        private readonly Dictionary<DateOnly, decimal?> _rateCache = [];

        public int AttemptCount { get; private set; }
        public int FailureCount { get; private set; }
        public bool IsUnavailable => AttemptCount > 0 && FailureCount == AttemptCount;
        public bool IsPartial => FailureCount > 0;

        public async Task<decimal?> ConvertAsync(decimal amount, DateOnly date)
        {
            if (from == to)
            {
                return amount;
            }

            if (!_rateCache.TryGetValue(date, out var rate))
            {
                AttemptCount++;
                rate = await exchangeRateProvider.GetHistoricalRateAsync(date, from, to).ConfigureAwait(false);
                _rateCache[date] = rate;
                if (rate is null)
                {
                    FailureCount++;
                }
            }

            return rate.HasValue ? amount * rate.Value : null;
        }
    }
}
