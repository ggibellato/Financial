using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
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
        var context = new CurrencyConversionContext(brokerCurrency, reportingCurrency, exchangeRateProvider);
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

    private static Task<decimal?> ConvertPointInTimeAsync(decimal? nativeAmount, DateOnly date, CurrencyConversionContext context) =>
        nativeAmount is decimal amount ? context.ConvertAsync(amount, date) : Task.FromResult((decimal?)null);

    private static async Task<(decimal ConvertedInvested, List<AssetCashFlowDTO> GrossFlows, List<AssetCashFlowDTO> NetOfTaxFlows)> ConvertAssetFlowsAsync(
        IReadOnlyList<Asset> assets, CurrencyConversionContext context)
    {
        var grossFlows = new List<AssetCashFlowDTO>();
        var netOfTaxFlows = new List<AssetCashFlowDTO>();
        decimal convertedTotalBought = 0m, convertedTotalSold = 0m;

        foreach (var asset in assets)
        {
            var converted = await ConvertedAssetFlows.ConvertAsync(asset, context).ConfigureAwait(false);
            grossFlows.AddRange(converted.GrossFlows.Select(ToCashFlowDto));
            netOfTaxFlows.AddRange(converted.NetOfTaxFlows.Select(ToCashFlowDto));
            convertedTotalBought += converted.Bought;
            convertedTotalSold += converted.Sold;
        }

        return (convertedTotalBought - convertedTotalSold, grossFlows, netOfTaxFlows);
    }

    private static AssetCashFlowDTO ToCashFlowDto((DateTime Date, decimal Amount) flow) =>
        new() { Date = flow.Date, Amount = flow.Amount };

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
        CurrencyConversionContext context)
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
}
