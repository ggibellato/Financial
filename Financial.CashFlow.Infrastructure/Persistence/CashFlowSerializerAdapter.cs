using Financial.CashFlow.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

/// <summary>
/// <see cref="CurrentVersion"/> has no predecessor to migrate from yet - it exists so the first
/// future schema change has a stored <c>Version</c> to branch on instead of needing to add one
/// retroactively. A document with no <c>Version</c> property (every file written before this) is
/// treated as version 1, matching <see cref="CurrentVersion"/>, so nothing is rewritten on load.
/// </summary>
public sealed class CashFlowSerializerAdapter : ICashFlowSerializer
{
    private const string VersionProperty = "Version";
    private const int CurrentVersion = 1;

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
        return root.Deserialize<CashFlowData>(Options)!;
    }
}
