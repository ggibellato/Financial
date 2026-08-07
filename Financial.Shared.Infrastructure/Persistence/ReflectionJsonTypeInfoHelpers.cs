using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Financial.Shared.Infrastructure.Persistence;

/// <summary>
/// Reflection-based wiring shared by the CashFlow and Investment <c>JsonTypeInfoResolver</c>s:
/// enabling a type's private constructor for deserialization, and wiring a JSON property to a
/// property's private setter (case-insensitively) since <see cref="System.Text.Json"/> only
/// wires public setters by default.
/// </summary>
public static class ReflectionJsonTypeInfoHelpers
{
    public static void EnablePrivateConstructor(Type type, JsonTypeInfo typeInfo)
    {
        if (typeInfo.CreateObject is not null)
            return;

        typeInfo.CreateObject = () =>
            Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException($"Failed to create instance of {type}.");
    }

    public static void WirePropertySetter(Type type, JsonPropertyInfo jsonProp)
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
