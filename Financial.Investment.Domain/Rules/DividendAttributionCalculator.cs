using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record DividendAttribution(
    decimal SharesForDividend,
    decimal AverageCostPerShare,
    decimal InvestedAmount,
    decimal? PriceOnDate,
    decimal? MarketValueOnDate,
    decimal? YieldOnInvested,
    decimal? YieldOnMarket);

/// <summary>
/// Attributes a dividend to the shares it was actually earned on (as declared via
/// <see cref="Credit.SharesForDividend"/>), rather than the asset's current position, which may
/// include shares bought after the dividend's reference date.
/// </summary>
public static class DividendAttributionCalculator
{
    public static DividendAttribution? Calculate(
        Credit credit,
        IEnumerable<Transaction> transactions,
        AssetPriceSnapshot? priceOnDate)
    {
        if (credit.SharesForDividend is not decimal sharesForDividend)
        {
            return null;
        }

        var openLots = OpenLotTracker.GetOpenLots(transactions.Where(t => t.Date <= credit.Date));
        var quantityHeld = openLots.Sum(lot => lot.RemainingQuantity);
        if (quantityHeld <= 0)
        {
            return null;
        }

        var averageCostPerShare = openLots.Sum(lot => lot.RemainingQuantity * lot.UnitCost) / quantityHeld;
        var investedAmount = sharesForDividend * averageCostPerShare;

        var marketValueOnDate = priceOnDate is null
            ? (decimal?)null
            : priceOnDate.ValuationMethod is ValuationMethod.ProviderValue or ValuationMethod.Manual
                ? priceOnDate.Price
                : sharesForDividend * priceOnDate.Price;

        return new DividendAttribution(
            sharesForDividend,
            averageCostPerShare,
            investedAmount,
            priceOnDate?.Price,
            marketValueOnDate,
            investedAmount > 0 ? credit.Value / investedAmount * 100m : null,
            marketValueOnDate is > 0 ? credit.Value / marketValueOnDate.Value * 100m : null);
    }
}
