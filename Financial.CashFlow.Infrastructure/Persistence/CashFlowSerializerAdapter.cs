using Financial.CashFlow.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CashFlowSerializerAdapter : ICashFlowSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        TypeInfoResolver = new CashFlowTypeInfoResolver()
    };

    public string Serialize(CashFlowData data) =>
        JsonSerializer.Serialize(data, Options);

    public CashFlowData Deserialize(string json) =>
        JsonSerializer.Deserialize<CashFlowData>(json, Options)!;
}
