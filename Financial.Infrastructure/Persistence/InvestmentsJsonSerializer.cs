using Financial.Common;
using Financial.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.Infrastructure.Persistence;

public static class InvestmentsJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        TypeInfoResolver = new PrivateConstructorContractResolver()
    };

    public static string Serialize(Investments investments) =>
        JsonSerializer.Serialize(investments, Options);

    public static Investments Deserialize(string json)
    {
        var investments = JsonSerializer.Deserialize<Investments>(json, Options);
        return investments!;
    }
}
