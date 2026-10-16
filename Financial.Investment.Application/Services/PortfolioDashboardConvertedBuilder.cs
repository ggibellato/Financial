using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Services;

internal readonly record struct PortfolioDashboardConversion(
    decimal? MarketValue,
    decimal? Invested,
    decimal? UnrealisedGainLoss,
    decimal? RealisedGainLoss,
    decimal? IncomeYtd,
    decimal? IncomeLifetime,
    decimal? GrossXirr,
    decimal? NetXirr,
    bool IsPartial,
    bool IsUnavailable);

internal static class PortfolioDashboardConvertedBuilder
{
    public static async Task<PortfolioDashboardConversion> BuildAsync(
        IReadOnlyList<PortfolioHolding> holdings,
        Currency reportingCurrency,
        IExchangeRateProvider exchangeRateProvider,
        DateOnly asOf)
    {
        var totals = new ConvertedTotals();
        var contexts = new List<CurrencyConversionContext>();

        foreach (var group in holdings.GroupBy(holding => holding.Currency))
        {
            var context = new CurrencyConversionContext(group.Key, reportingCurrency, exchangeRateProvider);
            contexts.Add(context);
            await AddCurrencyGroupAsync(totals, [.. group], context, asOf).ConfigureAwait(false);
        }

        return BuildResult(totals, contexts);
    }

    private static async Task AddCurrencyGroupAsync(
        ConvertedTotals totals,
        IReadOnlyList<PortfolioHolding> holdings,
        CurrencyConversionContext context,
        DateOnly asOf)
    {
        var yearStart = new DateTime(asOf.Year, 1, 1);

        foreach (var holding in holdings)
        {
            var flows = await ConvertedAssetFlows.ConvertAsync(holding.Asset, context).ConfigureAwait(false);
            AddAssetFlows(totals, flows, holding.IsActive, yearStart);
            await AddDisposalsAsync(totals, holding, context).ConfigureAwait(false);
        }

        await AddTerminalValueAsync(totals, holdings, context, asOf).ConfigureAwait(false);
    }

    private static void AddAssetFlows(ConvertedTotals totals, ConvertedAssetFlows flows, bool countsTowardsInvested, DateTime yearStart)
    {
        totals.GrossFlows.AddRange(flows.GrossFlows);
        totals.NetOfTaxFlows.AddRange(flows.NetOfTaxFlows);

        if (countsTowardsInvested)
        {
            totals.Bought += flows.Bought;
            totals.Sold += flows.Sold;
        }

        foreach (var (date, netAmount) in flows.NetCredits)
        {
            totals.IncomeLifetime += netAmount;
            if (date >= yearStart)
            {
                totals.IncomeYtd += netAmount;
            }
        }
    }

    private static async Task AddDisposalsAsync(ConvertedTotals totals, PortfolioHolding holding, CurrencyConversionContext context)
    {
        foreach (var disposal in holding.Asset.DisposalRecords.Where(record => record.Status == DisposalRecordStatus.Active))
        {
            var converted = await context.ConvertAsync(disposal.GainLoss, DateOnly.FromDateTime(disposal.Date)).ConfigureAwait(false);
            if (converted is decimal amount)
            {
                totals.RealisedGainLoss += amount;
            }
        }
    }

    private static async Task AddTerminalValueAsync(
        ConvertedTotals totals,
        IReadOnlyList<PortfolioHolding> holdings,
        CurrencyConversionContext context,
        DateOnly asOf)
    {
        var priced = holdings.Where(holding => holding.HasTerminalValue).ToList();
        if (priced.Count == 0)
        {
            return;
        }

        var convertedMarketValue = await context.ConvertAsync(priced.Sum(holding => holding.MarketValue!.Value), asOf).ConfigureAwait(false);
        var convertedUnrealisedGain = await context.ConvertAsync(priced.Sum(holding => holding.UnrealisedGain ?? 0m), asOf).ConfigureAwait(false);

        if (convertedMarketValue is decimal marketValue)
        {
            totals.MarketValue += marketValue;
            totals.GrossFlows.Add((asOf.ToDateTime(TimeOnly.MinValue), marketValue));
            totals.NetOfTaxFlows.Add((asOf.ToDateTime(TimeOnly.MinValue), marketValue));
        }

        if (convertedUnrealisedGain is decimal unrealisedGain)
        {
            totals.UnrealisedGainLoss += unrealisedGain;
        }
    }

    private static PortfolioDashboardConversion BuildResult(ConvertedTotals totals, IReadOnlyList<CurrencyConversionContext> contexts)
    {
        if (contexts.Any(context => context.AttemptCount > 0) && contexts.All(context => context.IsUnavailable))
        {
            return new PortfolioDashboardConversion(
                null, null, null, null, null, null, null, null, IsPartial: false, IsUnavailable: true);
        }

        return new PortfolioDashboardConversion(
            totals.MarketValue,
            totals.Bought - totals.Sold,
            totals.UnrealisedGainLoss,
            totals.RealisedGainLoss,
            totals.IncomeYtd,
            totals.IncomeLifetime,
            XirrCalculator.Calculate(totals.GrossFlows),
            XirrCalculator.Calculate(totals.NetOfTaxFlows),
            IsPartial: contexts.Any(context => context.FailureCount > 0),
            IsUnavailable: false);
    }

    private sealed class ConvertedTotals
    {
        public decimal MarketValue { get; set; }
        public decimal Bought { get; set; }
        public decimal Sold { get; set; }
        public decimal UnrealisedGainLoss { get; set; }
        public decimal RealisedGainLoss { get; set; }
        public decimal IncomeYtd { get; set; }
        public decimal IncomeLifetime { get; set; }
        public List<(DateTime Date, decimal Amount)> GrossFlows { get; } = [];
        public List<(DateTime Date, decimal Amount)> NetOfTaxFlows { get; } = [];
    }
}
