using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class AssetTotalsCalculator
{
    public static (decimal TotalBought, decimal TotalSold, decimal TotalCredits) CalculateTotals(Asset asset)
    {
        decimal totalBought = 0, totalSold = 0;
        foreach (var t in asset.Transactions)
        {
            var (bought, sold) = ClassifyNetCash(t.Type, t.NetCash);
            totalBought += bought;
            totalSold += sold;
        }

        var totalCredits = asset.Credits.Sum(c => c.Value);

        return (totalBought, totalSold, totalCredits);
    }

    public static (decimal Bought, decimal Sold) ClassifyNetCash(Transaction.TransactionType type, decimal netCash) =>
        TransactionTypeEffects.For(type).Cash switch
        {
            CashEffect.Out => (-netCash, 0m),
            CashEffect.In => (0m, netCash),
            _ => (0m, 0m)
        };
}
