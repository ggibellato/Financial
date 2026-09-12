using System.Text.Json;
using System.Text.Json.Nodes;

namespace Financial.Investment.TransactionIncomeVocabularyMigration;

/// <summary>
/// One-time raw-JSON rewrite of every <c>Credit.Type</c> string <c>"Rent"</c> to
/// <c>"SecuritiesLendingIncome"</c> (spec.md FR-013). This must run - and be verified against a
/// temp copy first - before the corresponding C# enum rename ships to production data, because
/// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/> throws on an unrecognized
/// stored string: without this migration, every existing "Rent" row fails to deserialize at the
/// next process start.
/// <para>
/// Operates on the raw JSON tree (<see cref="JsonNode"/>) rather than the typed
/// <c>Investments</c> model, for the same reason CashFlow's raw-JSON migrators do: deserializing
/// through the normal typed path would already fail on the very strings this migration exists to
/// fix.
/// </para>
/// </summary>
public static class TransactionIncomeVocabularyMigrator
{
    private const string LegacyRentType = "Rent";
    private const string SecuritiesLendingIncomeType = "SecuritiesLendingIncome";
    private static readonly string[] BrokerGroupNames = ["ActiveBrokers", "HistoricBrokers"];

    public static TransactionIncomeVocabularyMigrationSummary Migrate(string dataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);

        MigrationBackup.Create(dataPath);

        var root = JsonNode.Parse(File.ReadAllText(dataPath))?.AsObject()
            ?? throw new InvalidOperationException($"'{dataPath}' does not contain a JSON object at its root.");
        var summary = new TransactionIncomeVocabularyMigrationSummary();

        foreach (var brokerGroupName in BrokerGroupNames)
        {
            foreach (var credit in EnumerateCredits(root, brokerGroupName))
            {
                if (credit["Type"]?.GetValue<string>() == LegacyRentType)
                {
                    credit["Type"] = SecuritiesLendingIncomeType;
                    summary.CountRewritten();
                }
            }
        }

        File.WriteAllText(dataPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return summary;
    }

    private static IEnumerable<JsonObject> EnumerateCredits(JsonObject root, string brokerGroupName)
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
        }
    }
}
