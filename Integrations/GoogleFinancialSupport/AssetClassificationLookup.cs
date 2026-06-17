using Financial.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal readonly record struct AssetClassificationEntry(
    CountryCode Country,
    string LocalTypeCode,
    GlobalAssetClass Class);

internal static class AssetClassificationLookup
{
    private static readonly IReadOnlyDictionary<string, AssetClassificationEntry> Entries = LoadEntries();

    public static bool TryGet(string assetName, out AssetClassificationEntry entry)
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            entry = default;
            return false;
        }

        return Entries.TryGetValue(assetName.Trim(), out entry);
    }

    private static IReadOnlyDictionary<string, AssetClassificationEntry> LoadEntries()
    {
        const string ResourceName =
            "Financial.Infrastructure.Integrations.GoogleFinancialSupport.AssetClassifications.json";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' was not found. " +
                "Ensure AssetClassifications.json is marked as EmbeddedResource in the project file.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        var jsonEntries = JsonSerializer.Deserialize<AssetClassificationJson[]>(stream, options)!;

        return jsonEntries.ToDictionary(
            e => e.Name,
            e => new AssetClassificationEntry(e.Country, e.LocalTypeCode, e.AssetClass),
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed class AssetClassificationJson
    {
        public required string Name { get; set; }
        public CountryCode Country { get; set; }
        public string LocalTypeCode { get; set; } = "";
        public GlobalAssetClass AssetClass { get; set; }
    }
}
