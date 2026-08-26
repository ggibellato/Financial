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

    /// <summary>Builds a resolution context from the base reference collections. Pass an empty list
    /// for any collection a migrator doesn't have (e.g. one that predates it, or one it's itself
    /// migrating and resolves by name instead) - iterating an empty list is a no-op.</summary>
    public static ReferenceResolutionContext BuildContext(
        IEnumerable<Bank> banks, IEnumerable<IncomeSource> incomeSources, IEnumerable<InvestmentAccount> investmentAccounts,
        IEnumerable<ReserveBucket> reserveBuckets, IEnumerable<CreditCard> creditCards, IEnumerable<Category> categories)
    {
        var context = new ReferenceResolutionContext();
        foreach (var bank in banks) context.Banks[bank.Id] = bank;
        foreach (var incomeSource in incomeSources) context.IncomeSources[incomeSource.Id] = incomeSource;
        foreach (var account in investmentAccounts) context.InvestmentAccounts[account.Id] = account;
        foreach (var bucket in reserveBuckets) context.ReserveBuckets[bucket.Id] = bucket;
        foreach (var card in creditCards) context.CreditCards[card.Id] = card;
        foreach (var category in categories) context.Categories[category.Id] = category;
        return context;
    }

    /// <summary>Adds the base reference collections into a freshly created <see cref="CashFlowData"/>.
    /// Same empty-list convention as <see cref="BuildContext"/>.</summary>
    public static void AddBaseCollections(
        CashFlowData data,
        IEnumerable<Bank> banks, IEnumerable<IncomeSource> incomeSources, IEnumerable<InvestmentAccount> investmentAccounts,
        IEnumerable<ReserveBucket> reserveBuckets, IEnumerable<CreditCard> creditCards, IEnumerable<Category> categories)
    {
        foreach (var bank in banks) data.AddBank(bank);
        foreach (var incomeSource in incomeSources) data.AddIncomeSource(incomeSource);
        foreach (var account in investmentAccounts) data.AddInvestmentAccount(account);
        foreach (var bucket in reserveBuckets) data.AddReserveBucket(bucket);
        foreach (var card in creditCards) data.AddCreditCard(card);
        foreach (var category in categories) data.AddCategory(category);
    }

    /// <summary>Backs up the original file, serializes the rewritten data over it, and returns the
    /// migration summary unchanged - the shared tail every rewriting migrator ends with.</summary>
    public static TSummary SaveAndReturn<TSummary>(string dataPath, CashFlowData data, TSummary summary)
    {
        MigrationBackup.Create(dataPath);
        var serializer = new CashFlowSerializerAdapter();
        File.WriteAllText(dataPath, serializer.Serialize(data));
        return summary;
    }

    /// <summary>Rewrites one legacy string-named JSON property to a new Guid-valued property (e.g.
    /// "Category" -> "CategoryId"), copying every other property through unchanged. Writes the new
    /// field even if the legacy key was never present at all (as opposed to present with a null
    /// value) - the new field is required on read.</summary>
    public static string RewriteField(JsonElement item, string legacyFieldName, string newFieldName, Guid? newValue)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            var wroteNewField = false;
            foreach (var property in item.EnumerateObject())
            {
                if (property.NameEquals(legacyFieldName))
                {
                    WriteGuidOrNull(writer, newFieldName, newValue);
                    wroteNewField = true;
                    continue;
                }

                property.WriteTo(writer);
            }

            if (!wroteNewField)
            {
                WriteGuidOrNull(writer, newFieldName, newValue);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteGuidOrNull(Utf8JsonWriter writer, string propertyName, Guid? value)
    {
        writer.WritePropertyName(propertyName);
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
