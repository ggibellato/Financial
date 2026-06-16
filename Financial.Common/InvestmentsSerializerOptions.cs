using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.Common;

public static class InvestmentsSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        TypeInfoResolver = new PrivateConstructorContractResolver()
    };
}
