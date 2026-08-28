using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Financial.Investment.Infrastructure.Persistence;

public class InvestmentTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    private static readonly HashSet<Type> ManagedTypes =
    [
        typeof(Investments),
        typeof(Broker),
        typeof(Portfolio),
        typeof(Asset),
        typeof(Transaction),
        typeof(Credit),
        typeof(AssetPriceSnapshot)
    ];

    private static readonly HashSet<(Type, string)> ExcludedProperties =
    [
        (typeof(Asset), nameof(Asset.AveragePrice)),
        (typeof(Asset), nameof(Asset.Quantity)),
        (typeof(Asset), nameof(Asset.AverageSellPrice)),
        (typeof(Asset), nameof(Asset.RealizedGainLoss)),
        (typeof(Transaction), nameof(Transaction.TotalPrice))
    ];

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        if (!ManagedTypes.Contains(type) || typeInfo.Kind != JsonTypeInfoKind.Object)
            return typeInfo;

        ReflectionJsonTypeInfoHelpers.EnablePrivateConstructor(type, typeInfo);
        ConfigureProperties(type, typeInfo);

        return typeInfo;
    }

    private static void ConfigureProperties(Type type, JsonTypeInfo typeInfo)
    {
        var toRemove = new List<JsonPropertyInfo>();

        foreach (var jsonProp in typeInfo.Properties)
        {
            if (ExcludedProperties.Contains((type, jsonProp.Name)))
            {
                toRemove.Add(jsonProp);
                continue;
            }

            ReflectionJsonTypeInfoHelpers.WirePropertySetter(type, jsonProp);
        }

        foreach (var prop in toRemove)
            typeInfo.Properties.Remove(prop);
    }
}
