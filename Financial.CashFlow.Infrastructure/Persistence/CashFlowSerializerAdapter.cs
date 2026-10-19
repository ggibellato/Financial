using Financial.CashFlow.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CashFlowSerializerAdapter : ICashFlowSerializer
{
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
        CashFlowDataFileVersion.Stamp(root, CashFlowDataFileVersion.Current);
        return root.ToJsonString(Options);
    }

    public CashFlowData Deserialize(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();

        if (CashFlowDataFileVersion.Refusal(DataFileName, CashFlowDataFileVersion.StoredIn(root)) is { } refusal)
        {
            throw new InvalidDataException(refusal);
        }

        return root.Deserialize<CashFlowData>(Options)!;
    }
}
