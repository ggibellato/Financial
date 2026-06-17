using Financial.Domain.Entities;
using System.Text.Json;

namespace Financial.Infrastructure.Persistence;

public static class InvestmentsJsonSerializer
{
    public static string Serialize(Investments investments) =>
        JsonSerializer.Serialize(investments, InvestmentsSerializerOptions.Default);

    public static Investments Deserialize(string json)
    {
        var investments = JsonSerializer.Deserialize<Investments>(json, InvestmentsSerializerOptions.Default);
        return investments!;
    }
}
