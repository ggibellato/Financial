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
    private readonly string _entityName;

    protected ReferenceConverter(Dictionary<Guid, T>? lookup, Func<T, Guid> idSelector, string entityName)
    {
        _lookup = lookup;
        _idSelector = idSelector;
        _entityName = entityName;
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
            throw new JsonException($"{_entityName} '{id}' referenced but not found in the seeded {_entityName} collection.");
        }

        return resolved;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(_idSelector(value));
}
