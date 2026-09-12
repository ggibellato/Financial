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

    /// <summary>
    /// The net-of-tax counterpart to <see cref="BuildWithCredits"/>. The two series differ only on
    /// the credit side (<see cref="Credit.NetAmount"/> instead of <see cref="Credit.Value"/>) -
    /// income withholding is where a Gross/Net difference actually arises in this domain; a
    /// Transaction's own (effectively never nonzero) Withheld is already baked into
    /// <see cref="Transaction.NetCash"/> and so is identical in both series. There is no net-of-tax
    /// counterpart to <see cref="BuildWithoutCredits"/> - price-only already excludes all income by
    /// construction, so a "net of tax" variant of a series with no income in it is not a
    /// meaningful second figure.
    /// </summary>
    public static IReadOnlyList<AssetCashFlowDTO> BuildNetOfTaxWithCredits(Asset asset)
    {
        var flows = BuildFromTransactions(asset);

        foreach (var c in asset.Credits)
            flows.Add(new AssetCashFlowDTO { Date = c.Date, Amount = c.NetAmount });

        flows.Sort((a, b) => a.Date.CompareTo(b.Date));
        return flows;
    }

    public static IReadOnlyList<AssetCashFlowDTO> ConcatenateWithCredits(IEnumerable<Asset> assets) =>
        assets.SelectMany(BuildWithCredits).ToList();

    public static IReadOnlyList<AssetCashFlowDTO> ConcatenateWithoutCredits(IEnumerable<Asset> assets) =>
        assets.SelectMany(BuildWithoutCredits).ToList();

    public static IReadOnlyList<AssetCashFlowDTO> ConcatenateNetOfTaxWithCredits(IEnumerable<Asset> assets) =>
        assets.SelectMany(BuildNetOfTaxWithCredits).ToList();

    private static List<AssetCashFlowDTO> BuildFromTransactions(Asset asset)
    {
        var flows = new List<AssetCashFlowDTO>();

        foreach (var t in asset.Transactions)
        {
            flows.Add(new AssetCashFlowDTO { Date = t.Date, Amount = t.NetCash });
        }

        return flows;
    }
}
