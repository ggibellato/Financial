namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Identifies which of two ambiguous columns holds the expense category vs. the free-text
/// description. The header labels ("Quem"/"Motivo") swap meaning between spreadsheet eras, so
/// the header text cannot be trusted — the column whose values most often match a seeded
/// category name is the category column.
/// </summary>
public static class ColumnResolver
{
    public static bool IsCategoryColumn(
        IReadOnlyList<string?> candidateValues, IReadOnlyList<string?> otherValues, IReadOnlyCollection<string> categoryNames)
    {
        var nameSet = new HashSet<string>(categoryNames, StringComparer.OrdinalIgnoreCase);
        var candidateMatches = CountCategoryMatches(candidateValues, nameSet);
        var otherMatches = CountCategoryMatches(otherValues, nameSet);
        return candidateMatches >= otherMatches;
    }

    private static int CountCategoryMatches(IReadOnlyList<string?> values, HashSet<string> categoryNames) =>
        values.Count(v => v is not null && categoryNames.Contains(v.Trim()));
}
