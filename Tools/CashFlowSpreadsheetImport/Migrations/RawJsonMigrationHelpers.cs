using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations;

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

    /// <summary>Reads a collection if the legacy file already has it, otherwise seeds it via the
    /// entity's own migrator (the file predates that entity's seed migration). <paramref name="onBootstrapped"/>
    /// lets a caller record how many rows were seeded, for migrators whose summary tracks that.</summary>
    public static List<T> ResolveOrBootstrap<T>(
        JsonElement root, string propertyName, JsonSerializerOptions unresolvedOptions,
        Action<CashFlowData> bootstrap, Func<CashFlowData, IEnumerable<T>> selectSeeded,
        Action<int>? onBootstrapped = null)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Array)
        {
            return DeserializeCollection<T>(root, propertyName, unresolvedOptions);
        }

        var bootstrapData = CashFlowData.Create();
        bootstrap(bootstrapData);
        var bootstrapped = selectSeeded(bootstrapData).ToList();
        onBootstrapped?.Invoke(bootstrapped.Count);
        return bootstrapped;
    }

    public static void SetId(object entity, Guid id) =>
        entity.GetType().GetProperty("Id")!.SetMethod!.Invoke(entity, [id]);
}
