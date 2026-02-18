using System.Reflection;
using System;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;

namespace Financial.Common;

public class PrivateConstructorContractResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

        if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null)
        {
            if (jsonTypeInfo.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
            {
                // The type doesn't have public constructors
                jsonTypeInfo.CreateObject = () =>
                    Activator.CreateInstance(jsonTypeInfo.Type, true)
                    ?? throw new InvalidOperationException(
                        $"Failed to create instance of {jsonTypeInfo.Type}.");
            }
        }
        return jsonTypeInfo;
    }
}