using Financial.CashFlow.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CashFlowSerializerAdapter : ICashFlowSerializer
{
    public const int CurrentVersion = 2;
    public const string VersionProperty = "Version";

    private const string DataFileName = "data-cashflow.json";

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

        if (VersionRefusal(DataFileName, StoredVersion(root)) is { } refusal)
        {
            throw new InvalidDataException(refusal);
        }

        return root.Deserialize<CashFlowData>(Options)!;
    }

    public static int StoredVersion(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root[VersionProperty]?.GetValue<int>() ?? 1;
    }

    public static string? VersionRefusal(string sourceName, int storedVersion, int currentVersion = CurrentVersion)
    {
        if (storedVersion == currentVersion)
        {
            return null;
        }

        var refusal = $"{sourceName} is version {storedVersion}; this build needs version {currentVersion}.";
        return storedVersion < currentVersion ? $"{refusal} Run the migrate command first." : refusal;
    }
}
