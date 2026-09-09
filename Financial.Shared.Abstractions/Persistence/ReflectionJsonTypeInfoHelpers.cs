using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Financial.Shared.Abstractions.Persistence;

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

        jsonProp.Set = CompileSetter(propInfo.SetMethod);
    }

    /// <summary>
    /// Compiles the property's setter once (here, at <see cref="JsonTypeInfo"/>-build time, which
    /// System.Text.Json itself caches per <see cref="System.Text.Json.JsonSerializerOptions"/>)
    /// into a typed delegate, instead of paying <see cref="MethodInfo.Invoke"/>'s per-call
    /// argument-boxing and signature-check overhead on every property set during every full
    /// document load.
    /// </summary>
    private static Action<object, object?> CompileSetter(MethodInfo setter)
    {
        var targetParam = Expression.Parameter(typeof(object), "target");
        var valueParam = Expression.Parameter(typeof(object), "value");

        var typedTarget = Expression.Convert(targetParam, setter.DeclaringType!);
        var typedValue = Expression.Convert(valueParam, setter.GetParameters()[0].ParameterType);
        var call = Expression.Call(typedTarget, setter, typedValue);

        return Expression.Lambda<Action<object, object?>>(call, targetParam, valueParam).Compile();
    }
}
