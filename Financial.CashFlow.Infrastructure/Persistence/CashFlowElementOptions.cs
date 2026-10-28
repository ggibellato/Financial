using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public static class CashFlowElementOptions
{
    public static JsonSerializerOptions Create(ReferenceResolutionContext? context = null) => new()
    {
        Converters = { new JsonStringEnumConverter(), new AccountJsonConverter(context) },
        TypeInfoResolver = new CashFlowTypeInfoResolver(context)
    };
}
