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
/// <see cref="Credit.SharesForDividend"/>), or to the entire position held on the credit's date
/// when left unset - never to the asset's current position, which may include shares bought
/// after the dividend's reference date.
/// </summary>
public static class DividendAttributionCalculator
{
    public static DividendAttribution? Calculate(
        Credit credit,
        IEnumerable<Transaction> transactions,
        AssetPriceSnapshot? priceOnDate)
    {
        var openLots = OpenLotTracker.GetOpenLots(transactions.Where(t => t.Date <= credit.Date));
        var quantityHeld = openLots.Sum(lot => lot.RemainingQuantity);
        if (quantityHeld <= 0)
        {
            return null;
        }

        var sharesForDividend = credit.SharesForDividend ?? quantityHeld;

        var averageCostPerShare = openLots.Sum(lot => lot.RemainingQuantity * lot.UnitCost) / quantityHeld;
        var investedAmount = sharesForDividend * averageCostPerShare;

        // ProviderValue/Manual snapshots record the whole position's worth, not a per-share price
        // (see AssetPriceSnapshot.ValidatePrice), so it must be pro-rated to the attributed shares'
        // portion of the position held on the credit date - using it as-is would value the shares
        // that earned this dividend as if they were the entire holding.
        var marketValueOnDate = priceOnDate is null
            ? (decimal?)null
            : priceOnDate.ValuationMethod is ValuationMethod.ProviderValue or ValuationMethod.Manual
                ? priceOnDate.Price * (sharesForDividend / quantityHeld)
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
