using System.Text.Json.Nodes;

namespace Financial.Investment.Infrastructure.Persistence;

/// <summary>
/// On-the-fly upgrades applied to the raw JSON tree between the document's stored <c>Version</c>
/// and <see cref="CurrentVersion"/>, run by <see cref="InvestmentSerializerAdapter"/> on every load
/// so an older data file never needs a separate migration tool run before the app can read it. A
/// document with no <c>Version</c> property is treated as version 1.
/// </summary>
internal static class InvestmentDataMigrations
{
    public const int CurrentVersion = 3;

    private static readonly string[] BrokerGroupNames = ["ActiveBrokers", "HistoricBrokers"];

    public static void Apply(JsonObject root, int fromVersion)
    {
        if (fromVersion < 2)
        {
            RenameCreditType(root, "Rent", "SecuritiesLendingIncome");
        }

        if (fromVersion < 3)
        {
            RenamePriceHistoryToPriceSnapshots(root);
        }
    }

    private static void RenameCreditType(JsonObject root, string from, string to)
    {
        foreach (var brokerGroupName in BrokerGroupNames)
        {
            foreach (var credit in EnumerateCredits(root, brokerGroupName))
            {
                if (credit["Type"]?.GetValue<string>() == from)
                {
                    credit["Type"] = to;
                }
            }
        }
    }

    /// <summary>
    /// Renames the "PriceHistory" collection to "PriceSnapshots" (matching the Domain-internal
    /// <c>Asset.PriceHistory</c>→<c>PriceSnapshots</c> rename), and maps each existing entry's
    /// "IsManual" bool onto the new "Source" string — "Manual" or "Unknown" (an honest "some
    /// automatic provider, which one is lost to history", never conflated with "no data").
    /// </summary>
    private static void RenamePriceHistoryToPriceSnapshots(JsonObject root)
    {
        foreach (var brokerGroupName in BrokerGroupNames)
        {
            foreach (var asset in EnumerateAssets(root, brokerGroupName))
            {
                if (!asset.TryGetPropertyValue("PriceHistory", out var priceHistory) || priceHistory is not JsonArray snapshots)
                {
                    continue;
                }

                foreach (var snapshot in snapshots.OfType<JsonObject>())
                {
                    var wasManual = snapshot.TryGetPropertyValue("IsManual", out var isManualNode) && isManualNode?.GetValue<bool>() == true;
                    snapshot.Remove("IsManual");
                    snapshot["Source"] = wasManual ? "Manual" : "Unknown";
                }

                asset.Remove("PriceHistory");
                asset["PriceSnapshots"] = priceHistory;
            }
        }
    }

    private static IEnumerable<JsonObject> EnumerateCredits(JsonObject root, string brokerGroupName)
    {
        foreach (var asset in EnumerateAssets(root, brokerGroupName))
        {
            if (asset["Credits"] is not JsonArray credits)
            {
                continue;
            }

            foreach (var credit in credits.OfType<JsonObject>())
            {
                yield return credit;
            }
        }
    }

    private static IEnumerable<JsonObject> EnumerateAssets(JsonObject root, string brokerGroupName)
    {
        if (root[brokerGroupName] is not JsonArray brokers)
        {
            yield break;
        }

        foreach (var broker in brokers.OfType<JsonObject>())
        {
            if (broker["Portfolios"] is not JsonArray portfolios)
            {
                continue;
            }

            foreach (var portfolio in portfolios.OfType<JsonObject>())
            {
                if (portfolio["Assets"] is not JsonArray assets)
                {
                    continue;
                }

                foreach (var asset in assets.OfType<JsonObject>())
                {
                    yield return asset;
                }
            }
        }
    }
}
