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

/// <summary>
/// A whole-portfolio aggregate spans brokers holding different native currencies, so it needs one
/// <see cref="CurrencyConversionContext"/> per distinct source currency rather than the single pair
/// <see cref="ConvertedSummaryBuilder"/> can assume for one broker.
/// </summary>
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
            await AddTransactionsAsync(totals, holding, context).ConfigureAwait(false);
            await AddCreditsAsync(totals, holding, context, yearStart).ConfigureAwait(false);
            await AddDisposalsAsync(totals, holding, context).ConfigureAwait(false);
        }

        await AddTerminalValueAsync(totals, holdings, context, asOf).ConfigureAwait(false);
    }

    private static async Task AddTransactionsAsync(ConvertedTotals totals, PortfolioHolding holding, CurrencyConversionContext context)
    {
        foreach (var transaction in holding.Asset.Transactions)
        {
            var converted = await context.ConvertAsync(transaction.NetCash, DateOnly.FromDateTime(transaction.Date)).ConfigureAwait(false);
            if (converted is not decimal amount)
            {
                continue;
            }

            totals.GrossFlows.Add((transaction.Date, amount));
            totals.NetOfTaxFlows.Add((transaction.Date, amount));

            if (!holding.IsActive)
            {
                continue;
            }

            switch (TransactionTypeEffects.For(transaction.Type).Cash)
            {
                case CashEffect.Out:
                    totals.Bought += -amount;
                    break;
                case CashEffect.In:
                    totals.Sold += amount;
                    break;
            }
        }
    }

    private static async Task AddCreditsAsync(ConvertedTotals totals, PortfolioHolding holding, CurrencyConversionContext context, DateTime yearStart)
    {
        foreach (var credit in holding.Asset.Credits)
        {
            var date = DateOnly.FromDateTime(credit.Date);
            var convertedValue = await context.ConvertAsync(credit.Value, date).ConfigureAwait(false);
            var convertedNetAmount = await context.ConvertAsync(credit.NetAmount, date).ConfigureAwait(false);

            if (convertedValue is decimal grossAmount)
            {
                totals.GrossFlows.Add((credit.Date, grossAmount));
            }

            if (convertedNetAmount is not decimal netAmount)
            {
                continue;
            }

            totals.NetOfTaxFlows.Add((credit.Date, netAmount));
            totals.IncomeLifetime += netAmount;
            if (credit.Date >= yearStart)
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

    /// <summary>
    /// A currency whose source equals the reporting currency attempts nothing yet still produces
    /// every figure, so it keeps the whole portfolio out of the unavailable state.
    /// </summary>
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
