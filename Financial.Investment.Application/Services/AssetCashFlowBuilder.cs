using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

internal static class AssetCashFlowBuilder
{
    public static IReadOnlyList<AssetCashFlowDTO> BuildWithCredits(Asset asset)
    {
        var flows = BuildFromTransactions(asset);

        foreach (var c in asset.Credits)
            flows.Add(new AssetCashFlowDTO { Date = c.Date, Amount = c.Value });

        flows.Sort((a, b) => a.Date.CompareTo(b.Date));
        return flows;
    }

    public static IReadOnlyList<AssetCashFlowDTO> BuildWithoutCredits(Asset asset)
    {
        var flows = BuildFromTransactions(asset);
        flows.Sort((a, b) => a.Date.CompareTo(b.Date));
        return flows;
    }

    private static List<AssetCashFlowDTO> BuildFromTransactions(Asset asset)
    {
        var flows = new List<AssetCashFlowDTO>();

        foreach (var t in asset.Transactions)
        {
            var amount = t.Type == Transaction.TransactionType.Buy ? -t.TotalPrice : t.TotalPrice;
            flows.Add(new AssetCashFlowDTO { Date = t.Date, Amount = amount });
        }

        return flows;
    }
}
