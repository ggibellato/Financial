using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.Infrastructure.Persistence;

public static class InvestmentsSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        TypeInfoResolver = new InvestmentsTypeInfoResolver()
    };
}
