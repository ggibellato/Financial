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
            if (t.Type == Transaction.TransactionType.Buy)
                totalBought += t.TotalPrice;
            else
                totalSold += t.TotalPrice;
        }

        var totalCredits = asset.Credits.Sum(c => c.Value);

        return (totalBought, totalSold, totalCredits);
    }
}
