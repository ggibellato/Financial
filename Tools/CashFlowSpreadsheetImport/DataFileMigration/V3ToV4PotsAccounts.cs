using System.Text.Json.Nodes;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration.DataFileMigrationJson;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public sealed class V3ToV4PotsAccounts : IDataFileMigrationStep
{
    private const string ReservaName = "Reserva";
    private const string LegacyPotsSource = "ReserveBucketsSum";
    private const string PotsSource = "PotsAccountTotal";

    public int FromVersion => 3;

    public string Name => nameof(V3ToV4PotsAccounts);

    public string Apply(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var accounts = Objects(root, "Accounts");
        EnsureNoPotsAccount(accounts);
        EnsureNoAccountNamedReserva(accounts);

        var buckets = Objects(root, "ReserveBuckets");
        EnsureMovementBucketsExist(root, buckets);
        var categoryId = ReservaCategoryId(root);

        foreach (var account in accounts)
        {
            account["Pots"] = null;
            account["CategoryId"] = null;
        }

        var reserva = NewReservaAccount(buckets, categoryId);
        var accountArray = root["Accounts"] as JsonArray ?? [];
        accountArray.Add(reserva);
        root["Accounts"] = accountArray;
        root.Remove("ReserveBuckets");

        var reservaId = reserva["Id"]!.GetValue<string>();
        var snapshotSources = PointSnapshotSourcesAt(root, reservaId);
        var incomeSources = PointSplitsAt(root, "IncomeSources", "AutoSplitToReserve", reservaId);
        var incomes = PointSplitsAt(root, "Incomes", "SplitToReserve", reservaId);

        return $"{ReservaName} Pots account created with {buckets.Count} pots; category {ReservaName}; {snapshotSources} snapshot sources, {incomeSources} income sources and {incomes} incomes point at it";
    }

    private static int PointSplitsAt(JsonObject root, string collection, string splitFlag, string reservaId)
    {
        var count = 0;
        foreach (var item in Objects(root, collection))
        {
            var splitsToReserve = item[splitFlag]?.GetValue<bool>() == true;
            if (splitsToReserve)
            {
                count++;
            }
            item.Remove(splitFlag);
            item["PotsAccountId"] = splitsToReserve ? reservaId : null;
        }
        return count;
    }

    private static int PointSnapshotSourcesAt(JsonObject root, string reservaId)
    {
        var count = 0;
        foreach (var investmentAccount in Objects(root, "InvestmentAccounts"))
        {
            var isPotsSourced = investmentAccount["Source"]?.GetValue<string>() == LegacyPotsSource;
            if (isPotsSourced)
            {
                investmentAccount["Source"] = PotsSource;
                count++;
            }
            investmentAccount["PotsAccountId"] = isPotsSourced ? reservaId : null;
        }
        return count;
    }

    private static string ReservaCategoryId(JsonObject root)
    {
        var category = Objects(root, "Categories")
            .FirstOrDefault(category => string.Equals(category["Name"]?.GetValue<string>(), ReservaName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"No category named \"{ReservaName}\" to assign to the {ReservaName} Pots account.");

        return IdText(category["Id"], "Category");
    }

    private static void EnsureNoPotsAccount(IEnumerable<JsonObject> accounts)
    {
        var potsAccount = accounts.FirstOrDefault(account => account["Template"]?.GetValue<string>() == "Pots");
        if (potsAccount is not null)
        {
            throw new InvalidDataException(
                $"'{potsAccount["Name"]?.GetValue<string>()}' is a Pots account; Pots accounts start in V4. Remove it and migrate again.");
        }
    }

    private static void EnsureNoAccountNamedReserva(IEnumerable<JsonObject> accounts)
    {
        if (accounts.Any(account => string.Equals(account["Name"]?.GetValue<string>(), ReservaName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException($"An account named \"{ReservaName}\" already exists.");
        }
    }

    private static void EnsureMovementBucketsExist(JsonObject root, IEnumerable<JsonObject> buckets)
    {
        var bucketIds = buckets.Select(bucket => IdText(bucket["Id"], "ReserveBucket")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var movement in Objects(root, "ReserveMovements"))
        {
            var bucketId = IdText(movement["BucketId"], $"ReserveMovement {movement["Id"]} BucketId");
            if (!bucketIds.Contains(bucketId))
            {
                throw new InvalidDataException($"ReserveMovement {movement["Id"]} references unknown bucket {bucketId}");
            }
        }
    }

    private static JsonObject NewReservaAccount(IEnumerable<JsonObject> buckets, string categoryId) => new()
    {
        ["Id"] = Guid.NewGuid().ToString(),
        ["Name"] = ReservaName,
        ["Template"] = "Pots",
        ["Currency"] = "GBP",
        ["IsActive"] = true,
        ["OpeningBalance"] = null,
        ["RoundUp"] = null,
        ["Pots"] = new JsonArray(buckets.Select(bucket => (JsonNode)bucket.DeepClone()).ToArray()),
        ["CategoryId"] = categoryId
    };
}
