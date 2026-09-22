using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.Investment.Infrastructure.Persistence;

public sealed class InvestmentSerializerAdapter : IInvestmentSerializer
{
    private const string VersionProperty = "Version";

    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        TypeInfoResolver = new InvestmentTypeInfoResolver()
    };

    public string Serialize(Investments investments)
    {
        var root = JsonSerializer.SerializeToNode(investments, Options)!.AsObject();
        root[VersionProperty] = InvestmentDataMigrations.CurrentVersion;
        return root.ToJsonString(Options);
    }

    public Investments Deserialize(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        var storedVersion = (int?)root[VersionProperty] ?? 1;
        InvestmentDataMigrations.Apply(root, storedVersion);
        var investments = root.Deserialize<Investments>(Options)!;

        if (storedVersion < 5)
        {
            foreach (var failure in SharesForDividendBackfill.Apply(investments))
            {
                Trace.TraceWarning($"SharesForDividend backfill skipped credit {failure.CreditId}: {failure.Message}");
            }
        }

        return investments;
    }
}
