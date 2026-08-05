using System.Text.Json;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ExpenseChargeDate;

/// <summary>
/// Recovers each expense's pre-F01 SettledAt value directly from the raw pre-migration JSON
/// text, since Expense no longer declares that property - a normal typed deserialization would
/// silently discard it before ExpenseChargeDateMigrator ever saw the object.
/// </summary>
public static class LegacySettledAtExtractor
{
    public static IReadOnlyDictionary<Guid, DateOnly> Extract(string? rawJson)
    {
        var result = new Dictionary<Guid, DateOnly>();

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return result;
        }

        using var document = JsonDocument.Parse(rawJson);
        if (!document.RootElement.TryGetProperty("Expenses", out var expensesElement)
            || expensesElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var expenseElement in expensesElement.EnumerateArray())
        {
            if (!expenseElement.TryGetProperty("Id", out var idElement)
                || !expenseElement.TryGetProperty("SettledAt", out var settledAtElement)
                || settledAtElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            if (Guid.TryParse(idElement.GetString(), out var id)
                && DateOnly.TryParse(settledAtElement.GetString(), out var settledAt))
            {
                result[id] = settledAt;
            }
        }

        return result;
    }
}
