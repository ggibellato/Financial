using Financial.Investment.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.Investment.Infrastructure.Persistence;

public sealed class InvestmentSerializerAdapter : IInvestmentSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        TypeInfoResolver = new InvestmentTypeInfoResolver()
    };

    public string Serialize(Investments investments) =>
        JsonSerializer.Serialize(investments, Options);

    public Investments Deserialize(string json) =>
        JsonSerializer.Deserialize<Investments>(json, Options)!;
}
