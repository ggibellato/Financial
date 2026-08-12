using System.Diagnostics.CodeAnalysis;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Resolves a raw category label from the spreadsheet to a seeded <see cref="Category"/> by
/// name, tolerating known historical typos. A label that matches neither a seeded name nor a known
/// typo is not imported — it is reported instead, per the PRD's "flagged in the error report,
/// rather than being silently dropped or miscategorized" requirement.
/// </summary>
public static class CategoryResolver
{
    private static readonly Dictionary<string, string> KnownTypos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Casas"] = "Casa",
    };

    public static bool TryResolve(
        string? rawLabel,
        IReadOnlyDictionary<string, Category> categoriesByName,
        [NotNullWhen(true)] out Category? category)
    {
        var trimmedLabel = rawLabel?.Trim();
        if (!string.IsNullOrEmpty(trimmedLabel) && categoriesByName.TryGetValue(trimmedLabel, out category))
        {
            return true;
        }

        if (trimmedLabel is not null
            && KnownTypos.TryGetValue(trimmedLabel, out var correctedName)
            && categoriesByName.TryGetValue(correctedName, out category))
        {
            return true;
        }

        category = null;
        return false;
    }
}
