using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

internal sealed class ConvertedAssetFlows
{
    private readonly List<(DateTime Date, decimal Amount)> _grossFlows = [];
    private readonly List<(DateTime Date, decimal Amount)> _netOfTaxFlows = [];
    private readonly List<(DateTime Date, decimal Amount)> _netCredits = [];

    private ConvertedAssetFlows() { }

    public IReadOnlyList<(DateTime Date, decimal Amount)> GrossFlows => _grossFlows;
    public IReadOnlyList<(DateTime Date, decimal Amount)> NetOfTaxFlows => _netOfTaxFlows;
    public IReadOnlyList<(DateTime Date, decimal Amount)> NetCredits => _netCredits;
    public decimal Bought { get; private set; }
    public decimal Sold { get; private set; }

    public static async Task<ConvertedAssetFlows> ConvertAsync(Asset asset, CurrencyConversionContext context)
    {
        var flows = new ConvertedAssetFlows();
        await flows.AddTransactionsAsync(asset, context).ConfigureAwait(false);
        await flows.AddCreditsAsync(asset, context).ConfigureAwait(false);
        return flows;
    }

    private async Task AddTransactionsAsync(Asset asset, CurrencyConversionContext context)
    {
        foreach (var transaction in asset.Transactions)
        {
            var converted = await context.ConvertAsync(transaction.NetCash, DateOnly.FromDateTime(transaction.Date)).ConfigureAwait(false);
            if (converted is not decimal amount)
            {
                continue;
            }

            _grossFlows.Add((transaction.Date, amount));
            _netOfTaxFlows.Add((transaction.Date, amount));

            var (bought, sold) = AssetTotalsCalculator.ClassifyNetCash(transaction.Type, amount);
            Bought += bought;
            Sold += sold;
        }
    }

    private async Task AddCreditsAsync(Asset asset, CurrencyConversionContext context)
    {
        foreach (var credit in asset.Credits)
        {
            var date = DateOnly.FromDateTime(credit.Date);
            var convertedValue = await context.ConvertAsync(credit.Value, date).ConfigureAwait(false);
            var convertedNetAmount = await context.ConvertAsync(credit.NetAmount, date).ConfigureAwait(false);

            if (convertedValue is decimal grossAmount)
            {
                _grossFlows.Add((credit.Date, grossAmount));
            }

            if (convertedNetAmount is decimal netAmount)
            {
                _netOfTaxFlows.Add((credit.Date, netAmount));
                _netCredits.Add((credit.Date, netAmount));
            }
        }
    }
}
