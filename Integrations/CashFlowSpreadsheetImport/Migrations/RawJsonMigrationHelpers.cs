using Financial.CashFlow.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations;

/// <summary>
/// Shared low-level helpers for one-time raw-JSON rewrite migrators (<c>EntityReferenceMigrator</c>,
/// <c>ReserveBucketReferenceMigrator</c>) that read a data file before the normal typed
/// <see cref="CashFlowDataConverter"/> load can run. Each migrator still owns its own detect/rewrite
/// logic - only the entity-agnostic JSON plumbing lives here.
/// </summary>
internal static class RawJsonMigrationHelpers
{
    public static JsonSerializerOptions CreateElementOptions(ReferenceResolutionContext? context = null) => new()
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new CashFlowTypeInfoResolver(context)
    };

    public static List<T> DeserializeCollection<T>(JsonElement root, string propertyName, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(element.GetRawText(), options) ?? [];
    }

    public static void SetId(object entity, Guid id) =>
        entity.GetType().GetProperty("Id")!.SetMethod!.Invoke(entity, [id]);
}
