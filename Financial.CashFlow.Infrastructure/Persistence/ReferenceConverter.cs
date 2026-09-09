using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

/// <summary>
/// Base for a reference-typed property converter: on read, resolves a Guid against a lookup built
/// earlier in the same <see cref="CashFlowDataConverter"/> call; on write, emits only the
/// referenced entity's Id. The context is null for a write-only options instance, since writing
/// never needs to resolve anything.
/// </summary>
public abstract class ReferenceConverter<T> : JsonConverter<T> where T : class
{
    private readonly Dictionary<Guid, T>? _lookup;
    private readonly Func<T, Guid> _idSelector;

    protected ReferenceConverter(Dictionary<Guid, T>? lookup, Func<T, Guid> idSelector)
    {
        _lookup = lookup;
        _idSelector = idSelector;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var id = reader.GetGuid();

        if (_lookup is null)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} was used to read without a resolution context.");
        }

        if (!_lookup.TryGetValue(id, out var resolved))
        {
            var entityName = typeof(T).Name;
            throw new JsonException($"{entityName} '{id}' referenced but not found in the seeded {entityName} collection.");
        }

        return resolved;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(_idSelector(value));
}
