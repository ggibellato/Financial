using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Resolves a raw category label from the spreadsheet to a <see cref="Category"/>, tolerating
/// known historical typos. <see cref="Category"/> has no "raw label" storage of its own (F02
/// confirmed the real workbook uses exactly the 14 current names plus one single-occurrence
/// typo), so a label that still can't be resolved is not imported — it is reported instead,
/// per the PRD's "flagged in the error report, rather than being silently dropped or
/// miscategorized" requirement.
/// </summary>
public static class CategoryResolver
{
    private static readonly Dictionary<string, Category> KnownTypos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Casas"] = Category.Casa,
    };

    public static bool TryResolve(string? rawLabel, out Category category)
    {
        if (CategoryParser.TryParse(rawLabel, out category))
        {
            return true;
        }

        if (rawLabel is not null && KnownTypos.TryGetValue(rawLabel.Trim(), out category))
        {
            return true;
        }

        category = default;
        return false;
    }
}
