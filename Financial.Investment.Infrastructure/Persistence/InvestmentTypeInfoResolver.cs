using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Financial.Investment.Infrastructure.Persistence;

public class InvestmentTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    internal static readonly IReadOnlySet<Type> ManagedTypes = new HashSet<Type>
    {
        typeof(Investments),
        typeof(Broker),
        typeof(Portfolio),
        typeof(Asset),
        typeof(Transaction),
        typeof(Credit),
        typeof(AssetPriceSnapshot),
        typeof(FxRateSnapshot),
        typeof(RealisedGain),
        typeof(TaxRule),
        typeof(TaxClassification),
        typeof(CorporateAction)
    };

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        if (!ManagedTypes.Contains(type) || typeInfo.Kind != JsonTypeInfoKind.Object)
            return typeInfo;

        ReflectionJsonTypeInfoHelpers.EnablePrivateConstructor(type, typeInfo);
        ReflectionJsonTypeInfoHelpers.RemoveSetterlessProperties(typeInfo);

        foreach (var jsonProp in typeInfo.Properties)
            ReflectionJsonTypeInfoHelpers.WirePropertySetter(type, jsonProp);

        return typeInfo;
    }
}
