using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

internal static class AllocationBreakdownBuilder
{
    internal static AllocationBreakdownDTO Build(
        IReadOnlyList<AllocationHolding> holdings, IHoldingValuationService holdingValuationService)
    {
        var priced = holdings
            .Select(holding => ToPricedHolding(holding, holdingValuationService))
            .Where(holding => holding.HasValue)
            .Select(holding => holding!.Value)
            .ToList();

        return new AllocationBreakdownDTO
        {
            ByClass = Group(
                priced, holding => holding.Class, key => key.ToString(),
                (key, marketValue, percentage) => new AssetClassAllocationEntryDTO(key, marketValue, percentage)),
            ByCurrency = Group(
                priced, holding => holding.Currency, key => key,
                (key, marketValue, percentage) => new CurrencyAllocationEntryDTO(key, marketValue, percentage)),
            ByCountry = Group(
                priced, holding => holding.Country, key => key.ToString(),
                (key, marketValue, percentage) => new CountryAllocationEntryDTO(key, marketValue, percentage)),
            ByBroker = Group(
                priced, holding => holding.BrokerName, key => key,
                (key, marketValue, percentage) => new BrokerAllocationEntryDTO(key, marketValue, percentage))
        };
    }

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
                holding.Asset.Class, holding.Asset.Country, holding.Currency.ToString(), holding.BrokerName, weightBasis.Value);
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
        GlobalAssetClass Class, CountryCode Country, string Currency, string BrokerName, decimal MarketValue);
}
