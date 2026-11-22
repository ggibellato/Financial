using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Financial.TestUtilities;

public static class StoredJsonProperties
{
    public static IEnumerable<string> Written(JsonTypeInfo typeInfo) =>
        typeInfo.Properties.Select(property => ((PropertyInfo)property.AttributeProvider!).Name);

    public static IEnumerable<string> Settable(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0 && HasSetter(property))
            .Select(property => property.Name);

    private static bool HasSetter(PropertyInfo property) =>
        property.DeclaringType!
            .GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!
            .SetMethod is not null;
}
