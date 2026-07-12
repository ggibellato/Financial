using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.Infrastructure.Persistence;

public sealed class InvestmentsSerializerAdapter : IInvestmentsSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        TypeInfoResolver = new InvestmentsTypeInfoResolver()
    };

    public string Serialize(Investments investments) =>
        JsonSerializer.Serialize(investments, Options);

    public Investments Deserialize(string json) =>
        JsonSerializer.Deserialize<Investments>(json, Options)!;
}
