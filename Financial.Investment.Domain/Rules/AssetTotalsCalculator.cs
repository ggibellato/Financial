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
            switch (TransactionTypeEffects.For(t.Type).Cash)
            {
                case CashEffect.Out:
                    totalBought += -t.NetCash;
                    break;
                case CashEffect.In:
                    totalSold += t.NetCash;
                    break;
            }
        }

        var totalCredits = asset.Credits.Sum(c => c.Value);

        return (totalBought, totalSold, totalCredits);
    }
}
