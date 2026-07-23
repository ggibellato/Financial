using Financial.CashFlow.Domain.Entities;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Financial.CashFlow.Infrastructure.Persistence;

public class CashFlowTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    private static readonly HashSet<Type> ManagedTypes =
    [
        typeof(CashFlowData),
        typeof(Expense),
        typeof(ReserveMovement),
        typeof(CardStatement),
        typeof(RecurringBill),
        typeof(MaeLedgerEntry),
        typeof(InvestmentSnapshot)
    ];

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        if (!ManagedTypes.Contains(type) || typeInfo.Kind != JsonTypeInfoKind.Object)
            return typeInfo;

        EnablePrivateConstructor(type, typeInfo);
        ConfigureProperties(type, typeInfo);

        return typeInfo;
    }

    private static void EnablePrivateConstructor(Type type, JsonTypeInfo typeInfo)
    {
        if (typeInfo.CreateObject is not null)
            return;

        typeInfo.CreateObject = () =>
            Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException($"Failed to create instance of {type}.");
    }

    private static void ConfigureProperties(Type type, JsonTypeInfo typeInfo)
    {
        foreach (var jsonProp in typeInfo.Properties)
        {
            WirePropertySetter(type, jsonProp);
        }
    }

    private static void WirePropertySetter(Type type, JsonPropertyInfo jsonProp)
    {
        var propInfo = type.GetProperty(
            jsonProp.Name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (propInfo?.SetMethod is null)
            return;

        var setter = propInfo.SetMethod;
        jsonProp.Set = (obj, value) => setter.Invoke(obj, [value]);
    }
}
