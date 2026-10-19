using Financial.CashFlow.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CashFlowSerializerAdapter : ICashFlowSerializer
{
    public const int CurrentVersion = 2;

    private const string VersionProperty = "Version";

    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(), new CashFlowDataConverter() },
        WriteIndented = false,
        TypeInfoResolver = new CashFlowTypeInfoResolver()
    };

    public string Serialize(CashFlowData data)
    {
        var root = JsonSerializer.SerializeToNode(data, Options)!.AsObject();
        root[VersionProperty] = CurrentVersion;
        return root.ToJsonString(Options);
    }

    public CashFlowData Deserialize(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        EnsureCurrentVersion(StoredVersion(root));
        return root.Deserialize<CashFlowData>(Options)!;
    }

    public static int StoredVersion(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root[VersionProperty]?.GetValue<int>() ?? 1;
    }

    private static void EnsureCurrentVersion(int storedVersion)
    {
        if (storedVersion < CurrentVersion)
        {
            throw new InvalidDataException(
                $"data-cashflow.json is version {storedVersion}; this build needs version {CurrentVersion}. Run the migrate command first.");
        }

        if (storedVersion > CurrentVersion)
        {
            throw new InvalidDataException(
                $"data-cashflow.json is version {storedVersion}; this build needs version {CurrentVersion}.");
        }
    }
}
