using System.Text.Json.Nodes;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration.DataFileMigrationJson;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public sealed class V3ToV4PotsAccounts : IDataFileMigrationStep
{
    private const string ReservaName = "Reserva";

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

        var accountArray = root["Accounts"] as JsonArray ?? [];
        accountArray.Add(NewReservaAccount(buckets, categoryId));
        root["Accounts"] = accountArray;
        root.Remove("ReserveBuckets");

        return $"{ReservaName} Pots account created with {buckets.Count} pots; category {ReservaName}";
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
