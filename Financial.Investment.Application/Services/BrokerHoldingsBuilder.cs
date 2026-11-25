using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

internal sealed record HoldingsGroupSource(string Name, IReadOnlyList<PortfolioAssetSummaryItemDTO> Holdings);

internal static class BrokerHoldingsBuilder
{
    internal static BrokerHoldingsDTO Build(IReadOnlyList<string> currencies, IEnumerable<HoldingsGroupSource> groups)
    {
        var nonEmptyGroups = groups
            .Where(g => g.Holdings.Count > 0)
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var allHoldings = nonEmptyGroups.SelectMany(g => g.Holdings).ToList();
        var shareBase = ShareBase.Of(allHoldings);

        return new BrokerHoldingsDTO
        {
            Currencies = currencies,
            Groups = nonEmptyGroups.Select(g => BuildGroup(g, shareBase)).ToList(),
            Total = BuildTotals(allHoldings, shareBase, shareBase),
        };
    }

    private static HoldingsGroupDTO BuildGroup(HoldingsGroupSource group, ShareBase brokerBase)
    {
        var groupBase = ShareBase.Of(group.Holdings);
        var hasSiblings = group.Holdings.Count > 1;
        var rows = group.Holdings
            .Select(h => new HoldingRowDTO
            {
                Asset = h,
                InvestedShareOfGroup = hasSiblings ? Share(h.TotalInvested, groupBase.Invested) : null,
                MarketShareOfGroup = hasSiblings ? Share(h.MarketValue, groupBase.MarketValue) : null,
                InvestedShareOfBase = Share(h.TotalInvested, brokerBase.Invested),
                MarketShareOfBase = Share(h.MarketValue, brokerBase.MarketValue),
            })
            .ToList();

        return new HoldingsGroupDTO
        {
            Name = group.Name,
            Holdings = rows,
            Segments = BuildSegments(rows, groupBase, brokerBase),
            Subtotal = BuildTotals(group.Holdings, groupBase, brokerBase),
        };
    }

    private static IReadOnlyList<HoldingsSegmentDTO> BuildSegments(IReadOnlyList<HoldingRowDTO> rows, ShareBase groupBase, ShareBase brokerBase)
    {
        if (rows.All(r => r.Asset.Segment is null))
        {
            return [];
        }

        return rows
            .GroupBy(r => r.Asset.Segment, StringComparer.CurrentCultureIgnoreCase)
            .Select(segment =>
            {
                var holdings = segment.Select(r => r.Asset).ToList();
                var segmentBase = ShareBase.Of(holdings);
                return new HoldingsSegmentDTO
                {
                    Name = segment.Key is null ? null : holdings.MinBy(h => h.AssetName, StringComparer.CurrentCultureIgnoreCase)!.Segment,
                    Holdings = segment.ToList(),
                    Subtotal = BuildTotals(holdings, segmentBase, brokerBase),
                    InvestedShareOfGroup = Share(segmentBase.Invested, groupBase.Invested),
                    MarketShareOfGroup = Share(segmentBase.MarketValue, groupBase.MarketValue),
                };
            })
            .OrderBy(s => s.Name is null)
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static HoldingsTotalsDTO BuildTotals(IReadOnlyList<PortfolioAssetSummaryItemDTO> holdings, ShareBase own, ShareBase brokerBase)
    {
        var valued = holdings.Where(h => h.MarketValue.HasValue).ToList();
        var valuedInvested = valued.Sum(h => h.TotalInvested);
        var profit = own.MarketValue - valuedInvested;
        var result = PositionResultCalculator.ForOpenPosition(profit, valuedInvested, valued.Sum(h => h.TotalCredits));
        var totalCredits = holdings.Sum(h => h.TotalCredits);
        var lastCredits = holdings.Sum(h => h.LastMonthCredits);
        var everyLastCreditIsValued = valued.Sum(h => h.LastMonthCredits) == lastCredits;

        return new HoldingsTotalsDTO
        {
            HoldingCount = holdings.Count,
            UnvaluedHoldingCount = holdings.Count - valued.Count,
            Invested = own.Invested,
            MarketValue = own.MarketValue,
            InvestedShareOfBase = Share(own.Invested, brokerBase.Invested),
            MarketShareOfBase = Share(own.MarketValue, brokerBase.MarketValue),
            ProfitAmount = profit,
            ProfitPercent = result.ResultPercent,
            ProfitWithCreditsPercent = result.ResultWithCreditsPercent,
            TotalCredits = totalCredits,
            CreditsPercentOfInvested = Share(totalCredits, own.Invested),
            LastCreditAmount = lastCredits,
            LastCreditPercentOfMarketValue = everyLastCreditIsValued ? Share(lastCredits, own.MarketValue) : null,
        };
    }

    private static decimal? Share(decimal? part, decimal? whole) =>
        part is null || whole is null or 0m ? null : part / whole * 100m;

    private sealed record ShareBase(decimal Invested, decimal? MarketValue)
    {
        internal static ShareBase Of(IReadOnlyList<PortfolioAssetSummaryItemDTO> holdings)
        {
            var valued = holdings.Where(h => h.MarketValue.HasValue).ToList();
            return new ShareBase(
                holdings.Sum(h => h.TotalInvested),
                valued.Count == 0 ? null : valued.Sum(h => h.MarketValue!.Value));
        }
    }
}
