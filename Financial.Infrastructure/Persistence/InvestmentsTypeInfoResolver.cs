using Financial.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Financial.Infrastructure.Persistence;

public class InvestmentsTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    private static readonly HashSet<Type> ManagedTypes =
    [
        typeof(Investments),
        typeof(Broker),
        typeof(Portfolio),
        typeof(Asset),
        typeof(Transaction),
        typeof(Credit)
    ];

    private static readonly HashSet<(Type, string)> ExcludedProperties =
    [
        (typeof(Asset), nameof(Asset.AveragePrice)),
        (typeof(Asset), nameof(Asset.Quantity)),
        (typeof(Asset), nameof(Asset.Active)),
        (typeof(Transaction), nameof(Transaction.TotalPrice))
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
        var toRemove = new List<JsonPropertyInfo>();

        foreach (var jsonProp in typeInfo.Properties)
        {
            if (ExcludedProperties.Contains((type, jsonProp.Name)))
            {
                toRemove.Add(jsonProp);
                continue;
            }

            WirePropertySetter(type, jsonProp);
        }

        foreach (var prop in toRemove)
            typeInfo.Properties.Remove(prop);
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
