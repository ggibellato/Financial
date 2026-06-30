using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using System.Text.Json;

namespace Financial.Infrastructure.Persistence;

public sealed class InvestmentsSerializerAdapter : IInvestmentsSerializer
{
    public string Serialize(Investments investments) =>
        JsonSerializer.Serialize(investments, InvestmentsSerializerOptions.Default);

    public Investments Deserialize(string json) =>
        JsonSerializer.Deserialize<Investments>(json, InvestmentsSerializerOptions.Default)!;
}
