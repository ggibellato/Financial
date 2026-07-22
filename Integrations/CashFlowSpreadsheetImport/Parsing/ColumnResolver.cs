using Financial.CashFlow.Application.Validation;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Identifies which of two ambiguous columns holds the expense category vs. the free-text
/// description. The header labels ("Quem"/"Motivo") swap meaning between spreadsheet eras, so
/// the header text cannot be trusted — the column whose values most often match a known
/// <see cref="Financial.CashFlow.Domain.Enums.Category"/> name is the category column.
/// </summary>
public static class ColumnResolver
{
    public static bool IsCategoryColumn(IReadOnlyList<string?> candidateValues, IReadOnlyList<string?> otherValues)
    {
        var candidateMatches = CountCategoryMatches(candidateValues);
        var otherMatches = CountCategoryMatches(otherValues);
        return candidateMatches >= otherMatches;
    }

    private static int CountCategoryMatches(IReadOnlyList<string?> values) =>
        values.Count(v => CategoryParser.TryParse(v, out _));
}
