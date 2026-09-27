using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Services;

internal static class AllocationBreakdownBuilder
{
    internal static async Task<AllocationBreakdownDTO> BuildAsync(
        IReadOnlyList<AllocationHolding> holdings,
        IHoldingValuationService holdingValuationService,
        Currency? displayCurrency,
        IExchangeRateProvider exchangeRateProvider,
        DateOnly asOf)
    {
        var priced = holdings
            .Select(holding => ToPricedHolding(holding, holdingValuationService))
            .Where(holding => holding.HasValue)
            .Select(holding => holding!.Value)
            .ToList();

        if (displayCurrency is null)
        {
            return BuildResult(priced, displayCurrency: null, isPartial: false, isUnavailable: false);
        }

        var (converted, isPartial, isUnavailable) = await ConvertAsync(priced, displayCurrency.Value, exchangeRateProvider, asOf).ConfigureAwait(false);
        return BuildResult(converted, displayCurrency.Value.ToString(), isPartial, isUnavailable);
    }

    private static async Task<(IReadOnlyList<PricedHolding> Converted, bool IsPartial, bool IsUnavailable)> ConvertAsync(
        IReadOnlyList<PricedHolding> holdings, Currency displayCurrency, IExchangeRateProvider exchangeRateProvider, DateOnly asOf)
    {
        var converted = new List<PricedHolding>();
        var contexts = new List<CurrencyConversionContext>();

        foreach (var group in holdings.GroupBy(holding => holding.Currency))
        {
            var context = new CurrencyConversionContext(group.Key, displayCurrency, exchangeRateProvider);
            contexts.Add(context);

            foreach (var holding in group)
            {
                var convertedValue = await context.ConvertAsync(holding.MarketValue, asOf).ConfigureAwait(false);
                if (convertedValue is decimal marketValue)
                {
                    converted.Add(holding with { MarketValue = marketValue });
                }
            }
        }

        var isUnavailable = contexts.Any(context => context.AttemptCount > 0) && contexts.All(context => context.IsUnavailable);
        var isPartial = !isUnavailable && contexts.Any(context => context.FailureCount > 0);
        return (converted, isPartial, isUnavailable);
    }

    private static AllocationBreakdownDTO BuildResult(
        IReadOnlyList<PricedHolding> priced, string? displayCurrency, bool isPartial, bool isUnavailable) => new()
    {
        ByClass = Group(
            priced, holding => holding.Class, key => key.ToString(),
            (key, marketValue, percentage) => new AssetClassAllocationEntryDTO(key, marketValue, percentage)),
        ByCurrency = Group(
            priced, holding => holding.Currency, key => key.ToString(),
            (key, marketValue, percentage) => new CurrencyAllocationEntryDTO(key.ToString(), marketValue, percentage)),
        ByCountry = Group(
            priced, holding => holding.Country, key => key.ToString(),
            (key, marketValue, percentage) => new CountryAllocationEntryDTO(key, marketValue, percentage)),
        ByBroker = Group(
            priced, holding => holding.BrokerName, key => key,
            (key, marketValue, percentage) => new BrokerAllocationEntryDTO(key, marketValue, percentage)),
        DisplayCurrency = displayCurrency,
        IsPartial = isPartial,
        IsUnavailable = isUnavailable,
    };

    private static PricedHolding? ToPricedHolding(
        AllocationHolding holding, IHoldingValuationService holdingValuationService)
    {
        var valuation = holdingValuationService.GetValuation(holding.Asset, InvestmentScope.Active);
        var weightBasis = AssetAmountBases
            .For(InvestmentScope.Active, AssetTotals.For(holding.Asset), valuation.MarketValue)
            .WeightBasis;

        return weightBasis is null
            ? null
            : new PricedHolding(
                holding.Asset.Class, holding.Asset.Country, holding.Currency, holding.BrokerName, weightBasis.Value);
    }

    private static IReadOnlyList<TEntry> Group<TKey, TEntry>(
        IReadOnlyList<PricedHolding> holdings,
        Func<PricedHolding, TKey> selectKey,
        Func<TKey, string> selectLabel,
        Func<TKey, decimal, decimal, TEntry> createEntry)
    {
        var dimensionTotal = holdings.Sum(holding => holding.MarketValue);

        return holdings
            .GroupBy(selectKey)
            .Select(group => (group.Key, MarketValue: group.Sum(holding => holding.MarketValue)))
            .OrderByDescending(group => group.MarketValue)
            .ThenBy(group => selectLabel(group.Key), StringComparer.OrdinalIgnoreCase)
            .Select(group => createEntry(group.Key, group.MarketValue, Percentage(group.MarketValue, dimensionTotal)))
            .ToList();
    }

    private static decimal Percentage(decimal marketValue, decimal dimensionTotal) =>
        dimensionTotal == 0m ? 0m : marketValue / dimensionTotal * 100m;

    private readonly record struct PricedHolding(
        GlobalAssetClass Class, CountryCode Country, Currency Currency, string BrokerName, decimal MarketValue);
}
