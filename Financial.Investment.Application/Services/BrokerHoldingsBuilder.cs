using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

internal sealed record HoldingMonthStarts(MonthStartValue Last, MonthStartValue This);

internal sealed record HoldingsGroupSource(
    string Name,
    IReadOnlyList<PortfolioAssetSummaryItemDTO> Holdings,
    IReadOnlyList<HoldingMonthStarts> MonthStarts);

internal static class BrokerHoldingsBuilder
{
    internal static BrokerHoldingsDTO Build(
        IReadOnlyList<string> currencies,
        DateOnly lastMonthStart,
        DateOnly thisMonthStart,
        IEnumerable<HoldingsGroupSource> groups)
    {
        var nonEmptyGroups = groups
            .Where(g => g.Holdings.Count > 0)
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var shareBase = ShareBase.Of(nonEmptyGroups.SelectMany(g => g.Holdings).ToList());
        var builtGroups = nonEmptyGroups.Select(g => BuildGroup(g, shareBase)).ToList();

        return new BrokerHoldingsDTO
        {
            Currencies = currencies,
            LastMonthStart = MonthStartColumn(lastMonthStart),
            ThisMonthStart = MonthStartColumn(thisMonthStart),
            Groups = builtGroups,
            Total = BuildTotals(builtGroups.SelectMany(g => g.Holdings).ToList(), shareBase, shareBase),
        };
    }

    private static MonthStartColumnDTO MonthStartColumn(DateOnly monthStart) => new()
    {
        Date = monthStart,
        EarliestPriceDate = MonthStartValueCalculator.EarliestPriceDate(monthStart),
    };

    private static MonthStartValueDTO ToDto(MonthStartValue value) => new()
    {
        Value = value.Value,
        UnitPrice = value.UnitPrice,
        PriceDate = value.PriceDate,
        Currency = value.Currency,
    };

    private static HoldingsGroupDTO BuildGroup(HoldingsGroupSource group, ShareBase brokerBase)
    {
        var groupBase = ShareBase.Of(group.Holdings);
        var hasSiblings = group.Holdings.Count > 1;
        var rows = group.Holdings
            .Select((h, index) =>
            {
                var monthStarts = group.MonthStarts[index];
                return new HoldingRowDTO
                {
                    Asset = h,
                    InvestedShareOfGroup = hasSiblings ? Share(h.TotalInvested, groupBase.Invested) : null,
                    MarketShareOfGroup = hasSiblings ? Share(h.MarketValue, groupBase.MarketValue) : null,
                    InvestedShareOfBase = Share(h.TotalInvested, brokerBase.Invested),
                    MarketShareOfBase = Share(h.MarketValue, brokerBase.MarketValue),
                    LastMonthStart = ToDto(monthStarts.Last),
                    ThisMonthStart = ToDto(monthStarts.This),
                    MonthChangePercent = MonthStartValueCalculator.ChangePercent(monthStarts.Last.Value, monthStarts.This.Value),
                };
            })
            .ToList();

        return new HoldingsGroupDTO
        {
            Name = group.Name,
            Holdings = rows,
            Segments = BuildSegments(rows, groupBase, brokerBase),
            Subtotal = BuildTotals(rows, groupBase, brokerBase),
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
                var segmentRows = segment.ToList();
                var holdings = segmentRows.Select(r => r.Asset).ToList();
                var segmentBase = ShareBase.Of(holdings);
                return new HoldingsSegmentDTO
                {
                    Name = segment.Key is null ? null : holdings.MinBy(h => h.AssetName, StringComparer.CurrentCultureIgnoreCase)!.Segment,
                    Holdings = segmentRows,
                    Subtotal = BuildTotals(segmentRows, segmentBase, brokerBase),
                    InvestedShareOfGroup = Share(segmentBase.Invested, groupBase.Invested),
                    MarketShareOfGroup = Share(segmentBase.MarketValue, groupBase.MarketValue),
                };
            })
            .OrderBy(s => s.Name is null)
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static HoldingsTotalsDTO BuildTotals(IReadOnlyList<HoldingRowDTO> rows, ShareBase own, ShareBase brokerBase)
    {
        var holdings = rows.Select(r => r.Asset).ToList();
        var withBothMonthStarts = rows.Where(r => r.LastMonthStart.Value.HasValue && r.ThisMonthStart.Value.HasValue).ToList();
        decimal? lastMonthStart = withBothMonthStarts.Count == 0 ? null : withBothMonthStarts.Sum(r => r.LastMonthStart.Value!.Value);
        decimal? thisMonthStart = withBothMonthStarts.Count == 0 ? null : withBothMonthStarts.Sum(r => r.ThisMonthStart.Value!.Value);
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
            LastMonthStartValue = lastMonthStart,
            ThisMonthStartValue = thisMonthStart,
            MonthChangePercent = MonthStartValueCalculator.ChangePercent(lastMonthStart, thisMonthStart),
            MonthStartExcludedCount = rows.Count - withBothMonthStarts.Count,
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
